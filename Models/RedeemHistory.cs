using System.ComponentModel.DataAnnotations.Schema;

namespace SonicPoints.Models
{
    public class RedeemHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; }
        public int RedeemableItemId { get; set; }
        public RedeemableItem RedeemableItem { get; set; }

        public int Quantity { get; set; }         
        public int PointsUsed { get; set; }
        public DateTime RedeemedOn { get; set; }
    }

}
