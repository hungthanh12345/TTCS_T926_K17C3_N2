using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.DTOs.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/admin/system-settings")]
    [Authorize(Roles = "ROLE_ADMIN")]
    [Produces("application/json")]
    public sealed class SystemSettingsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SystemSettingsController(AppDbContext db) => _db = db;

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<SystemSettingsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            if (!await _db.Database.CanConnectAsync(cancellationToken))
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse.Fail("Database is unavailable."));

            var programs = await _db.InternshipPrograms
                .AsNoTracking()
                .OrderByDescending(program => program.StartDate)
                .ThenBy(program => program.Name)
                .Select(program => new InternshipProgramSettingDto
                {
                    Id = program.Id,
                    Name = program.Name,
                    DepartmentName = program.Department.Name,
                    StartDate = program.StartDate,
                    EndDate = program.EndDate,
                    StudentCount = program.Students.Count
                })
                .ToListAsync(cancellationToken);

            var settings = new SystemSettingsDto
            {
                DatabaseStatus = "Available",
                AccountCount = await _db.Users.CountAsync(cancellationToken),
                StudentProfileCount = await _db.Students.CountAsync(cancellationToken),
                MentorProfileCount = await _db.Mentors.CountAsync(cancellationToken),
                PendingRegistrationCount = await _db.Users.CountAsync(user =>
                    user.Role.Name == "ROLE_STUDENT" && user.Status == Data.Entities.UserStatus.PENDING_APPROVAL, cancellationToken),
                AdminContacts = await _db.Users.AsNoTracking()
                    .Where(user => user.Role.Name == "ROLE_ADMIN")
                    .OrderBy(user => user.Email)
                    .Select(user => user.Email)
                    .ToListAsync(cancellationToken),
                Universities = await _db.Students.AsNoTracking()
                    .Where(student => student.University != null && student.University.Trim() != string.Empty)
                    .Select(student => student.University)
                    .Distinct()
                    .OrderBy(university => university)
                    .ToListAsync(cancellationToken),
                InternshipPrograms = programs
            };

            return Ok(ApiResponse<SystemSettingsDto>.Ok(settings, "System information retrieved successfully."));
        }
    }
}
