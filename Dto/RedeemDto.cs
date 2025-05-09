namespace SonicPoints.Dto
{
    public class RedeemDto
    {
        public int RedeemableItemId { get; set; }  // Correct ID field
        public string RedeemableItemName { get; set; }  // Correct naming
        public int PointsUsed { get; set; }
        public DateTime RedeemedAt { get; set; }
        public string UserId { get; set; }  // Ensure consistency (UserId is a string)
        public int ProjectId { get; set; } // Needed for filtering
    }

}
