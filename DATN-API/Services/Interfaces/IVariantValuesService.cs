using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IVariantValuesService
    {
        Task<IEnumerable<VariantValues>> GetAllAsync();
        Task<VariantValues> GetByIdAsync(int id);
        Task<VariantValues> CreateAsync(VariantValues model);
        Task<bool> UpdateAsync(int id, VariantValues model);
        Task<bool> DeleteAsync(int id);
    }
}
