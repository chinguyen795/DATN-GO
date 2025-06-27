using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface ICitiesService
    {
        Task<IEnumerable<Cities>> GetAllAsync();
        Task<Cities> GetByIdAsync(int id);
        Task<Cities> CreateAsync(Cities model);
        Task<bool> UpdateAsync(int id, Cities model);
        Task<bool> DeleteAsync(int id);
    }
}
