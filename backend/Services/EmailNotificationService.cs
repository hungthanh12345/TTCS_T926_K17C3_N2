using System.Net;
using System.Net.Mail;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Services;

public interface IEmailLogQueue
{
    Task QueueDecisionEmailAsync(Student student, bool approved, CancellationToken cancellationToken = default);
}

public interface IEmailLogProcessor
{
    Task ProcessDueAsync(CancellationToken cancellationToken = default);
}

public interface ISmtpEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}

public sealed class EmailLogQueue : IEmailLogQueue
{
    private readonly AppDbContext _db;

    public EmailLogQueue(AppDbContext db) => _db = db;

    public Task QueueDecisionEmailAsync(
        Student student,
        bool approved,
        CancellationToken cancellationToken = default)
    {
        var recipient = student.User?.Email?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
            throw new InvalidOperationException("The student account has no recipient email address.");

        _db.EmailLogs.Add(new EmailLog
        {
            StudentId = student.Id,
            RecipientEmail = recipient,
            TemplateCode = approved ? "APP_APPROVED" : "APP_REJECTED",
            Status = "PENDING",
            ErrorMessage = null,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            NextAttemptAt = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }
}

public sealed class SmtpEmailSender : ISmtpEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration) => _configuration = configuration;

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = ReadSetting("SMTP_HOST", "Smtp:Host");
        var user = ReadSetting("SMTP_USER", "Smtp:User");
        var password = ReadSetting("SMTP_PASS", "Smtp:Password");
        var from = ReadSetting("MAIL_FROM", "Mail:From");
        var portText = ReadSetting("SMTP_PORT", "Smtp:Port");

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("SMTP is not configured. Set SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASS, and MAIL_FROM.");
        }

        if (!int.TryParse(portText, out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("SMTP_PORT must be a valid TCP port.");

        using var message = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipient));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user, password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private string? ReadSetting(string environmentName, string configurationPath) =>
        Environment.GetEnvironmentVariable(environmentName) ?? _configuration[configurationPath];
}

public sealed class EmailLogProcessor : IEmailLogProcessor
{
    private const int MaximumRetries = 3;
    private const int ClaimLeaseSeconds = 180;
    private static readonly TimeSpan DeliveryTimeout = TimeSpan.FromSeconds(120);
    private readonly AppDbContext _db;
    private readonly ISmtpEmailSender _sender;
    private readonly ILogger<EmailLogProcessor> _logger;

    public EmailLogProcessor(AppDbContext db, ISmtpEmailSender sender, ILogger<EmailLogProcessor> logger)
    {
        _db = db;
        _sender = sender;
        _logger = logger;
    }

    public async Task ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        var dueIds = await _db.EmailLogs.AsNoTracking()
            .Where(log => log.Status == "PENDING" && log.NextAttemptAt <= DateTime.UtcNow)
            .OrderBy(log => log.NextAttemptAt)
            .ThenBy(log => log.Id)
            .Select(log => log.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var id in dueIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessOneAsync(id, cancellationToken);
        }
    }

    private async Task ProcessOneAsync(int id, CancellationToken cancellationToken)
    {
        // Atomically lease a due row so multiple application instances cannot send it at once.
        // The lease uses database UTC time and is longer than the SMTP send timeout.
        var claimedRows = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE email_logs
            SET next_attempt_at = TIMESTAMPADD(SECOND, {ClaimLeaseSeconds}, UTC_TIMESTAMP(6))
            WHERE id = {id}
              AND status = 'PENDING'
              AND next_attempt_at <= UTC_TIMESTAMP(6)
            """, cancellationToken);
        if (claimedRows != 1) return;

        var log = await _db.EmailLogs
            .Include(item => item.Template)
            .Include(item => item.Student!)
                .ThenInclude(student => student.Program)
            .SingleOrDefaultAsync(item => item.Id == id && item.Status == "PENDING", cancellationToken);
        if (log == null || log.Template == null) return;

        try
        {
            if (!log.Template.IsActive)
                throw new InvalidOperationException($"Email template '{log.TemplateCode}' is inactive.");

            var (subject, body) = Render(log);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(DeliveryTimeout);
            await _sender.SendAsync(log.RecipientEmail, subject, body, timeout.Token);
            log.Status = "SENT";
            log.SentAt = DateTime.UtcNow;
            log.ErrorMessage = null;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var now = DateTime.UtcNow;
            log.ErrorMessage = Limit($"{exception.GetType().Name}: {exception.Message}", 2000);
            if (log.RetryCount < MaximumRetries)
            {
                log.RetryCount++;
                log.NextAttemptAt = now.Add(RetryDelay(log.RetryCount));
            }
            else
            {
                log.Status = "FAILED";
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Email log {EmailLogId} delivery failed (retry {RetryCount}/{MaximumRetries}): {Error}",
                log.Id, log.RetryCount, MaximumRetries, log.ErrorMessage);
        }
    }

    private static (string Subject, string Body) Render(EmailLog log)
    {
        var student = log.Student;
        var programName = student?.Program?.Name ?? "chương trình thực tập";
        var startDate = student?.Program?.StartDate is { } date
            ? $" (ngày bắt đầu dự kiến: {date:dd/MM/yyyy})"
            : string.Empty;
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{StudentName}}"] = student?.FullName ?? "Sinh viên",
            ["{{ProgramName}}"] = programName,
            ["{{StartDate}}"] = startDate,
            ["{{NextSteps}}"] = "Bộ phận Nhân sự sẽ liên hệ bạn để hướng dẫn các bước tiếp theo.",
            ["{{RejectionReason}}"] = string.IsNullOrWhiteSpace(student?.RejectionReason)
                ? "HR chưa cung cấp lý do cụ thể. Bạn có thể liên hệ bộ phận Nhân sự để được hỗ trợ."
                : student.RejectionReason.Trim()
        };

        string Apply(string template) => values.Aggregate(template, (result, value) =>
            result.Replace(value.Key, value.Value, StringComparison.Ordinal));

        return (Apply(log.Template.SubjectTemplate), Apply(log.Template.BodyTemplate));
    }

    private static TimeSpan RetryDelay(int retryNumber) => retryNumber switch
    {
        1 => TimeSpan.FromMinutes(1),
        2 => TimeSpan.FromMinutes(5),
        _ => TimeSpan.FromMinutes(15)
    };

    private static string Limit(string value, int length) => value.Length <= length ? value : value[..length];
}

public sealed class EmailNotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationWorker> _logger;

    public EmailNotificationWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<EmailNotificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = int.TryParse(_configuration["Email:PollingIntervalSeconds"], out var configured) && configured > 0
            ? configured
            : 5;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));

        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IEmailLogProcessor>();
                await processor.ProcessDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Email background worker encountered an unexpected error.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
