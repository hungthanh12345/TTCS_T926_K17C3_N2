namespace InternshipManagementApi.DTOs.Admin
{
    public sealed class SystemSettingsDto
    {
        public string DatabaseStatus { get; set; } = "Unavailable";
        public int AccountCount { get; set; }
        public int StudentProfileCount { get; set; }
        public int MentorProfileCount { get; set; }
        public int PendingRegistrationCount { get; set; }
        public IReadOnlyList<string> AdminContacts { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> Universities { get; set; } = Array.Empty<string>();
        public IReadOnlyList<InternshipProgramSettingDto> InternshipPrograms { get; set; } = Array.Empty<InternshipProgramSettingDto>();
    }

    public sealed class InternshipProgramSettingDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int StudentCount { get; set; }
    }
}
