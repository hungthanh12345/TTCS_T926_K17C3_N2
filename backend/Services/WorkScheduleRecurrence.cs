namespace InternshipManagementApi.Services;

public static class WorkScheduleRecurrenceType
{
    public const string None = "NONE";
    public const string Weekly = "WEEKLY";
}

public sealed record WorkScheduleRecurrenceSettings(
    DateOnly? StartDate,
    string? Type,
    int? Weeks,
    DateOnly? EndDate);

public static class WorkScheduleRecurrence
{
    public const int MaximumWeeks = 52;

    public static bool TryResolve(
        DateOnly? startDate,
        string? recurrenceType,
        int? recurrenceWeeks,
        out WorkScheduleRecurrenceSettings? settings,
        out string? error)
    {
        settings = null;
        error = null;
        if (!startDate.HasValue)
        {
            error = "Vui lòng chọn ngày bắt đầu áp dụng.";
            return false;
        }

        if (recurrenceType == WorkScheduleRecurrenceType.None)
        {
            if (recurrenceWeeks.HasValue && recurrenceWeeks.Value != 1)
            {
                error = "Lịch không lặp chỉ áp dụng trong một tuần.";
                return false;
            }

            if (!TryCalculateEndDate(startDate.Value, 1, out var endDate))
            {
                error = "Ngày kết thúc vượt phạm vi hợp lệ.";
                return false;
            }

            settings = new WorkScheduleRecurrenceSettings(startDate, recurrenceType, 1, endDate);
            return true;
        }

        if (recurrenceType != WorkScheduleRecurrenceType.Weekly)
        {
            error = "Kiểu lặp lịch không hợp lệ.";
            return false;
        }

        if (recurrenceWeeks.HasValue && recurrenceWeeks.Value is < 1 or > MaximumWeeks)
        {
            error = "Số tuần lặp phải từ 1 đến 52.";
            return false;
        }

        DateOnly? weeklyEndDate = null;
        if (recurrenceWeeks.HasValue)
        {
            if (!TryCalculateEndDate(startDate.Value, recurrenceWeeks.Value, out var endDate))
            {
                error = "Ngày kết thúc vượt phạm vi hợp lệ.";
                return false;
            }
            weeklyEndDate = endDate;
        }

        settings = new WorkScheduleRecurrenceSettings(startDate, recurrenceType, recurrenceWeeks, weeklyEndDate);
        return true;
    }

    public static bool IsEffectiveOn(DateOnly? startDate, DateOnly? endDate, DateOnly date) =>
        (!startDate.HasValue || date >= startDate.Value) && (!endDate.HasValue || date <= endDate.Value);

    public static bool IsScheduledOn(DateOnly? startDate, DateOnly? endDate, int dayOfWeek, DateOnly date) =>
        dayOfWeek == (int)date.DayOfWeek && IsEffectiveOn(startDate, endDate, date);

    private static bool TryCalculateEndDate(DateOnly startDate, int weeks, out DateOnly endDate)
    {
        var daysUntilSunday = (7 - (int)startDate.DayOfWeek) % 7;
        try
        {
            endDate = startDate.AddDays(daysUntilSunday + (weeks - 1) * 7);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            endDate = default;
            return false;
        }
    }
}
