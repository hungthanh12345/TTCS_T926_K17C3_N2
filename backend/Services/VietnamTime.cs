namespace InternshipManagementApi.Services;

internal static class VietnamTime
{
    private static readonly Lazy<TimeZoneInfo> Zone = new(() =>
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    });

    public static DateTime ToLocal(DateTime utcNow) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), Zone.Value);

    public static DateOnly Today(DateTime utcNow) =>
        DateOnly.FromDateTime(ToLocal(utcNow));

    public static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, Zone.Value);
    }

    public static DateTime ToUtcStartOfDay(DateOnly date) => ToUtc(date, TimeOnly.MinValue);
}
