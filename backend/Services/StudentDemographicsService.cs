using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Reports;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Services;

public interface IStudentDemographicsService
{
    Task<StudentDemographicsResponseDto> GetAsync(CancellationToken cancellationToken);
}

public sealed class StudentDemographicsService : IStudentDemographicsService
{
    private const string MissingValue = "Ch\u01B0a c\u1EADp nh\u1EADt";
    private readonly AppDbContext _db;

    public StudentDemographicsService(AppDbContext db) => _db = db;

    public async Task<StudentDemographicsResponseDto> GetAsync(CancellationToken cancellationToken)
    {
        var approvedStudents = _db.Students.AsNoTracking()
            .Where(student => student.ApplicationStatus == StudentApplicationStatus.Approved);

        var rows = await approvedStudents
            .GroupBy(student => new
            {
                University = student.University == null || student.University.Trim() == ""
                    ? MissingValue
                    : student.University.Trim(),
                Major = student.Major == null || student.Major.Trim() == ""
                    ? MissingValue
                    : student.Major.Trim()
            })
            .Select(group => new
            {
                group.Key.University,
                group.Key.Major,
                StudentCount = group.Count()
            })
            .OrderBy(row => row.University)
            .ThenBy(row => row.Major)
            .Select(row => new StudentDemographicPairDto
            {
                University = row.University,
                Major = row.Major,
                StudentCount = row.StudentCount
            })
            .ToListAsync(cancellationToken);

        return new StudentDemographicsResponseDto
        {
            TotalApprovedStudents = rows.Sum(row => row.StudentCount),
            Universities = rows.Select(row => row.University).Distinct().OrderBy(value => value).ToArray(),
            Majors = rows.Select(row => row.Major).Distinct().OrderBy(value => value).ToArray(),
            Items = rows
        };
    }
}
