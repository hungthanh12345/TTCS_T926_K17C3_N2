using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Hr
{
    public class AttendanceSummaryDto
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int TotalPresent { get; set; }
        public int TotalLate { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalLeaveApproved { get; set; }
    }

    public class AttendanceReportRequestDto
    {
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? StudentId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
