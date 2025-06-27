using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IReviewsService
    {
        Task<IEnumerable<Reviews>> GetAllAsync();
        Task<Reviews> GetByIdAsync(int id);
        Task<Reviews> CreateAsync(Reviews model);
        Task<bool> UpdateAsync(int id, Reviews model);
        Task<bool> DeleteAsync(int id);
    }
}
