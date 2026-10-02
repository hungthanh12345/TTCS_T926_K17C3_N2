using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Sprint2
{
    public sealed class InternshipProgramDatesRequest : IValidatableObject
    {
        [Required]
        public DateOnly? StartDate { get; set; }

        [Required]
        public DateOnly? EndDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && EndDate.HasValue && EndDate.Value <= StartDate.Value)
            {
                yield return new ValidationResult(
                    "Ngày kết thúc phải sau ngày bắt đầu.",
                    new[] { nameof(EndDate) });
            }
        }
    }

    public sealed record InternshipProgramDatesResponseDto(
        int Id,
        DateOnly? StartDate,
        DateOnly? EndDate,
        DateTime UpdatedAt);
}
