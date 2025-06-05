namespace SonicPoints.Dto
{
    public class LeaderboardDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int PointsEarned { get; set; }
        public int TaskCompletionCount { get; set; }
        public int RedeemedPoints { get; set; }
        public DateTime? DateCompleted { get; set; }  // Add the DateCompleted field
        public int LeaderboardRank { get; set; }
        public int TotalPoints { get; set; }

        public double ProjectProgress { get; set; }
        public int RedeemablePoints { get; set; }
    }
}
