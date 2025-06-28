using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IDeliveryTrackingsService
    {
        Task<IEnumerable<DeliveryTrackings>> GetAllAsync();
        Task<DeliveryTrackings?> GetByIdAsync(int id);
        Task<DeliveryTrackings?> GetByOrderIdAsync(int orderId);
        Task<DeliveryTrackings> CreateAsync(DeliveryTrackings model);
        Task<bool> UpdateAsync(int id, DeliveryTrackings model);
        Task<bool> DeleteAsync(int id);
    }
}
