namespace InternshipManagementApi.DTOs.Student
{
    public sealed record StudentContractResponse(
        int Id,
        string ContractNumber,
        string Status,
        string? ProgramName,
        DateOnly? StartDate,
        DateOnly? EndDate,
        string? MentorName,
        string? FileName,
        bool HasFile,
        DateTime IssuedAt,
        DateTime? ConfirmedAt);
}
