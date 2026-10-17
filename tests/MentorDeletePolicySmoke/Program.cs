using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Mentor;
using InternshipManagementApi.Repositories;
using InternshipManagementApi.Services;
using Microsoft.EntityFrameworkCore;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseMySql(
        "Server=127.0.0.1;Port=1;Database=design_time_only",
        new MySqlServerVersion(new Version(8, 0, 36)))
    .Options;

using (var db = new AppDbContext(options))
{
    var taskMentorForeignKey = db.Model.FindEntityType(typeof(InternshipTask))!
        .GetForeignKeys()
        .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Mentor));

    Assert(taskMentorForeignKey.DeleteBehavior == DeleteBehavior.Restrict,
        "Deleting a Mentor must be restricted while InternshipTask rows reference it.");
    Assert(taskMentorForeignKey.GetConstraintName() == "fk_sprint2_internship_tasks_mentor",
        "EF must retain the bootstrap FK name so the forward migration targets the expected constraint.");
}

var repository = new FakeMentorRepository(hasWorkReferences: true);
var service = new MentorService(repository, null!, null!, null!, null!);

try
{
    await service.DeleteMentorAsync(7);
    throw new InvalidOperationException("Expected deletion with work references to be rejected.");
}
catch (ConflictException)
{
    Assert(repository.DeleteCalls == 0, "Mentor deletion must not be attempted while task/history references exist.");
}

Console.WriteLine("Mentor deletion policy smoke tests passed.");

sealed class FakeMentorRepository(bool hasWorkReferences) : IMentorRepository
{
    public int DeleteCalls { get; private set; }

    public Task<Mentor?> GetByIdAsync(int id) => Task.FromResult<Mentor?>(new Mentor { Id = id });
    public Task<Mentor?> GetByIdWithDetailsAsync(int id) => Task.FromResult<Mentor?>(null);
    public Task<Mentor?> GetByUserIdAsync(int userId) => Task.FromResult<Mentor?>(null);
    public Task<bool> ExistsByUserIdAsync(int userId, int? excludeId = null) => Task.FromResult(false);
    public Task<bool> HasWorkReferencesAsync(int mentorId) => Task.FromResult(hasWorkReferences);
    public Task<IEnumerable<Mentor>> GetAllWithDetailsAsync() => Task.FromResult<IEnumerable<Mentor>>(Array.Empty<Mentor>());
    public Task<Mentor> AddAsync(Mentor mentor) => Task.FromResult(mentor);
    public Task UpdateAsync(Mentor mentor) => Task.CompletedTask;

    public Task DeleteAsync(Mentor mentor)
    {
        DeleteCalls++;
        return Task.CompletedTask;
    }
}
