using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Sprint2
{
    public sealed class DepartmentRequest
    {
        [Required, MinLength(2), MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public sealed class InternshipProgramRequest
    {
        [Required, MinLength(2), MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(1, int.MaxValue)]
        public int DepartmentId { get; set; }
    }

    public sealed record DepartmentResponseDto(int Id, string Name, string? Description);

    public sealed record InternshipProgramResponseDto(
        int Id,
        string Name,
        string? Description,
        int DepartmentId,
        string DepartmentName,
        DateOnly? StartDate,
        DateOnly? EndDate,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed class InternshipProgramAssignmentRequestDto
    {
        [Range(1, int.MaxValue)]
        public int? ProgramId { get; set; }
    }

    public sealed record InternshipProgramAssignmentResponseDto(int StudentId, int? ProgramId, string? ProgramName);
}
