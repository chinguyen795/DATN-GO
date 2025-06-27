using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface ICategoriesService
    {
        Task<IEnumerable<Categories>> GetAllAsync();
        Task<Categories> GetByIdAsync(int id);
        Task<Categories> CreateAsync(Categories model);
        Task<bool> UpdateAsync(int id, Categories model);
        Task<bool> DeleteAsync(int id);
    }
}
