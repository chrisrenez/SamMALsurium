namespace SamMALsurium.Models.ViewModels.Admin
{
    public class DashboardViewModel
    {
        public UserMetrics Users { get; set; } = new();
        public ContentMetrics Content { get; set; } = new();
        public SystemHealthMetrics SystemHealth { get; set; } = new();
    }

    public class UserMetrics
    {
        public int TotalMembers { get; set; }
        public int PendingApprovals { get; set; }
        public int NewThisMonth { get; set; }
        public int ActiveUsers { get; set; }
    }

    public class ContentMetrics
    {
        public int TotalArtworks { get; set; }
        public int TotalEvents { get; set; }
        public int TotalSurveys { get; set; }
        public List<RecentActivityItem> RecentActivity { get; set; } = new();
    }

    public class RecentActivityItem
    {
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string AdminName { get; set; } = string.Empty;
    }

    public class SystemHealthMetrics
    {
        public string DatabaseStatus { get; set; } = string.Empty;
        public long StorageUsedBytes { get; set; }
        public long StorageAvailableBytes { get; set; }
        public int RecentErrors { get; set; }

        public string StorageUsedFormatted => FormatBytes(StorageUsedBytes);
        public string StorageAvailableFormatted => FormatBytes(StorageAvailableBytes);
        public double StorageUsedPercentage => StorageAvailableBytes > 0
            ? (double)StorageUsedBytes / (StorageUsedBytes + StorageAvailableBytes) * 100
            : 0;

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
