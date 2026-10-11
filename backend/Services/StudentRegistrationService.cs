using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Auth;
using InternshipManagementApi.DTOs.StudentRegistration;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Services
{
    public interface IStudentRegistrationService
    {
        Task<StudentRegistrationStatusResponseDto> RegisterAsync(StudentRegistrationRequestDto request);
        Task<StudentRegistrationStatusResponseDto> RegisterWithApplicationAsync(StudentRegistrationApplicationRequestDto request);
        Task<StudentRegistrationStatusResponseDto> GetOwnStatusAsync(LoginRequestDto request);
        Task<IReadOnlyList<StudentRegistrationReviewDto>> GetPendingAsync();
        Task<StudentRegistrationReviewDto> GetPendingDetailsAsync(int studentId);
        Task<StudentRegistrationReviewDto> ApproveAsync(int studentId, int reviewerUserId);
        Task<StudentRegistrationReviewDto> RejectAsync(int studentId, int reviewerUserId, string? rejectionReason = null);
    }

    public sealed class StudentRegistrationService : IStudentRegistrationService
    {
        private const string StudentRoleName = "ROLE_STUDENT";
        private readonly AppDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly INotificationService _notificationService;
        private readonly IEmailLogQueue _emailLogQueue;

        public StudentRegistrationService(
            AppDbContext db,
            IPasswordHasher passwordHasher,
            INotificationService notificationService,
            IEmailLogQueue emailLogQueue)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _notificationService = notificationService;
            _emailLogQueue = emailLogQueue;
        }

        public async Task<StudentRegistrationStatusResponseDto> RegisterAsync(StudentRegistrationRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var studentCode = request.StudentCode.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.University) ||
                string.IsNullOrWhiteSpace(request.Major) ||
                string.IsNullOrWhiteSpace(studentCode))
            {
                throw new BadRequestException("Student profile fields cannot be blank.");
            }

            if (await _db.Users.AnyAsync(user => user.Email.ToLower() == email))
                throw new ConflictException("This email is already registered.");

            if (await _db.Students.AnyAsync(student => student.StudentCode.ToLower() == studentCode.ToLower()))
                throw new ConflictException("This student code is already registered.");

            var studentRole = await _db.Roles.SingleOrDefaultAsync(role => role.Name == StudentRoleName);
            if (studentRole == null)
                throw new BadRequestException("The student role is not configured in the system.");

            var user = new User
            {
                Email = email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                RoleId = studentRole.Id,
                Role = studentRole,
                Status = UserStatus.PENDING_APPROVAL,
                Student = new Student
                {
                    StudentCode = studentCode,
                    FullName = request.FullName.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                    University = request.University.Trim(),
                    Major = request.Major.Trim()
                }
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return ToStatusResponse(user, user.Student!);
        }

        public async Task<StudentRegistrationStatusResponseDto> RegisterWithApplicationAsync(
            StudentRegistrationApplicationRequestDto request)
        {
            var cv = await StudentDocumentFileValidator.InspectAsync(request.Cv);
            if (!cv.IsValid)
                throw new BadRequestException($"CV: {cv.Error}");

            var letter = await StudentDocumentFileValidator.InspectAsync(request.InternshipLetter);
            if (!letter.IsValid)
                throw new BadRequestException($"Đơn xin thực tập: {letter.Error}");

            var email = request.Email.Trim().ToLowerInvariant();
            var studentCode = request.StudentCode.Trim().ToUpperInvariant();
            ValidateRequiredProfile(request.FullName, request.University, request.Major, studentCode);
            await EnsureRegistrationIsUniqueAsync(email, studentCode);

            var program = await _db.InternshipPrograms
                .Include(item => item.Department)
                .SingleOrDefaultAsync(item => item.Id == request.ProgramId);
            if (program == null)
                throw new BadRequestException("Chương trình thực tập không tồn tại.");

            var studentRole = await _db.Roles.SingleOrDefaultAsync(role => role.Name == StudentRoleName);
            if (studentRole == null)
                throw new BadRequestException("The student role is not configured in the system.");

            var student = new Student
            {
                StudentCode = studentCode,
                FullName = request.FullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                University = request.University.Trim(),
                Major = request.Major.Trim(),
                ProgramId = program.Id,
                Program = program
            };
            var user = new User
            {
                Email = email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                RoleId = studentRole.Id,
                Role = studentRole,
                Status = UserStatus.PENDING_APPROVAL,
                Student = student
            };

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                var uploadedAt = DateTime.UtcNow;
                _db.StudentDocuments.AddRange(
                    CreateDocument(student.Id, "CV", cv, uploadedAt),
                    CreateDocument(student.Id, "INTERNSHIP_LETTER", letter, uploadedAt));
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return ToStatusResponse(user, student);
        }

        private async Task EnsureRegistrationIsUniqueAsync(string email, string studentCode)
        {
            if (await _db.Users.AnyAsync(user => user.Email.ToLower() == email))
                throw new ConflictException("This email is already registered.");
            if (await _db.Students.AnyAsync(student => student.StudentCode.ToLower() == studentCode.ToLower()))
                throw new ConflictException("This student code is already registered.");
        }

        private static void ValidateRequiredProfile(string fullName, string university, string major, string studentCode)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(university) ||
                string.IsNullOrWhiteSpace(major) || string.IsNullOrWhiteSpace(studentCode))
                throw new BadRequestException("Student profile fields cannot be blank.");
        }

        private static StudentDocument CreateDocument(
            int studentId,
            string documentType,
            StudentDocumentFileInspection file,
            DateTime uploadedAt) => new()
        {
            StudentId = studentId,
            DocumentType = documentType,
            OriginalFileName = file.FileName!,
            StoredFileName = $"db-{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}",
            ContentType = file.ContentType!,
            SizeBytes = file.Content!.LongLength,
            FileContent = file.Content,
            UploadedAt = uploadedAt
        };

        public async Task<StudentRegistrationStatusResponseDto> GetOwnStatusAsync(LoginRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _db.Users
                .Include(item => item.Role)
                .Include(item => item.Student)
                .SingleOrDefaultAsync(item => item.Email.ToLower() == email);

            if (user == null ||
                !_passwordHasher.Verify(request.Password, user.PasswordHash) ||
                !string.Equals(user.Role?.Name, StudentRoleName, StringComparison.OrdinalIgnoreCase) ||
                user.Student == null)
            {
                throw new UnauthorizedException("Invalid email or password.");
            }

            return ToStatusResponse(user, user.Student);
        }

        public async Task<IReadOnlyList<StudentRegistrationReviewDto>> GetPendingAsync()
        {
            var students = await _db.Students
                .AsNoTracking()
                .Include(student => student.User)
                    .ThenInclude(user => user!.Role)
                .Include(student => student.Program)
                .Where(student => student.User != null &&
                                  student.User.Role.Name == StudentRoleName &&
                                  student.User.Status == UserStatus.PENDING_APPROVAL)
                .OrderBy(student => student.CreatedAt)
                .ToListAsync();

            return students.Select(ToReviewDto).ToArray();
        }

        public async Task<StudentRegistrationReviewDto> GetPendingDetailsAsync(int studentId)
        {
            var student = await GetStudentForReviewAsync(studentId);
            if (student.User!.Status != UserStatus.PENDING_APPROVAL)
                throw new ConflictException("This student registration is no longer awaiting approval.");

            return ToReviewDto(student);
        }

        public Task<StudentRegistrationReviewDto> ApproveAsync(int studentId, int reviewerUserId) =>
            TransitionAsync(studentId, reviewerUserId, UserStatus.ACTIVE);

        public Task<StudentRegistrationReviewDto> RejectAsync(
            int studentId,
            int reviewerUserId,
            string? rejectionReason = null) =>
            TransitionAsync(studentId, reviewerUserId, UserStatus.REJECTED, rejectionReason);

        private async Task<StudentRegistrationReviewDto> TransitionAsync(
            int studentId,
            int reviewerUserId,
            UserStatus targetStatus,
            string? rejectionReason = null)
        {
            var student = await GetStudentForReviewAsync(studentId);
            if (student.User!.Status != UserStatus.PENDING_APPROVAL)
                throw new ConflictException("This student registration is no longer awaiting approval.");

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var changed = await _db.Users
                    .Where(user => user.Id == student.UserId && user.Status == UserStatus.PENDING_APPROVAL)
                    .ExecuteUpdateAsync(update => update.SetProperty(user => user.Status, targetStatus));

                if (changed != 1)
                    throw new ConflictException("This student registration has already been reviewed.");

                student.User.Status = targetStatus;
                student.ReviewedByUserId = reviewerUserId;
                student.ReviewedAt = DateTime.UtcNow;
                student.RejectionReason = targetStatus == UserStatus.REJECTED &&
                    !string.IsNullOrWhiteSpace(rejectionReason)
                        ? rejectionReason.Trim()
                        : null;
                await _notificationService.AddRegistrationReviewedAsync(student, targetStatus == UserStatus.ACTIVE);
                await _emailLogQueue.QueueDecisionEmailAsync(student, targetStatus == UserStatus.ACTIVE);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return ToReviewDto(student);
        }

        private async Task<Student> GetStudentForReviewAsync(int studentId)
        {
            var student = await _db.Students
                .Include(item => item.User)
                    .ThenInclude(user => user!.Role)
                .Include(item => item.Program)
                .SingleOrDefaultAsync(item => item.Id == studentId);

            if (student?.User == null || !string.Equals(student.User.Role?.Name, StudentRoleName, StringComparison.OrdinalIgnoreCase))
                throw new NotFoundException($"Student registration {studentId} was not found.");

            return student;
        }

        private static StudentRegistrationStatusResponseDto ToStatusResponse(User user, Student student)
        {
            var status = user.Status switch
            {
                UserStatus.ACTIVE => "APPROVED",
                UserStatus.PENDING_APPROVAL => "PENDING_APPROVAL",
                UserStatus.REJECTED => "REJECTED",
                _ => user.Status.ToString()
            };

            var message = status switch
            {
                "APPROVED" => "Hồ sơ đăng ký thực tập của bạn đã được HR xét duyệt.",
                "REJECTED" => "Hồ sơ đăng ký thực tập của bạn chưa được HR duyệt. Vui lòng liên hệ HR để được hỗ trợ.",
                _ => "Hồ sơ đăng ký của bạn đang chờ HR xét duyệt."
            };

            return new StudentRegistrationStatusResponseDto
            {
                StudentId = student.Id,
                Email = user.Email,
                FullName = student.FullName,
                Status = status,
                Message = message
            };
        }

        private static StudentRegistrationReviewDto ToReviewDto(Student student) => new()
        {
            StudentId = student.Id,
            UserId = student.UserId!.Value,
            Email = student.User!.Email,
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            PhoneNumber = student.PhoneNumber,
            University = student.University,
            Major = student.Major,
            ProgramName = student.Program?.Name,
            Status = student.User.Status == UserStatus.ACTIVE ? "APPROVED" : student.User.Status.ToString(),
            SubmittedAt = student.CreatedAt,
            ReviewedByUserId = student.ReviewedByUserId,
            ReviewedAt = student.ReviewedAt,
            RejectionReason = student.RejectionReason
        };
    }
}
