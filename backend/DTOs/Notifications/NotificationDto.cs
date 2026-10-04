namespace InternshipManagementApi.DTOs.Notifications
{
    public sealed class NotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}
