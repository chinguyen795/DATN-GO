using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IPricesService
    {
        Task<IEnumerable<Prices>> GetAllAsync();
        Task<Prices> GetByIdAsync(int id);
        Task<Prices> CreateAsync(Prices model);
        Task<bool> UpdateAsync(int id, Prices model);
        Task<bool> DeleteAsync(int id);
    }
}
