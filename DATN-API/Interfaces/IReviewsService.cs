
using DATN_API.Models;
using DATN_API.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IReviewsService
    {
        Task<IEnumerable<Reviews>> GetAllAsync();
        Task<Reviews?> GetByIdAsync(int id);
        Task<Reviews> CreateAsync(Reviews model, List<string>? mediaList);
        Task<bool> UpdateAsync(int id, Reviews model, List<string>? mediaList);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<ReviewViewModel>> GetByProductIdAsync(int productId);

        Task<bool> HasUserReviewedProductAsync(int orderId, int productId, int userId);
        Task<bool> IsOrderCompletedAsync(int orderId);
        Task<List<CompletedOrderViewModel>> GetCompletedOrdersByUserAsync(int userId);
    }
}
