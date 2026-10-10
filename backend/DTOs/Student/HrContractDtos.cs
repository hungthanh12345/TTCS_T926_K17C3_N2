namespace InternshipManagementApi.DTOs.Student
{
    public sealed record HrContractResponse(
        int StudentId,
        string StudentCode,
        string StudentName,
        string? ProgramName,
        int? ContractId,
        string? ContractNumber,
        string? Status,
        string? FileName,
        long SizeBytes,
        bool HasFile,
        DateTime? FileUploadedAt,
        DateTime? ConfirmedAt);

    public sealed class HrContractUploadRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public IFormFile? File { get; set; }
    }
}
