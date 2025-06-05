using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SonicPoints.Data;
using SonicPoints.Dto;
using SonicPoints.Models;
using SonicPoints.Repositories;
using SonicPoints.Services;
using System.Security.Claims;

namespace SonicPoints.Controllers
{
    [Route("api/leaderboard")]
    [ApiController]
    [Authorize]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardRepository _leaderboardRepository;
        private readonly IMemoryCache _cache;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private readonly AppDbContext _context;


        public LeaderboardController(
            ILeaderboardRepository leaderboardRepository,
            IMemoryCache cache,
            IProjectAuthorizationService projectAuthorization,
            AppDbContext context)
        {
            _leaderboardRepository = leaderboardRepository;
            _cache = cache;
            _projectAuthorization = projectAuthorization;
            _context = context;
        }

        //  Get Leaderboard by Project with Pagination and Caching
        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetLeaderboard(int projectId, int pageNumber = 1, int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var hasAccess = await _projectAuthorization.HasProjectRoleAsync(userId, projectId, "Admin", "Manager", "Checker", "Member");
            if (!hasAccess)
                return Forbid("You are not a member of this project.");

            string cacheKey = $"leaderboard_{projectId}_page_{pageNumber}_size_{pageSize}";

            if (!_cache.TryGetValue(cacheKey, out List<LeaderboardDto> cachedLeaderboard))
            {
                try
                {
                    var leaderboard = await _leaderboardRepository.GetLeaderboardByProjectAsync(projectId);
                    if (leaderboard == null || !leaderboard.Any())
                        return Ok(new List<LeaderboardDto>());


                    int totalTasksInProject = await _leaderboardRepository.GetTotalTasksInProjectAsync(projectId);
                    totalTasksInProject = Math.Max(totalTasksInProject, 1); // Prevent division by zero

                    var pagedLeaderboard = leaderboard
                      .OrderByDescending(l => l.PointsEarned)
                      .Skip((pageNumber - 1) * pageSize)
                      .Take(pageSize)
                      .ToList() // Force materialization before calling _context
                      .Select((l, index) => new LeaderboardDto
                      {
                          UserId = l.UserId,
                          UserName = l.User.UserName,
                          PointsEarned = l.PointsEarned,
                          TaskCompletionCount = l.TaskCompletionCount,
                          RedeemedPoints = l.RedeemedPoints,
                          LeaderboardRank = index + 1 + ((pageNumber - 1) * pageSize),
                          ProjectProgress = (l.TaskCompletionCount / (double)totalTasksInProject) * 100,
                          RedeemablePoints = CalculateKpiPoints(l.PointsEarned, l.TaskCompletionCount, l.RedeemedPoints, index + 1, totalTasksInProject),

                          TotalPoints = _context.ProjectUserPoints
                              .Where(p => p.ProjectId == projectId && p.UserId == l.UserId)
                              .Select(p => p.TotalPoints)
                              .FirstOrDefault()
                      })
                      .ToList();



                    if (pagedLeaderboard.Any())
                        _cache.Set(cacheKey, pagedLeaderboard, TimeSpan.FromMinutes(10));

                    return Ok(pagedLeaderboard);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Internal server error", details = ex.ToString() });

                }
            }

            return Ok(cachedLeaderboard);
        }

        //  KPI-Based Redeemable Points Calculation
        private int CalculateKpiPoints(int pointsEarned, int taskCompletionCount, int redeemedPoints, int rank, int totalTasks)
        {
            double basePoints = pointsEarned * 0.5;
            double taskFactor = taskCompletionCount * 1.2;
            double redemptionPenalty = redeemedPoints * 0.3;
            double rankBonus = (totalTasks > 0) ? ((totalTasks - rank) / (double)totalTasks) * 50 : 0;

            return Math.Max(0, (int)(basePoints + taskFactor + rankBonus - redemptionPenalty));
        }
    }
}
