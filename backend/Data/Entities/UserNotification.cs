namespace InternshipManagementApi.Data.Entities
{
    public sealed class UserNotification
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string SourceKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }

        public User User { get; set; } = null!;
    }
}
