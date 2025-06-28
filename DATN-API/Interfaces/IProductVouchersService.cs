using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IProductVouchersService
    {
        Task<IEnumerable<ProductVouchers>> GetAllAsync();
        Task<ProductVouchers> GetByIdAsync(int id);
        Task<ProductVouchers> CreateAsync(ProductVouchers model);
        Task<bool> UpdateAsync(int id, ProductVouchers model);
        Task<bool> DeleteAsync(int id);
    }
}
