using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IOrdersService
    {
        Task<IEnumerable<Orders>> GetAllAsync();
        Task<Orders> GetByIdAsync(int id);
        Task<Orders> CreateAsync(Orders model);
        Task<bool> UpdateAsync(int id, Orders model);
        Task<bool> DeleteAsync(int id);
    }
}
