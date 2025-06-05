using Microsoft.EntityFrameworkCore;
using SonicPoints.Data;
using SonicPoints.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SonicPoints.Repositories
{
    public class LeaderboardRepository : ILeaderboardRepository
    {
        private readonly AppDbContext _context;

        public LeaderboardRepository(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Get leaderboard for a specific project
        public async Task<IEnumerable<Leaderboard>> GetLeaderboardByProjectAsync(int projectId)
        {
            var usersInProject = await _context.ProjectUsers
                .Where(pu => pu.ProjectId == projectId)
                .Select(pu => new
                {
                    pu.UserId,
                    pu.User
                }).ToListAsync();

            var result = new List<Leaderboard>();

            foreach (var user in usersInProject)
            {
                var pointsEarned = await _context.Leaderboards
                    .Where(l => l.UserId == user.UserId && l.Task.ProjectId == projectId)
                    .SumAsync(l => (int?)l.PointsEarned) ?? 0;

                var taskCount = await _context.Tasks
                    .CountAsync(t => t.UserId == user.UserId && t.ProjectId == projectId && t.Status == ProjectTaskStatus.Completed);

                var redeemedPoints = await _context.RedeemHistory
                    .Where(r => r.UserId == user.UserId && r.ProjectId == projectId)
                    .SumAsync(r => (int?)r.PointsUsed) ?? 0;

                result.Add(new Leaderboard
                {
                    UserId = user.UserId,
                    User = user.User,
                    PointsEarned = pointsEarned,
                    TaskCompletionCount = taskCount,
                    RedeemedPoints = redeemedPoints
                });
            }

            return result.OrderByDescending(r => r.PointsEarned).ToList();
        }



        //  Get total number of tasks in a project
        public async Task<int> GetTotalTasksInProjectAsync(int projectId)
        {
            return await _context.Tasks.CountAsync(t => t.ProjectId == projectId);
        }

        //  Get a specific leaderboard entry by task ID and user ID
        public async Task<Leaderboard> GetLeaderboardEntry(int taskId, string userId)
        {
            return await _context.Leaderboards
                .FirstOrDefaultAsync(l => l.TaskId == taskId && l.UserId == userId);
        }

        //  Add a new leaderboard entry
        public async Task AddLeaderboardEntry(Leaderboard leaderboardEntry)
        {
            await _context.Leaderboards.AddAsync(leaderboardEntry);
            await _context.SaveChangesAsync();
        }

        //  Update an existing leaderboard entry
        public async Task UpdateLeaderboardEntry(Leaderboard leaderboardEntry)
        {
            _context.Leaderboards.Update(leaderboardEntry);
            await _context.SaveChangesAsync();
        }
    }
}
