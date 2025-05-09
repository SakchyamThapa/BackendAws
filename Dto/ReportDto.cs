namespace SonicPoints.Dto
{
    public class ReportDto
    {
        public int ReportId { get; set; }
        public int UserId { get; set; }
        public string ReportContent { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string User { get; set; }
    }
}
