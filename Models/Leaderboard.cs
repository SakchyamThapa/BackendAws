namespace SonicPoints.Models
{
    public class Leaderboard
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public int TaskId { get; set; }
        public TaskItem Task { get; set; }
        public int PointsEarned { get; set; }  
        public int TaskCompletionCount { get; set; }
        public int RedeemedPoints { get; set; }
        public DateTime? DateCompleted { get; set; }
    }
}
