using System.Text.Json.Serialization;

namespace SonicPoints.Models
{
    public class RedeemableItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }  // Points required
        public int ProjectId { get; set; } // Only available in the specific project

        public string ImageUrl { get; set; } // ✅ URL to the image file

        [JsonIgnore]
        public Project Project { get; set; }
    }

}
