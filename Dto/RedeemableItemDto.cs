namespace SonicPoints.Dto
{
    namespace SonicPoints.Dto
    {
        public class RedeemableItemDto
        {
           
            public string Name { get; set; }
            public int Cost { get; set; }       // Points required to redeem
            public int ProjectId { get; set; }  // Project ID
            public IFormFile ImageUrl { get; set; } // ✅ Image URL (e.g., Cloudinary or Firebase)
        }
    }

}
