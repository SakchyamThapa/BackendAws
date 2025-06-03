using System.ComponentModel.DataAnnotations;

namespace SonicPoints.DTOs
{
    public class AssignUserRoleDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string AdminId { get; set; }

        [Required]
        [RegularExpression("Admin|Manager|Checker|Member", ErrorMessage = "Invalid role.")]
        public string Role { get; set; }
    }
}
