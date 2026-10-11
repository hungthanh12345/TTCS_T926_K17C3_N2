using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Attendance
{
    public class CheckInRequestDto
    {
        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }
    }

    public class CheckOutRequestDto
    {
        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }
    }

    public class AttendanceResponseDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int? DurationMinutes { get; set; }
        public string DurationFormatted { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TodayAttendanceStatusDto
    {
        public bool IsCheckedIn { get; set; }
        public bool IsCheckedOut { get; set; }
        public DateOnly TodayDate { get; set; }
        public string ServerTime { get; set; } = string.Empty;
        public AttendanceResponseDto? CurrentAttendance { get; set; }
    }

    public class AttendanceSummaryDto
    {
        public int TotalDays { get; set; }
        public int TotalMinutes { get; set; }
        public string TotalHoursFormatted { get; set; } = string.Empty;
        public int CompletedDays { get; set; }
        public int PendingDays { get; set; }
        public IReadOnlyList<AttendanceResponseDto> Records { get; set; } = Array.Empty<AttendanceResponseDto>();
    }
}
