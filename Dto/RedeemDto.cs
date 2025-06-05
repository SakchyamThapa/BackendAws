namespace SonicPoints.Dto
{
    public class RedeemDto
    {
        public int RedeemableItemId { get; set; }
        public int ProjectId { get; set; }
        public string UserId { get; set; }

        public int Quantity { get; set; }          
        public int PointsUsed { get; set; }
        public DateTime RedeemedAt { get; set; }
    }


}
