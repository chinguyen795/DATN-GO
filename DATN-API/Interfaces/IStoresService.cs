using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IStoresService
    {
        Task<IEnumerable<Stores>> GetAllAsync();
        Task<Stores> GetByIdAsync(int id);
        Task<Stores> CreateAsync(Stores model);
        Task<bool> UpdateAsync(int id, Stores model);
        Task<bool> DeleteAsync(int id);
        Task<Stores?> GetByUserIdAsync(int userId);

        Task<Stores?> GetStoreByUserIdAsync(int userId);
        Task<int> GetTotalStoresAsync();
        Task<int> GetTotalActiveStoresAsync();
        Task<int> GetStoreCountByMonthYearAsync(int month, int year);
        Task<Dictionary<int, int>> GetStoreCountByMonthAsync(int year);

        Task<IEnumerable<Stores>> GetByStatusAsync(string status);
        Task<bool> UpdateStatusAsync(int id, string status);
    }
}
