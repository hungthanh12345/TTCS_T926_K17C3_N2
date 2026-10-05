using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Notifications;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Services
{
    public interface INotificationService
    {
        Task<IReadOnlyList<NotificationDto>> GetForUserAsync(int userId, string role, CancellationToken cancellationToken = default);
        Task<bool> MarkReadAsync(int userId, long notificationId, CancellationToken cancellationToken = default);
        Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default);
        Task AddRegistrationReviewedAsync(Student student, bool approved, CancellationToken cancellationToken = default);
        Task AddMentorAssignedAsync(Student student, CancellationToken cancellationToken = default);
    }

    public sealed class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;

        public NotificationService(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(
            int userId,
            string role,
            CancellationToken cancellationToken = default)
        {
            var candidates = await BuildCurrentNotificationsAsync(userId, role, cancellationToken);
            var sourceKeys = candidates.Select(item => item.SourceKey).ToArray();
            var current = sourceKeys.Length == 0
                ? new List<UserNotification>()
                : await _db.Notifications
                    .Where(item => item.UserId == userId && sourceKeys.Contains(item.SourceKey))
                    .ToListAsync(cancellationToken);

            foreach (var candidate in candidates)
            {
                var existing = current.SingleOrDefault(item => item.SourceKey == candidate.SourceKey);
                if (existing == null)
                {
                    existing = new UserNotification
                    {
                        UserId = userId,
                        SourceKey = candidate.SourceKey,
                        Title = candidate.Title,
                        Message = candidate.Message,
                        Route = candidate.Route,
                        CreatedAt = candidate.CreatedAt ?? DateTime.UtcNow
                    };
                    current.Add(existing);
                    _db.Notifications.Add(existing);
                    continue;
                }

                var newerSourceRecord = candidate.CreatedAt.HasValue && candidate.CreatedAt.Value > existing.CreatedAt;
                if (existing.Title != candidate.Title || existing.Message != candidate.Message || existing.Route != candidate.Route || newerSourceRecord)
                {
                    existing.Title = candidate.Title;
                    existing.Message = candidate.Message;
                    existing.Route = candidate.Route;
                    existing.CreatedAt = candidate.CreatedAt ?? DateTime.UtcNow;
                    existing.ReadAt = null;
                }
            }

            if (_db.ChangeTracker.HasChanges())
                await _db.SaveChangesAsync(cancellationToken);

            var durableEvents = await _db.Notifications
                .AsNoTracking()
                .Where(item => item.UserId == userId && item.SourceKey.StartsWith("event:"))
                .ToListAsync(cancellationToken);

            return current.Concat(durableEvents)
                .GroupBy(item => item.Id)
                .Select(group => group.First())
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Take(50)
                .Select(ToDto)
                .ToArray();
        }

        public async Task<bool> MarkReadAsync(int userId, long notificationId, CancellationToken cancellationToken = default)
        {
            var notification = await _db.Notifications
                .SingleOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId, cancellationToken);
            if (notification == null) return false;
            if (notification.ReadAt == null)
            {
                notification.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
            return true;
        }

        public async Task MarkAllReadAsync(int userId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            await _db.Notifications
                .Where(item => item.UserId == userId && item.ReadAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.ReadAt, now), cancellationToken);
        }

        public Task AddRegistrationReviewedAsync(
            Student student,
            bool approved,
            CancellationToken cancellationToken = default)
        {
            if (!student.UserId.HasValue) return Task.CompletedTask;

            var title = approved ? "Đăng ký thực tập đã được duyệt" : "Hồ sơ đăng ký thực tập đã bị từ chối";
            var message = approved
                ? "Hồ sơ đăng ký thực tập của bạn đã được HR xét duyệt."
                : "Hồ sơ đăng ký thực tập của bạn chưa được duyệt. Vui lòng liên hệ HR để biết thêm thông tin.";
            var sourceKey = $"event:registration-reviewed:{student.Id}:{(approved ? "approved" : "rejected")}";
            return AddEventAsync(student.UserId.Value, sourceKey, title, message, "/student/profile", cancellationToken);
        }

        public Task AddMentorAssignedAsync(Student student, CancellationToken cancellationToken = default)
        {
            if (!student.UserId.HasValue || !student.MentorId.HasValue) return Task.CompletedTask;
            return AddEventAsync(
                student.UserId.Value,
                $"event:mentor-assigned:{student.Id}:{student.MentorId.Value}",
                "Bạn đã được phân công Mentor",
                "Thông tin Mentor hướng dẫn đã được cập nhật trong hồ sơ thực tập của bạn.",
                "/student/profile",
                cancellationToken);
        }

        private async Task AddEventAsync(
            int userId,
            string sourceKey,
            string title,
            string message,
            string route,
            CancellationToken cancellationToken)
        {
            if (await _db.Notifications.AnyAsync(item => item.UserId == userId && item.SourceKey == sourceKey, cancellationToken))
                return;

            _db.Notifications.Add(new UserNotification
            {
                UserId = userId,
                SourceKey = sourceKey,
                Title = title,
                Message = message,
                Route = route,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<List<NotificationCandidate>> BuildCurrentNotificationsAsync(
            int userId,
            string role,
            CancellationToken cancellationToken)
        {
            var notifications = new List<NotificationCandidate>();

            if (role is "ROLE_ADMIN" or "ROLE_HR")
            {
                var pendingRegistrations = _db.Students
                    .Where(student => student.User != null &&
                                      student.User.Role.Name == "ROLE_STUDENT" &&
                                      student.User.Status == UserStatus.PENDING_APPROVAL);
                var pendingCount = await pendingRegistrations.CountAsync(cancellationToken);
                var pendingSince = await pendingRegistrations.Select(student => (DateTime?)student.CreatedAt).MaxAsync(cancellationToken);
                AddCount(notifications, "pending-registrations", "Hồ sơ đăng ký đang chờ duyệt",
                    pendingCount, "hồ sơ sinh viên cần được xem xét.",
                    role == "ROLE_HR" ? "/hr/student-registrations" : "/admin/users", pendingSince);

                var accountsWithoutProfiles = _db.Users
                    .Where(user => user.Role.Name == "ROLE_STUDENT" &&
                                   !_db.Students.Any(student => student.UserId == user.Id));
                var unlinkedAccounts = await accountsWithoutProfiles.CountAsync(cancellationToken);
                var unlinkedAccountSince = await accountsWithoutProfiles.Select(user => (DateTime?)user.CreatedAt).MaxAsync(cancellationToken);
                AddCount(notifications, "unlinked-student-accounts", "Tài khoản sinh viên chưa có hồ sơ",
                    unlinkedAccounts, "tài khoản cần được liên kết với hồ sơ sinh viên.", "/hr/students", unlinkedAccountSince);

                var unassignedStudents = _db.Students.Where(student => student.MentorId == null &&
                    (student.User == null ||
                     (student.User.Status != UserStatus.PENDING_APPROVAL && student.User.Status != UserStatus.REJECTED)));
                var unassignedCount = await unassignedStudents.CountAsync(cancellationToken);
                var unassignedSince = await unassignedStudents.Select(student => (DateTime?)student.CreatedAt).MaxAsync(cancellationToken);
                AddCount(notifications, "students-without-mentor", "Sinh viên chưa được phân Mentor",
                    unassignedCount, "hồ sơ đang chờ phân công Mentor.", "/hr/students", unassignedSince);

                var profilesWithoutAccounts = _db.Students.Where(student => student.UserId == null);
                var orphanProfileCount = await profilesWithoutAccounts.CountAsync(cancellationToken);
                var orphanProfileSince = await profilesWithoutAccounts.Select(student => (DateTime?)student.CreatedAt).MaxAsync(cancellationToken);
                AddCount(notifications, "student-profiles-without-account", "Hồ sơ chưa liên kết tài khoản",
                    orphanProfileCount, "hồ sơ cần được đối chiếu thủ công.", "/hr/students", orphanProfileSince);
            }
            else if (role == "ROLE_MENTOR")
            {
                var mentorId = await _db.Mentors.Where(mentor => mentor.UserId == userId)
                    .Select(mentor => (int?)mentor.Id).SingleOrDefaultAsync(cancellationToken);
                if (mentorId.HasValue)
                {
                    var openTasks = _db.Tasks.Where(task => task.MentorId == mentorId.Value && task.Status != "DONE");
                    var openTaskCount = await openTasks.CountAsync(cancellationToken);
                    var openTaskSince = await openTasks.Select(task => (DateTime?)task.CreatedAt).MaxAsync(cancellationToken);
                    AddCount(notifications, "mentor-open-tasks", "Công việc đang theo dõi", openTaskCount,
                        "nhiệm vụ chưa hoàn tất trong danh sách Mentor.", "/mentor/students", openTaskSince);

                    var reportsAwaitingFeedback = _db.WeeklyReports.Where(report =>
                        report.Student.MentorId == mentorId.Value &&
                        !_db.MentorFeedbacks.Any(feedback => feedback.WeeklyReportId == report.Id));
                    var reportCount = await reportsAwaitingFeedback.CountAsync(cancellationToken);
                    var reportSince = await reportsAwaitingFeedback.Select(report => (DateTime?)report.CreatedAt).MaxAsync(cancellationToken);
                    AddCount(notifications, "mentor-reports-awaiting-feedback", "Báo cáo tuần chờ phản hồi", reportCount,
                        "báo cáo của sinh viên đang chờ Mentor phản hồi.", "/mentor/students", reportSince);

                    var studentsAwaitingEvaluation = _db.Students.Where(student =>
                        student.MentorId == mentorId.Value &&
                        !_db.InternshipEvaluations.Any(evaluation => evaluation.StudentId == student.Id));
                    var evaluationCount = await studentsAwaitingEvaluation.CountAsync(cancellationToken);
                    var evaluationSince = await studentsAwaitingEvaluation.Select(student => (DateTime?)student.CreatedAt).MaxAsync(cancellationToken);
                    AddCount(notifications, "mentor-evaluations-pending", "Sinh viên chưa có đánh giá", evaluationCount,
                        "sinh viên được phân công chưa có đánh giá thực tập.", "/mentor/students", evaluationSince);
                }
            }
            else if (role == "ROLE_STUDENT")
            {
                var studentId = await _db.Students.Where(student => student.UserId == userId)
                    .Select(student => (int?)student.Id).SingleOrDefaultAsync(cancellationToken);
                if (studentId.HasValue)
                {
                    var activeTasks = _db.Tasks.Where(task => task.StudentId == studentId.Value && task.Status != "DONE");
                    var activeTaskCount = await activeTasks.CountAsync(cancellationToken);
                    var activeTaskSince = await activeTasks.Select(task => (DateTime?)task.CreatedAt).MaxAsync(cancellationToken);
                    AddCount(notifications, "student-active-tasks", "Nhiệm vụ chưa hoàn tất", activeTaskCount,
                        "nhiệm vụ đang cần bạn cập nhật tiến độ.", "/student/profile", activeTaskSince);

                    var feedbacks = _db.MentorFeedbacks.Where(feedback => feedback.WeeklyReport.StudentId == studentId.Value);
                    var feedbackCount = await feedbacks.CountAsync(cancellationToken);
                    var feedbackSince = await feedbacks.Select(feedback => (DateTime?)feedback.CreatedAt).MaxAsync(cancellationToken);
                    AddCount(notifications, "student-mentor-feedback", "Phản hồi từ Mentor", feedbackCount,
                        "phản hồi có trong báo cáo tuần của bạn.", "/student/profile", feedbackSince);

                    var evaluation = await _db.InternshipEvaluations
                        .Where(item => item.StudentId == studentId.Value)
                        .Select(item => new { item.EvaluatedAt })
                        .SingleOrDefaultAsync(cancellationToken);
                    if (evaluation != null)
                        notifications.Add(new NotificationCandidate("student-evaluation-available", "Đã có đánh giá thực tập",
                            "Mentor đã lưu đánh giá cho hồ sơ của bạn.", "/student/profile", evaluation.EvaluatedAt));

                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    var dueSoon = _db.Tasks.Where(task => task.StudentId == studentId.Value && task.Status != "DONE" &&
                        task.DueDate != null && task.DueDate >= today && task.DueDate <= today.AddDays(7));
                    var dueSoonCount = await dueSoon.CountAsync(cancellationToken);
                    var dueSoonSince = await dueSoon.Select(task => (DateTime?)task.CreatedAt).MaxAsync(cancellationToken);
                    AddCount(notifications, "student-upcoming-deadlines", "Nhiệm vụ sắp đến hạn", dueSoonCount,
                        "nhiệm vụ chưa hoàn tất có hạn trong 7 ngày tới.", "/student/profile", dueSoonSince);

                    var overdueTasks = _db.Tasks.Where(task => task.StudentId == studentId.Value && task.Status != "DONE" &&
                        task.DueDate != null && task.DueDate < today);
                    var overdueCount = await overdueTasks.CountAsync(cancellationToken);
                    var overdueSince = await overdueTasks.Select(task => (DateTime?)task.CreatedAt).MaxAsync(cancellationToken);
                    AddCount(notifications, "student-overdue-deadlines", "Nhiệm vụ quá hạn", overdueCount,
                        "nhiệm vụ chưa hoàn tất đã quá hạn.", "/student/profile", overdueSince);
                }
            }

            return notifications;
        }

        private static void AddCount(
            ICollection<NotificationCandidate> notifications,
            string sourceKey,
            string title,
            int count,
            string description,
            string route,
            DateTime? createdAt)
        {
            if (count <= 0) return;
            notifications.Add(new NotificationCandidate($"summary:{sourceKey}", title,
                $"{count} {description}", route, createdAt));
        }

        private static NotificationDto ToDto(UserNotification notification) => new()
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Route = notification.Route,
            CreatedAt = notification.CreatedAt,
            IsRead = notification.ReadAt.HasValue,
            ReadAt = notification.ReadAt
        };

        private sealed record NotificationCandidate(string SourceKey, string Title, string Message, string Route, DateTime? CreatedAt);
    }
}
