using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IShippingMethodsService
    {
        Task<IEnumerable<ShippingMethods>> GetAllAsync();
        Task<ShippingMethods> GetByIdAsync(int id);
        Task<ShippingMethods> CreateAsync(ShippingMethods model);
        Task<bool> UpdateAsync(int id, ShippingMethods model);
        Task<bool> DeleteAsync(int id);
    }
}
