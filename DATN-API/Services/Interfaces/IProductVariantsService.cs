using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IProductVariantsService
    {
        Task<IEnumerable<ProductVariants>> GetAllAsync();
        Task<ProductVariants> GetByIdAsync(int id);
        Task<ProductVariants> CreateAsync(ProductVariants model);
        Task<bool> UpdateAsync(int id, ProductVariants model);
        Task<bool> DeleteAsync(int id);
    }
}
