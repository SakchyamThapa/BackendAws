using Microsoft.EntityFrameworkCore;
using SonicPoints.Data;
using SonicPoints.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SonicPoints.Repositories
{
    public class RewardRepository : IRewardRepository
    {
        private readonly AppDbContext _context;

        public RewardRepository(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Get a redeemable item by ID
        public async Task<RedeemableItem> GetRedeemableItemByIdAsync(int redeemId, int projectId)
        {
            return await _context.RedeemableItems
                .FirstOrDefaultAsync(r => r.Id == redeemId && r.ProjectId == projectId);
        }

        // ✅ Save redeem history when a user redeems a reward
        public async Task SaveRedeemHistoryAsync(RedeemHistory redeemHistory)
        {
            _context.RedeemHistory.Add(redeemHistory);
            await _context.SaveChangesAsync();
        }

        // ✅ Get redeemed rewards history for a specific project
        public async Task<List<RedeemHistory>> GetRedeemedHistoryByProjectAsync(int projectId)
        {
            return await _context.RedeemHistory
                .Include(r => r.RedeemableItem)
                .Where(r => r.ProjectId == projectId)
                .ToListAsync();
        }
    }
}
