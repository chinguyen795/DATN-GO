using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IRolesService
    {
        Task<IEnumerable<Roles>> GetAllAsync();
        Task<Roles> GetByIdAsync(int id);
        Task<Roles> CreateAsync(Roles model);
        Task<bool> UpdateAsync(int id, Roles model);
        Task<bool> DeleteAsync(int id);
    }
}
