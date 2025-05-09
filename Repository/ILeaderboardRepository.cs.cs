using SonicPoints.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonicPoints.Repositories
{
    public interface ILeaderboardRepository
    {
        Task<IEnumerable<Leaderboard>> GetLeaderboardByProjectAsync(int projectId);
        Task<int> GetTotalTasksInProjectAsync(int projectId);

        Task<Leaderboard> GetLeaderboardEntry(int taskId, string userId); // Get leaderboard entry by task and user
        Task AddLeaderboardEntry(Leaderboard leaderboardEntry); // Add a new leaderboard entry
        Task UpdateLeaderboardEntry(Leaderboard leaderboardEntry); // Update existing leaderboard entry
    }
}
