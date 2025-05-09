using Microsoft.EntityFrameworkCore;
using SonicPoints.Data;
using SonicPoints.DTOs;
using SonicPoints.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SonicPoints.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _context;

        public ProjectRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Project>> GetUserProjectsAsync(string userId)
        {
            return await _context.Projects
                .Where(p => p.ProjectUsers.Any(pu => pu.UserId == userId))
                .ToListAsync();
        }

        public async Task<Project> GetProjectByIdAsync(int projectId, string userId)
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.ProjectUsers.Any(pu => pu.UserId == userId));
        }

        public async Task<Project> CreateProjectAsync(Project project, string userId)
        {
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            var projectUser = new ProjectUser
            {
                ProjectId = project.Id,
                UserId = userId,
                Role = "Admin",
                RewardPoints = 0
            };

            _context.ProjectUsers.Add(projectUser);
            await _context.SaveChangesAsync();

            return project;
        }

        public async Task<Project> UpdateProjectAsync(int projectId, string userId, UpdateProjectDto updateProjectDto)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.AdminId != userId) return null;

            project.Name = updateProjectDto.Name;
            project.Description = updateProjectDto.Description;
            project.DueDate = updateProjectDto.DueDate;

            await _context.SaveChangesAsync();
            return project;
        }

        public async Task<bool> DeleteProjectAsync(int projectId, string userId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.AdminId != userId) return false;

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddUserToProjectAsync(int projectId, string adminId, string userEmailOrUserId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.AdminId != adminId) return false;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmailOrUserId || u.Id == userEmailOrUserId);
            if (user == null) return false;

            var alreadyExists = await _context.ProjectUsers
                .AnyAsync(pu => pu.ProjectId == projectId && pu.UserId == user.Id);
            if (alreadyExists) return false;

            var projectUser = new ProjectUser
            {
                ProjectId = projectId,
                UserId = user.Id,
                Role = "Member"
            };

            _context.ProjectUsers.Add(projectUser);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<string> GetUserRoleInProjectAsync(int projectId, string userId)
        {
            return await _context.ProjectUsers
                .Where(pu => pu.ProjectId == projectId && pu.UserId == userId)
                .Select(pu => pu.Role)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateProjectUserRoleAsync(int projectId, string adminId, string userId, string newRole)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null || project.AdminId != adminId)
                return false;

            var projectUser = await _context.ProjectUsers
                .FirstOrDefaultAsync(pu => pu.ProjectId == projectId && pu.UserId == userId);

            if (projectUser == null)
                return false;

            projectUser.Role = newRole;
            _context.ProjectUsers.Update(projectUser);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
