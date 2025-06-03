using SonicPoints.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonicPoints.Repositories
{
    public interface IRedeemableItemRepository
    {
        // Add a new redeemable item to the database
        Task<RedeemableItem> AddRedeemableItemAsync(RedeemableItem redeemableItem);

        // Get a redeemable item by its ID (not by project)
        Task<RedeemableItem> GetRedeemableItemByIdAsync(int id);

        // Get all redeemable items for a specific project
        Task<List<RedeemableItem>> GetRedeemableItemsByProjectId(int projectId);

        // Update a redeemable item
        Task<RedeemableItem> UpdateRedeemableItemAsync(RedeemableItem redeemableItem);

        // Delete a redeemable item
        Task DeleteRedeemableItemAsync(int id);

        // Save changes to the database
        Task SaveAsync();
    }
}
