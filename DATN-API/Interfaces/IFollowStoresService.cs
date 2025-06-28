using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IFollowStoresService
    {
        Task<IEnumerable<FollowStores>> GetAllAsync();
        Task<FollowStores> GetByIdAsync(int id);
        Task<FollowStores> CreateAsync(FollowStores model);
        Task<bool> UpdateAsync(int id, FollowStores model);
        Task<bool> DeleteAsync(int id);
    }
}
