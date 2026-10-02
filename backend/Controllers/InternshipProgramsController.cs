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
                    program.CreatedAt,
                    program.UpdatedAt))
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<InternshipProgramResponseDto>>.Ok(programs));
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
                program.CreatedAt,
                program.UpdatedAt);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<InternshipProgramResponseDto>.Created(response));
        }

        private static string? NormalizeDescription(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
