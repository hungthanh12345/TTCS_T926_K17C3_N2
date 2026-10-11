
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.WorkSchedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Controllers;

[ApiController]
[Route("api/hr/work-schedules")]
[Authorize]
public class WorkSchedulesController : ControllerBase
{
    private readonly AppDbContext _context;

    public WorkSchedulesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/hr/work-schedules
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var schedules = await _context.WorkSchedules
            .AsNoTracking()
            .Include(s => s.Days)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.TargetType,
                s.GroupName,
                s.StudentId,
                s.IsFlexible,
                s.Status,
                s.CreatedAt,
                s.UpdatedAt,
                Days = s.Days.OrderBy(d => d.DayOfWeek)
                    .Select(d => new
                    {
                        d.Id,
                        d.DayOfWeek,
                        d.StartTime,
                        d.EndTime,
                        d.BreakStart,
                        d.BreakEnd
                    }).ToList()
            })
            .ToListAsync();

        return Ok(schedules);
    }

    // GET: api/hr/work-schedules/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var schedule = await _context.WorkSchedules
            .AsNoTracking()
            .Include(s => s.Days)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
            return NotFound(new { message = "Khong tim thay lich lam viec." });

        return Ok(schedule);
    }

    // POST: api/hr/work-schedules
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] WorkScheduleRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (request.TargetType == "STUDENT")
        {
            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == request.StudentId);

            if (!studentExists)
                return BadRequest(new { message = "Thuc tap sinh khong ton tai." });
        }

        var schedule = new WorkSchedule
        {
            Name = request.Name.Trim(),
            TargetType = request.TargetType,
            GroupName = request.TargetType == "GROUP"
                ? request.GroupName?.Trim()
                : null,
            StudentId = request.TargetType == "STUDENT"
                ? request.StudentId
                : null,
            IsFlexible = request.IsFlexible,
            Status = "ACTIVE",
            Days = request.Days.Select(d => new WorkScheduleDay
            {
                DayOfWeek = d.DayOfWeek,
                StartTime = d.StartTime,
                EndTime = d.EndTime,
                BreakStart = d.BreakStart,
                BreakEnd = d.BreakEnd
            }).ToList()
        };

        _context.WorkSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = schedule.Id },
            schedule);
    }

    // PUT: api/hr/work-schedules/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] WorkScheduleRequestDto request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var schedule = await _context.WorkSchedules
            .Include(s => s.Days)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
            return NotFound(new { message = "Khong tim thay lich lam viec." });

        if (request.TargetType == "STUDENT")
        {
            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == request.StudentId);

            if (!studentExists)
                return BadRequest(new { message = "Thuc tap sinh khong ton tai." });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        schedule.Name = request.Name.Trim();
        schedule.TargetType = request.TargetType;
        schedule.GroupName = request.TargetType == "GROUP"
            ? request.GroupName?.Trim()
            : null;
        schedule.StudentId = request.TargetType == "STUDENT"
            ? request.StudentId
            : null;
        schedule.IsFlexible = request.IsFlexible;

        _context.WorkScheduleDays.RemoveRange(schedule.Days);
        schedule.Days = request.Days.Select(d => new WorkScheduleDay
        {
            WorkScheduleId = schedule.Id,
            DayOfWeek = d.DayOfWeek,
            StartTime = d.StartTime,
            EndTime = d.EndTime,
            BreakStart = d.BreakStart,
            BreakEnd = d.BreakEnd
        }).ToList();

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            message = "Cap nhat lich lam viec thanh cong.",
            schedule.Id,
            schedule.Name,
            schedule.TargetType,
            schedule.GroupName,
            schedule.StudentId,
            schedule.IsFlexible,
            schedule.Days
        });
    }

    // DELETE: api/hr/work-schedules/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var schedule = await _context.WorkSchedules
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
            return NotFound(new { message = "Khong tim thay lich lam viec." });

        // Khong xoa cung lich dang duoc tham chieu boi du lieu khac.
        _context.WorkSchedules.Remove(schedule);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Khong the xoa lich dang duoc su dung."
            });
        }

        return Ok(new { message = "Xoa lich lam viec thanh cong." });
    }
}
