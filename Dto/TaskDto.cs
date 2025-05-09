using SonicPoints.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace SonicPoints.DTOs
{
    /// <summary>
    /// DTO for creating a new task.
    /// </summary>
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Task title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Priority is required.")]
        public TaskPriority Priority { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Valid project ID is required.")]
        public int ProjectId { get; set; }

        [Range(0, 999, ErrorMessage = "Reward points must be between 0 and 999.")]
        public int RewardPoints { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }
    }

    /// <summary>
    /// DTO for updating task status.
    /// </summary>
    public class UpdateTaskDto
    {
        [Range(0, 3, ErrorMessage = "Status must be 0 (Backlog) to 3 (Completed).")]
        public int Status { get; set; }
    }

    /// <summary>
    /// DTO for editing task details.
    /// </summary>
    public class EditTaskDto
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        public TaskPriority Priority { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }
        public int RewardPoints { get; set; }

    }
    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int ProjectId { get; set; }
        public int RewardPoints { get; set; }
        public DateTime DueDate { get; set; }
        public string? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
    }

    /// <summary>
    /// DTO for drag-and-drop task reordering.
    /// </summary>
    public class TaskOrderDto
    {
        [Required]
        public int TaskId { get; set; }

        [Range(0, 3, ErrorMessage = "Status must be between 0 and 3.")]
        public int NewStatus { get; set; }
    }
}
