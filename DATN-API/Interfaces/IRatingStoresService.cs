using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IRatingStoresService
    {
        Task<IEnumerable<RatingStores>> GetAllAsync();
        Task<RatingStores> GetByIdAsync(int id);
        Task<RatingStores> CreateAsync(RatingStores model);
        Task<bool> UpdateAsync(int id, RatingStores model);
        Task<bool> DeleteAsync(int id);
    }
}
