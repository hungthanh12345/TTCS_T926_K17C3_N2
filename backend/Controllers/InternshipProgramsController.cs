using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Sprint2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/hr")]
    [Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]
    public sealed class InternshipProgramsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public InternshipProgramsController(AppDbContext db) => _db = db;

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _db.Departments.AsNoTracking()
                .OrderBy(department => department.Name)
                .Select(department => new DepartmentResponseDto(
                    department.Id, department.Name, department.Description))
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<DepartmentResponseDto>>.Ok(departments));
        }

        [HttpPost("departments")]
        public async Task<IActionResult> CreateDepartment([FromBody] DepartmentRequest request)
        {
            var name = request.Name.Trim();
            if (name.Length < 2)
                return BadRequest(ApiResponse.Fail("Department name must contain at least 2 characters."));

            if (await _db.Departments.AnyAsync(department => department.Name == name))
                return Conflict(ApiResponse.Fail("Department name already exists."));

            var department = new Department
            {
                Name = name,
                Description = NormalizeDescription(request.Description)
            };

            _db.Departments.Add(department);
            await _db.SaveChangesAsync();

            var response = new DepartmentResponseDto(department.Id, department.Name, department.Description);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<DepartmentResponseDto>.Created(response));
        }

        [HttpGet("programs")]
        public async Task<IActionResult> GetPrograms()
        {
            var programs = await _db.InternshipPrograms.AsNoTracking()
                .OrderByDescending(program => program.CreatedAt)
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
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<InternshipProgramResponseDto>>.Ok(programs));
        }

        [HttpPut("students/{studentId:int}/program")]
        public async Task<IActionResult> AssignStudentProgram(
            int studentId,
            [FromBody] InternshipProgramAssignmentRequestDto request)
        {
            var student = await _db.Students
                .Include(item => item.User)
                .SingleOrDefaultAsync(item => item.Id == studentId);
            if (student == null)
                return NotFound(ApiResponse.Fail("Student profile was not found."));

            InternshipProgram? program = null;
            if (request.ProgramId.HasValue)
            {
                program = await _db.InternshipPrograms.SingleOrDefaultAsync(item => item.Id == request.ProgramId.Value);
                if (program == null)
                    return NotFound(ApiResponse.Fail("Internship program was not found."));

                if (student.User?.Status is UserStatus.PENDING_APPROVAL or UserStatus.REJECTED)
                    return Conflict(ApiResponse.Fail("Student registration must be approved before program assignment."));
            }

            var requestedProgramId = program?.Id;
            if (student.ProgramId != requestedProgramId)
            {
                var changedRows = await _db.Students
                    .Where(item => item.Id == studentId && item.ProgramId == student.ProgramId)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(item => item.ProgramId, requestedProgramId));

                if (changedRows != 1)
                    return Conflict(ApiResponse.Fail("The student's program changed during assignment. Refresh and try again."));
            }

            var response = new InternshipProgramAssignmentResponseDto(student.Id, requestedProgramId, program?.Name);
            return Ok(ApiResponse<InternshipProgramAssignmentResponseDto>.Ok(response, "Student program assignment updated."));
        }

        [HttpPost("programs")]
        public async Task<IActionResult> CreateProgram([FromBody] InternshipProgramRequest request)
        {
            var name = request.Name.Trim();
            if (name.Length < 2)
                return BadRequest(ApiResponse.Fail("Program name must contain at least 2 characters."));

            var department = await _db.Departments.FindAsync(request.DepartmentId);
            if (department == null)
                return BadRequest(ApiResponse.Fail("Department was not found."));

            if (await _db.InternshipPrograms.AnyAsync(program =>
                    program.DepartmentId == request.DepartmentId && program.Name == name))
                return Conflict(ApiResponse.Fail("A program with this name already exists in the selected department."));

            var program = new InternshipProgram
            {
                Name = name,
                Description = NormalizeDescription(request.Description),
                DepartmentId = department.Id,
                Department = department
            };

            _db.InternshipPrograms.Add(program);
            await _db.SaveChangesAsync();

            var response = new InternshipProgramResponseDto(
                program.Id,
                program.Name,
                program.Description,
                program.DepartmentId,
                department.Name,
                program.StartDate,
                program.EndDate,
                program.CreatedAt,
                program.UpdatedAt);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<InternshipProgramResponseDto>.Created(response));
        }

        [HttpPut("programs/{programId:int}/dates")]
        public async Task<IActionResult> UpdateProgramDates(
            int programId,
            [FromBody] InternshipProgramDatesRequest request)
        {
            var program = await _db.InternshipPrograms.FindAsync(programId);
            if (program == null)
                return NotFound(ApiResponse.Fail("Internship program was not found."));

            program.StartDate = request.StartDate!.Value;
            program.EndDate = request.EndDate!.Value;
            await _db.SaveChangesAsync();

            var response = new InternshipProgramDatesResponseDto(
                program.Id,
                program.StartDate,
                program.EndDate,
                program.UpdatedAt);

            return Ok(ApiResponse<InternshipProgramDatesResponseDto>.Ok(response,
                "Internship program dates updated."));
        }

        private static string? NormalizeDescription(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
