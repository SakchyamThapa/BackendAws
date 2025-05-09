using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SonicPoints.Models
{
    [Index(nameof(Email), IsUnique = true)]
    public class User : IdentityUser
    {
        public List<TaskItem> Tasks { get; set; }
        public List<ProjectUser> ProjectUsers { get; set; }
        public List<Leaderboard> TaskCompletions { get; set; }
        public List<RedeemHistory> RedeemHistories { get; set; }
    }

}
