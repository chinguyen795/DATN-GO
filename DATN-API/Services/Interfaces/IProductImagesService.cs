using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IProductImagesService
    {
        Task<IEnumerable<ProductImages>> GetAllAsync();
        Task<ProductImages> GetByIdAsync(int id);
        Task<ProductImages> CreateAsync(ProductImages model);
        Task<bool> UpdateAsync(int id, ProductImages model);
        Task<bool> DeleteAsync(int id);
    }
}
