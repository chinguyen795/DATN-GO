using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IDecoratesService
    {
        Task<IEnumerable<Decorates>> GetAllAsync();
        Task<Decorates> GetByIdAsync(int id);
        Task<Decorates> CreateAsync(Decorates model);
        Task<bool> UpdateAsync(int id, Decorates model);
        Task<bool> DeleteAsync(int id);
    }
}
