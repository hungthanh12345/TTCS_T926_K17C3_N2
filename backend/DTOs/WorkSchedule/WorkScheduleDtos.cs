
using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.WorkSchedule;

public class WorkScheduleDayRequestDto : IValidatableObject
{
    [Range(1, 7, ErrorMessage = "Ngày làm việc phải từ 1 đến 7.")]
    public int DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public TimeSpan? BreakStart { get; set; }

    public TimeSpan? BreakEnd { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();
        var oneDay = TimeSpan.FromDays(1);

        if (StartTime < TimeSpan.Zero || StartTime >= oneDay)
        {
            errors.Add(new ValidationResult(
                "Giờ bắt đầu phải nằm trong khoảng 00:00 đến trước 24:00.",
                new[] { nameof(StartTime) }));
        }

        if (EndTime < TimeSpan.Zero || EndTime >= oneDay)
        {
            errors.Add(new ValidationResult(
                "Giờ kết thúc phải nằm trong khoảng 00:00 đến trước 24:00.",
                new[] { nameof(EndTime) }));
        }

        if (StartTime >= TimeSpan.Zero &&
            StartTime < oneDay &&
            EndTime >= TimeSpan.Zero &&
            EndTime < oneDay &&
            EndTime <= StartTime)
        {
            errors.Add(new ValidationResult(
                "Giờ kết thúc phải sau giờ bắt đầu.",
                new[] { nameof(EndTime) }));
        }

        // Neu co gio nghi thi phai nhap ca hai moc.
        if (BreakStart.HasValue != BreakEnd.HasValue)
        {
            errors.Add(new ValidationResult(
                "Phải nhập cả giờ bắt đầu nghỉ và giờ kết thúc nghỉ.",
                new[] { nameof(BreakStart), nameof(BreakEnd) }));
        }

        // Kiem tra khoang nghi nam hoan toan trong ca lam.
        if (BreakStart.HasValue && BreakEnd.HasValue)
        {
            if (StartTime < TimeSpan.Zero ||
                StartTime >= oneDay ||
                EndTime < TimeSpan.Zero ||
                EndTime >= oneDay ||
                EndTime <= StartTime)
            {
                errors.Add(new ValidationResult(
                    "Cần thiết lập ca làm hợp lệ trước khi khai báo giờ nghỉ.",
                    new[] { nameof(BreakStart), nameof(BreakEnd) }));
            }
            else if (BreakStart.Value < TimeSpan.Zero ||
                     BreakStart.Value >= oneDay ||
                     BreakEnd.Value < TimeSpan.Zero ||
                     BreakEnd.Value >= oneDay)
            {
                errors.Add(new ValidationResult(
                    "Giờ nghỉ phải nằm trong khoảng 00:00 đến trước 24:00.",
                    new[] { nameof(BreakStart), nameof(BreakEnd) }));
            }
            else if (BreakStart.Value <= StartTime ||
                     BreakEnd.Value >= EndTime ||
                     BreakStart.Value >= BreakEnd.Value)
            {
                errors.Add(new ValidationResult(
                    "Giờ nghỉ phải bắt đầu sau giờ vào, kết thúc trước giờ tan ca và giờ kết thúc nghỉ phải sau giờ bắt đầu nghỉ.",
                    new[] { nameof(BreakStart), nameof(BreakEnd) }));
            }
        }

        return errors;
    }
}

public class WorkScheduleRequestDto : IValidatableObject
{
    [Required(ErrorMessage = "Tên lịch làm việc là bắt buộc.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Tên lịch phải có từ 2 đến 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đối tượng áp dụng là bắt buộc.")]
    [RegularExpression("^(GROUP|STUDENT)$",
        ErrorMessage = "Đối tượng áp dụng phải là GROUP hoặc STUDENT.")]
    public string TargetType { get; set; } = "GROUP";

    [StringLength(100,
        ErrorMessage = "Tên nhóm không được vượt quá 100 ký tự.")]
    public string? GroupName { get; set; }

    public int? StudentId { get; set; }

    public bool IsFlexible { get; set; }

    [Required(ErrorMessage = "Danh sách ngày làm việc là bắt buộc.")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất một ngày làm việc.")]
    public List<WorkScheduleDayRequestDto> Days { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(Name) ||
            Name.Trim().Length < 2)
        {
            errors.Add(new ValidationResult(
                "Tên lịch làm việc phải có ít nhất 2 ký tự không phải khoảng trắng.",
                new[] { nameof(Name) }));
        }

        if (TargetType == "GROUP" &&
            string.IsNullOrWhiteSpace(GroupName))
        {
            errors.Add(new ValidationResult(
                "Vui lòng nhập tên nhóm áp dụng.",
                new[] { nameof(GroupName) }));
        }

        if (TargetType == "STUDENT" &&
            (!StudentId.HasValue || StudentId.Value <= 0))
        {
            errors.Add(new ValidationResult(
                "Vui lòng chọn thực tập sinh hợp lệ.",
                new[] { nameof(StudentId) }));
        }

        if (Days == null || Days.Count == 0)
        {
            errors.Add(new ValidationResult(
                "Lịch làm việc phải có ít nhất một ngày.",
                new[] { nameof(Days) }));

            return errors;
        }

        // Khong cho phep trung ngay trong cung mot lich.
        var duplicateDays = Days
            .GroupBy(d => d.DayOfWeek)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateDays.Count > 0)
        {
            errors.Add(new ValidationResult(
                $"Ngày làm việc bị trùng: {string.Join(", ", duplicateDays)}.",
                new[] { nameof(Days) }));
        }

        return errors;
    }
}

public class WorkScheduleResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string? GroupName { get; set; }

    public int? StudentId { get; set; }

    public bool IsFlexible { get; set; }

    public string Status { get; set; } = "ACTIVE";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<WorkScheduleDayRequestDto> Days { get; set; } = new();
}
