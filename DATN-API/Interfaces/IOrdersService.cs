using DATN_API.Models;
using DATN_API.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IOrdersService
    {
        Task<IEnumerable<Orders>> GetAllAsync();
        Task<Orders> GetByIdAsync(int id);
        Task<Orders> CreateAsync(Orders model);
        Task<bool> UpdateAsync(int id, Orders model);
        Task<bool> DeleteAsync(int id);
        Task<List<OrderViewModel>> GetOrdersByUserIdAsync(int userId);
        Task<(bool, string)> UpdateStatusAsync(int orderId, OrderStatus status);
        Task<object> GetStatisticsAsync(int storeId, DateTime? start, DateTime? end, DateTime? startCompare, DateTime? endCompare);
        Task<OrderViewModel?> GetOrderDetailAsync(int orderId);
        Task<List<OrderViewModel>> GetOrdersByStoreUserAsync(int userId);
    }
}
