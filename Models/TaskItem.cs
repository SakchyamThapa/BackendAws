using System.Text.Json.Serialization;

namespace SonicPoints.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        
        public string Description { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProjectTaskStatus Status { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TaskPriority Priority { get; set; }
        public int ProjectId { get; set; }
        public string? PointEligibleUserId { get; set; }


        public Project Project { get; set; }
        public int RewardPoints { get; set; } // Points given when completed
        public DateTime AssignedDate { get; set; }
        public DateTime DueDate { get; set; } // Task deadline
        public List<Leaderboard> Leaderboards { get; set; } // Track who completed it

        public string UserId { get; set; }  // UserId for the user who completed the task
       
        public User User { get; set; }
    }

    public enum ProjectTaskStatus
    {
        Backlog,
        InProgress,
        Review,
        Completed
    }
    public enum TaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

}
