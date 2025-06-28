using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IVariantsService
    {
        Task<IEnumerable<Variants>> GetAllAsync();
        Task<Variants> GetByIdAsync(int id);
        Task<Variants> CreateAsync(Variants model);
        Task<bool> UpdateAsync(int id, Variants model);
        Task<bool> DeleteAsync(int id);
    }
}
