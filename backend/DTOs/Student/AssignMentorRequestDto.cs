using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Student
{
    public class AssignMentorRequestDto
    {
        /// <summary>
        /// ID of the mentor to assign. Pass null or a valid mentor ID. To unassign, pass null.
        /// </summary>
        public int? MentorId { get; set; }
    }
}
