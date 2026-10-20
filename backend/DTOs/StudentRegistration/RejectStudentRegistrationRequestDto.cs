using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.StudentRegistration;

public sealed class RejectStudentRegistrationRequestDto
{
    [StringLength(1000)]
    public string? RejectionReason { get; set; }
}
