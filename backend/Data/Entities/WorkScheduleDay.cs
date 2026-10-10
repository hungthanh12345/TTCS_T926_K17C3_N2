
namespace InternshipManagementApi.Data.Entities;

public class WorkScheduleDay
{
    public int Id { get; set; }

    public int WorkScheduleId { get; set; }

    // 1 = Thu Hai, ..., 7 = Chu Nhat
    public int DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public TimeSpan? BreakStart { get; set; }

    public TimeSpan? BreakEnd { get; set; }

    public WorkSchedule WorkSchedule { get; set; } = null!;
}
