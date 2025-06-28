using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IWardsService
    {
        Task<IEnumerable<Wards>> GetAllAsync();
        Task<Wards> GetByIdAsync(int id);
        Task<IEnumerable<Wards>> GetByDistrictIdAsync(int districtId);
        Task<Wards> CreateAsync(Wards model);
        Task<bool> UpdateAsync(int id, Wards model);
        Task<bool> DeleteAsync(int id);
    }
}
