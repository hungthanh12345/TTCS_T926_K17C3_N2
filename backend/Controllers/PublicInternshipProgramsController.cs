using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Sprint2;
using InternshipManagementApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/programs")]
    [AllowAnonymous]
    [Produces("application/json")]
    public sealed class PublicInternshipProgramsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PublicInternshipProgramsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAvailablePrograms(CancellationToken cancellationToken)
        {
            var programs = await _db.InternshipPrograms.AsNoTracking()
                .OrderBy(program => program.Name)
                .Select(program => new InternshipProgramResponseDto(
                    program.Id,
                    program.Name,
                    program.Description,
                    program.DepartmentId,
                    program.Department.Name,
                    program.StartDate,
                    program.EndDate,
                    program.CreatedAt,
                    program.UpdatedAt))
                .ToListAsync(cancellationToken);

            return Ok(ApiResponse<IEnumerable<InternshipProgramResponseDto>>.Ok(programs));
        }
    }
}
