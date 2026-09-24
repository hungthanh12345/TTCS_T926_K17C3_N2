using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Student
{
    public class StudentSearchFilterDto
    {
        public string? University { get; set; }
        public string? Major { get; set; }
        public string? FullName { get; set; }
        public string? Keyword { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than or equal to 1.")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 10;
    }
}
