namespace SonicPoints.Models
{
    public class Report
    {
        public int ReportId { get; set; } // Primary Key
        public string UserId { get; set; } // Foreign Key
        public string ReportContent { get; set; }
        public DateTime GeneratedAt { get; set; }

        // Navigation property
        public User User { get; set; }
    }
}
