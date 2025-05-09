namespace SonicPoints.Models
{
    public class Project
    {
        public int Id { get; set; }  // Ensure this is an int
        public string Name { get; set; }
        public string Description { get; set; }
        public string AdminId { get; set; }
        public User Admin { get; set; }
        public DateTime DueDate { get; set; } // Project Deadline
        public List<ProjectUser> ProjectUsers { get; set; }  // Navigation property
        public List<TaskItem> Tasks { get; set; }
        public List<RedeemableItem> ShopItems { get; set; }
        public List<RedeemHistory> RedeemHistories { get; set; }
        public string ProjectStatus { get; set; }

        public double Progress
        {
            get
            {
                if (Tasks == null || Tasks.Count == 0)
                    return 0; // No tasks, 0% progress

                int totalTasks = Tasks.Count;
                int completedTasks = Tasks.Count(t => t.Status == ProjectTaskStatus.Completed);
                // Compare with TaskStatus enum

                return (double)completedTasks / totalTasks * 100;
            }
        }

        // Compute Project Status based on progress
        public string ComputedProjectStatus
        {
            get
            {
                if (Progress == 100)
                    return "Completed";
                else if (Progress > 0)
                    return "In Progress";
                else
                    return "Not Started";
            }
        }
    }
}
