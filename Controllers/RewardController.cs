using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonicPoints.DTOs;
using SonicPoints.Models;
using SonicPoints.Repositories;
using SonicPoints.Services;
using SonicPoints.Data;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using SonicPoints.Dto;

namespace SonicPoints.Controllers
{
    [Route("api/rewards")]
    [ApiController]
    [Authorize]
    public class RewardController : ControllerBase
    {
        private readonly IRewardRepository _rewardRepository;
        private readonly ILeaderboardRepository _leaderboardRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private readonly AppDbContext _context;

        public RewardController(
            IRewardRepository rewardRepository,
            ILeaderboardRepository leaderboardRepository,
            IProjectRepository projectRepository,
            IProjectAuthorizationService projectAuthorization,
            AppDbContext context)
        {
            _rewardRepository = rewardRepository;
            _leaderboardRepository = leaderboardRepository;
            _projectRepository = projectRepository;
            _projectAuthorization = projectAuthorization;
            _context = context;
        }

        //  Redeem a reward
        [HttpPost("redeem")]
        public async Task<IActionResult> RedeemReward([FromBody] RedeemRequestDto redeemDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!await _projectAuthorization.HasProjectRoleAsync(userId, redeemDto.ProjectId, "Admin", "Manager", "Member"))
                return Forbid("You are not authorized to redeem rewards in this project.");

            var redeemableItem = await _rewardRepository.GetRedeemableItemByIdAsync(redeemDto.RedeemableItemId, redeemDto.ProjectId);
            if (redeemableItem == null)
                return NotFound("Reward item not found for this project.");

            var userPointsEntry = await _context.ProjectUserPoints
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ProjectId == redeemDto.ProjectId);

            if (userPointsEntry == null || userPointsEntry.TotalPoints < redeemableItem.Cost)
                return BadRequest("Not enough points to redeem this reward.");

            // Deduct from user points
            userPointsEntry.TotalPoints -= redeemableItem.Cost;
            await _context.SaveChangesAsync();

            // Also deduct from leaderboard (if exists)
            var leaderboardEntry = await _context.Leaderboards
                .FirstOrDefaultAsync(l => l.UserId == userId && l.Task.ProjectId == redeemDto.ProjectId);

            if (leaderboardEntry != null)
            {
                leaderboardEntry.RedeemedPoints += redeemableItem.Cost;
                await _context.SaveChangesAsync();
            }

            // Log redemption
            var redeemHistory = new RedeemHistory
            {
                UserId = userId,
                RedeemableItemId = redeemableItem.Id,
                PointsUsed = redeemableItem.Cost,
                RedeemedOn = DateTime.UtcNow,
                ProjectId = redeemDto.ProjectId
            };

            await _rewardRepository.SaveRedeemHistoryAsync(redeemHistory);

            return Ok(new
            {
                message = "Reward redeemed successfully!",
                remainingPoints = userPointsEntry.TotalPoints
            });
        }


        // ✅ Get Redeemed Rewards History (project-admin only)
        [HttpGet("redeemed/{projectId}")]
        public async Task<IActionResult> GetRedeemedRewards(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!await _projectAuthorization.HasProjectRoleAsync(userId, projectId, "Admin"))
                return Forbid("Only project Admins can access redemption history.");

            var redeemedRewards = await _rewardRepository.GetRedeemedHistoryByProjectAsync(projectId);
            if (redeemedRewards == null || !redeemedRewards.Any())
                return NotFound("No redeemed rewards found for this project.");

            return Ok(redeemedRewards);
        }
        //  Show redemption history for all users in a project (accessible to project members)
        [HttpGet("history/{projectId}")]
        public async Task<IActionResult> GetMyProjectRedeemHistory(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            //  Verify the user is part of this project
            var isAuthorized = await _projectAuthorization.HasProjectRoleAsync(userId, projectId, "Admin", "Manager", "Checker", "Member");
            if (!isAuthorized)
                return Forbid("You are not part of this project.");

            var allHistory = await _rewardRepository.GetRedeemedHistoryByProjectAsync(projectId);

            if (allHistory == null || !allHistory.Any())
                return NotFound("No redeem history found for this project.");

            var response = allHistory.Select(h => new
            {
                h.UserId,
                h.RedeemableItemId,
                h.PointsUsed,
                h.RedeemedOn,
                h.ProjectId,
                RedeemableItemName = h.RedeemableItem.Name
            });

            return Ok(response);
        }

    }
}