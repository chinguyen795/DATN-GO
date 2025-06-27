using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IStoresService
    {
        Task<IEnumerable<Stores>> GetAllAsync();
        Task<Stores> GetByIdAsync(int id);
        Task<Stores> CreateAsync(Stores model);
        Task<bool> UpdateAsync(int id, Stores model);
        Task<bool> DeleteAsync(int id);
    }
}
