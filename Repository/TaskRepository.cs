using Microsoft.EntityFrameworkCore;
using SonicPoints.Data;
using SonicPoints.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SonicPoints.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        // Get all tasks by projectId
        public async Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId, string userId)
        {
            return await _context.Tasks
                .Where(t => t.ProjectId == projectId &&
                            (t.Project.AdminId == userId ||
                             t.Project.ProjectUsers.Any(pu => pu.UserId == userId)))
                .ToListAsync();
        }

        //  Get filtered tasks by status (optional)
        public async Task<IEnumerable<TaskItem>> GetFilteredTasksAsync(int projectId, string userId, string status = null)
        {
            var query = _context.Tasks
                .Where(t => t.ProjectId == projectId &&
                            (t.Project.AdminId == userId ||
                             t.Project.ProjectUsers.Any(pu => pu.UserId == userId)));

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ProjectTaskStatus>(status, out var parsedStatus))
            {
                query = query.Where(t => t.Status == parsedStatus);
            }

            return await query.ToListAsync();
        }

        // Create a new task
        public async Task<TaskItem> CreateTaskAsync(TaskItem task, string userId)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Add user to ProjectUsers if not already in the project
            var isUserInProject = await _context.ProjectUsers
                .AnyAsync(pu => pu.UserId == userId && pu.ProjectId == task.ProjectId);

            if (!isUserInProject)
            {
                _context.ProjectUsers.Add(new ProjectUser
                {
                    UserId = userId,
                    ProjectId = task.ProjectId,
                    Role = "Member",
                    RewardPoints = 0
                });

                await _context.SaveChangesAsync();
            }

            return task;
        }

        // Update task status
        public async Task<bool> UpdateTaskStatusAsync(TaskItem task)
        {
            var existingTask = await _context.Tasks.FindAsync(task.Id);
            if (existingTask == null)
                return false;

            existingTask.Status = task.Status;
            _context.Tasks.Update(existingTask);
            await _context.SaveChangesAsync();
            return true;
        }

        //  Get task by ID
        public async Task<TaskItem> GetTaskByIdAsync(int taskId)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        // Delete a task with permission check
        public async Task<bool> DeleteTaskAsync(int taskId, string userId)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return false;

            bool isOwner = task.UserId == userId;
            bool isAdmin = task.Project.AdminId == userId;

            if (!isOwner && !isAdmin)
                return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateTaskAsync(TaskItem task)
        {
            var existingTask = await _context.Tasks.FindAsync(task.Id);
            if (existingTask == null)
                return false;

            existingTask.Title = task.Title ?? existingTask.Title;
            existingTask.Description = task.Description ?? existingTask.Description;
            existingTask.Priority = task.Priority;
            existingTask.RewardPoints = task.RewardPoints;
            existingTask.DueDate = task.DueDate;
            existingTask.Status = task.Status;

           

            await _context.SaveChangesAsync();
            return true;
        }



        public async Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId)
        {
            return await _context.Tasks
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();
        }

        //  Save async
        public async Task<bool> SaveAsync()
        {
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
