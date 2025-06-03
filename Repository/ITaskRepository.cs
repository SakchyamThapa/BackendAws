using SonicPoints.Models;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId, string userId);
    Task<IEnumerable<TaskItem>> GetFilteredTasksAsync(int projectId, string userId, string status = null);
    Task<TaskItem> CreateTaskAsync(TaskItem task, string userId);
    Task<bool> UpdateTaskStatusAsync(TaskItem task);
    Task<TaskItem> GetTaskByIdAsync(int taskId);
    Task<bool> UpdateTaskAsync(TaskItem task);

    Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId);

    Task<bool> DeleteTaskAsync(int taskId, string userId);
    Task<bool> SaveAsync();
}
