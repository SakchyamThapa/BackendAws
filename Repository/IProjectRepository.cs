using SonicPoints.DTOs;
using SonicPoints.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonicPoints.Repositories
{
    public interface IProjectRepository
    {
        Task<IEnumerable<Project>> GetUserProjectsAsync(string userId);
        Task<Project> GetProjectByIdAsync(int projectId, string userId);
        Task<Project> CreateProjectAsync(Project project, string userId);
        Task<Project> UpdateProjectAsync(int projectId, string userId, UpdateProjectDto updateProjectDto);
        Task<bool> DeleteProjectAsync(int projectId, string userId);
        Task<bool> AddUserToProjectAsync(int projectId, string adminId, string newUserId);
        Task<string> GetUserRoleInProjectAsync(int projectId, string userId);

    }
}
