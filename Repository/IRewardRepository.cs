using SonicPoints.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonicPoints.Repositories
{
    public interface IRewardRepository
    {
        Task<RedeemableItem> GetRedeemableItemByIdAsync(int redeemId, int projectId);
        Task SaveRedeemHistoryAsync(RedeemHistory redeemHistory);
        Task<List<RedeemHistory>> GetRedeemedHistoryByProjectAsync(int projectId);
    }
}
