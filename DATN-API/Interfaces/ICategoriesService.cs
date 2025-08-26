using DATN_API.Models;
using DATN_API.ViewModels;

namespace DATN_API.Interfaces
{
    public interface ICategoriesService
    {
        Task<IEnumerable<Categories>> GetAllAsync(); // lấy danh sách gốc
        Task<IEnumerable<CategoryWithUsageViewModel>> GetAllWithUsageAsync(); // lấy danh sách + usage
        Task<Categories?> GetByIdAsync(int id);
        Task<Categories> CreateAsync(Categories model);
        Task<bool> UpdateAsync(int id, Categories model);
        Task<bool> DeleteAsync(int id);
        Task<Categories?> GetCategoryByProductIdAsync(int productId);
    }
}
