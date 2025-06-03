namespace SonicPoints.Models
{
    public class ProjectUser
    {
        public int Id { get; set; }  // Primary key for ProjectUser

        // Foreign key to Project
        public int ProjectId { get; set; }  
        public Project Project { get; set; }  // Navigation property to Project

       
        public string UserId { get; set; }
        public User User { get; set; }  // Navigation property to User

        public int RewardPoints { get; set; }  // Points earned in this project
        public string Role { get; set; }  // "Member", "Admin"
    }
}
