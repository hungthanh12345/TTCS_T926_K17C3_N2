namespace InternshipManagementApi.DTOs.Notifications
{
    public sealed class NotificationDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
