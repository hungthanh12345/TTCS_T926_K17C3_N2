using InternshipManagementApi.Services;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var start = new DateOnly(2026, 10, 13); // Tuesday
Assert(WorkScheduleRecurrence.TryResolve(start, WorkScheduleRecurrenceType.None, null, out var once, out _), "One-time schedule should resolve.");
Assert(once!.Weeks == 1 && once.EndDate == new DateOnly(2026, 10, 18), "One-time schedule should end on Sunday of its effective week.");
foreach (var weeks in new[] { 1, 2, 4, 8, 12 })
{
    Assert(WorkScheduleRecurrence.TryResolve(start, WorkScheduleRecurrenceType.Weekly, weeks, out var finite, out _), $"Finite {weeks}-week schedule should resolve.");
    Assert(finite!.EndDate == start.AddDays(5 + (weeks - 1) * 7), $"Finite {weeks}-week end date should be correct.");
}
Assert(WorkScheduleRecurrence.TryResolve(start, WorkScheduleRecurrenceType.Weekly, null, out var forever, out _), "Unbounded weekly schedule should resolve.");
Assert(forever!.EndDate is null, "Unbounded schedule should have no end date.");
Assert(WorkScheduleRecurrence.IsScheduledOn(start, forever.EndDate, (int)DayOfWeek.Tuesday, start.AddDays(7)), "Weekly schedule should repeat on the matching weekday.");
Assert(!WorkScheduleRecurrence.IsScheduledOn(start, forever.EndDate, (int)DayOfWeek.Monday, start.AddDays(7)), "Weekly schedule should not apply on another weekday.");
Assert(!WorkScheduleRecurrence.IsEffectiveOn(start, new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 26)), "Finite schedule should stop after its end.");
Assert(WorkScheduleRecurrence.IsEffectiveOn(null, null, new DateOnly(2036, 10, 13)), "Legacy schedules with no recurrence metadata remain effective.");
Assert(!WorkScheduleRecurrence.TryResolve(null, WorkScheduleRecurrenceType.Weekly, null, out _, out _), "Recurrence requires an explicit start.");
Assert(!WorkScheduleRecurrence.TryResolve(start, WorkScheduleRecurrenceType.Weekly, 53, out _, out _), "More than 52 weeks must be rejected.");
Assert(!WorkScheduleRecurrence.TryResolve(start, "OTHER", null, out _, out _), "Unknown recurrence type must be rejected.");
Assert(!WorkScheduleRecurrence.TryResolve(new DateOnly(9999, 12, 31), WorkScheduleRecurrenceType.None, null, out _, out _), "Overflow end date must be rejected.");
Console.WriteLine("US23 recurrence smoke tests passed.");