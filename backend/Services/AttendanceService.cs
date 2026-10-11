using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Attendance;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace InternshipManagementApi.Services
{
    public interface IAttendanceService
    {
        Task<AttendanceResponseDto> CheckInAsync(int userId, CheckInRequestDto? request);
        Task<AttendanceResponseDto> CheckOutAsync(int userId, CheckOutRequestDto? request);
        Task<TodayAttendanceStatusDto> GetTodayStatusAsync(int userId);
        Task<AttendanceSummaryDto> GetOwnHistoryAsync(int userId, DateOnly? startDate = null, DateOnly? endDate = null, string? status = null);
        Task<AttendanceSummaryDto> GetStudentHistoryAsync(int studentId, DateOnly? startDate = null, DateOnly? endDate = null, string? status = null);
    }

    public sealed class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _db;

        public AttendanceService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<AttendanceResponseDto> CheckInAsync(int userId, CheckInRequestDto? request)
        {
            var student = await GetStudentByUserIdAsync(userId);
            var today = GetCurrentDate();

            var existing = await _db.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == student.Id && a.Date == today);

            if (existing != null)
            {
                throw new ConflictException("Hôm nay bạn đã thực hiện check-in rồi. Không thể check-in nhiều lần trong cùng một ngày.");
            }

            var now = DateTime.UtcNow;
            var attendance = new Attendance
            {
                StudentId = student.Id,
                Date = today,
                CheckInTime = now,
                CheckOutTime = null,
                DurationMinutes = null,
                Status = "CHECKED_IN",
                Notes = string.IsNullOrWhiteSpace(request?.Notes) ? null : request.Notes.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Attendances.Add(attendance);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                throw new ConflictException("Hôm nay bạn đã thực hiện check-in rồi. Không thể check-in nhiều lần trong cùng một ngày.");
            }

            return MapToDto(attendance, student);
        }

        public async Task<AttendanceResponseDto> CheckOutAsync(int userId, CheckOutRequestDto? request)
        {
            var student = await GetStudentByUserIdAsync(userId);
            var today = GetCurrentDate();

            var attendance = await _db.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == student.Id && a.Date == today);

            if (attendance == null)
            {
                throw new BadRequestException("Bạn chưa thực hiện check-in hôm nay. Không thể check-out trước khi check-in.");
            }

            if (attendance.CheckOutTime != null || attendance.Status == "COMPLETED")
            {
                throw new ConflictException("Bạn đã hoàn thành check-out cho ngày hôm nay rồi.");
            }

            var now = DateTime.UtcNow;
            if (now < attendance.CheckInTime)
            {
                throw new BadRequestException("Thời gian check-out không thể diễn ra trước thời gian check-in.");
            }

            var durationMinutes = (int)Math.Max(0, (now - attendance.CheckInTime).TotalMinutes);

            attendance.CheckOutTime = now;
            attendance.DurationMinutes = durationMinutes;
            attendance.Status = "COMPLETED";

            if (!string.IsNullOrWhiteSpace(request?.Notes))
            {
                var newNote = request.Notes.Trim();
                attendance.Notes = string.IsNullOrWhiteSpace(attendance.Notes)
                    ? newNote
                    : $"{attendance.Notes} | Checkout: {newNote}";
            }

            attendance.UpdatedAt = now;

            await _db.SaveChangesAsync();

            return MapToDto(attendance, student);
        }

        public async Task<TodayAttendanceStatusDto> GetTodayStatusAsync(int userId)
        {
            var student = await GetStudentByUserIdAsync(userId);
            var today = GetCurrentDate();

            var attendance = await _db.Attendances
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.StudentId == student.Id && a.Date == today);

            var isCheckedIn = attendance != null;
            var isCheckedOut = attendance?.CheckOutTime != null;

            return new TodayAttendanceStatusDto
            {
                IsCheckedIn = isCheckedIn,
                IsCheckedOut = isCheckedOut,
                TodayDate = today,
                ServerTime = DateTime.UtcNow.ToString("o"),
                CurrentAttendance = attendance != null ? MapToDto(attendance, student) : null
            };
        }

        public async Task<AttendanceSummaryDto> GetOwnHistoryAsync(
            int userId,
            DateOnly? startDate = null,
            DateOnly? endDate = null,
            string? status = null)
        {
            var student = await GetStudentByUserIdAsync(userId);
            return await QueryHistoryAsync(student.Id, student.FullName, student.StudentCode, startDate, endDate, status);
        }

        public async Task<AttendanceSummaryDto> GetStudentHistoryAsync(
            int studentId,
            DateOnly? startDate = null,
            DateOnly? endDate = null,
            string? status = null)
        {
            var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == studentId);
            if (student == null)
            {
                throw new NotFoundException($"Không tìm thấy hồ sơ sinh viên với mã {studentId}.");
            }

            return await QueryHistoryAsync(student.Id, student.FullName, student.StudentCode, startDate, endDate, status);
        }

        private async Task<AttendanceSummaryDto> QueryHistoryAsync(
            int studentId,
            string studentName,
            string studentCode,
            DateOnly? startDate,
            DateOnly? endDate,
            string? status)
        {
            var query = _db.Attendances
                .AsNoTracking()
                .Where(a => a.StudentId == studentId);

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Date <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim().ToUpperInvariant();
                query = query.Where(a => a.Status == normalizedStatus);
            }

            var records = await query
                .OrderByDescending(a => a.Date)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var dtos = records.Select(a => MapToDto(a, studentName, studentCode)).ToList();

            var totalDays = dtos.Count;
            var totalMinutes = dtos.Sum(a => a.DurationMinutes ?? 0);
            var completedDays = dtos.Count(a => a.Status == "COMPLETED");
            var pendingDays = dtos.Count(a => a.Status == "CHECKED_IN");

            return new AttendanceSummaryDto
            {
                TotalDays = totalDays,
                TotalMinutes = totalMinutes,
                TotalHoursFormatted = FormatHoursAndMinutes(totalMinutes),
                CompletedDays = completedDays,
                PendingDays = pendingDays,
                Records = dtos
            };
        }

        private async Task<Student> GetStudentByUserIdAsync(int userId)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null)
            {
                throw new NotFoundException("Không tìm thấy hồ sơ thực tập sinh tương ứng với tài khoản đăng nhập.");
            }
            return student;
        }

        private static DateOnly GetCurrentDate()
        {
            // Vietnam Time GMT+7
            return DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        }

        private static AttendanceResponseDto MapToDto(Attendance attendance, Student student)
        {
            return MapToDto(attendance, student.FullName, student.StudentCode);
        }

        private static AttendanceResponseDto MapToDto(Attendance attendance, string studentName, string studentCode)
        {
            return new AttendanceResponseDto
            {
                Id = attendance.Id,
                StudentId = attendance.StudentId,
                StudentName = studentName,
                StudentCode = studentCode,
                Date = attendance.Date,
                CheckInTime = attendance.CheckInTime,
                CheckOutTime = attendance.CheckOutTime,
                DurationMinutes = attendance.DurationMinutes,
                DurationFormatted = FormatDuration(attendance.DurationMinutes),
                Status = attendance.Status,
                Notes = attendance.Notes,
                CreatedAt = attendance.CreatedAt,
                UpdatedAt = attendance.UpdatedAt
            };
        }

        private static string FormatDuration(int? minutes)
        {
            if (!minutes.HasValue || minutes.Value <= 0) return "0 phút";
            var hours = minutes.Value / 60;
            var mins = minutes.Value % 60;
            if (hours > 0 && mins > 0) return $"{hours} giờ {mins} phút";
            if (hours > 0) return $"{hours} giờ";
            return $"{mins} phút";
        }

        private static string FormatHoursAndMinutes(int totalMinutes)
        {
            var hours = totalMinutes / 60;
            var mins = totalMinutes % 60;
            return $"{hours} giờ {mins} phút";
        }

        private static bool IsDuplicateKey(DbUpdateException ex)
        {
            return ex.InnerException is MySqlException mysqlEx && mysqlEx.Number == 1062;
        }
    }
}
