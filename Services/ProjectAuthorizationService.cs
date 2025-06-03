using Microsoft.EntityFrameworkCore;

using SonicPoints.Data;

namespace SonicPoints.Services
{
    public class ProjectAuthorizationService : IProjectAuthorizationService
    {
        private readonly AppDbContext _context;

        public ProjectAuthorizationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasProjectRoleAsync(string userId, int projectId, params string[] roles)
        {
            return await _context.ProjectUsers
                .AnyAsync(pu => pu.UserId == userId && pu.ProjectId == projectId && roles.Contains(pu.Role));
        }
    }

}
