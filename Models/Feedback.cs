
using System;
using System.ComponentModel.DataAnnotations;

namespace SonicPoints.Models
{
    public class Feedback
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Reason { get; set; }

        [Required]
        public string Type { get; set; }  // Complaint, Suggestion, Other

        [Range(1, 3)]
        public int Rating { get; set; }

        public DateTime SubmittedAt { get; set; }

        public string SubmittedByUserId { get; set; } // Optional (linked to Identity)
    }
}
