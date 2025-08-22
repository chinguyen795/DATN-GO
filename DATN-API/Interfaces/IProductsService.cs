using DATN_API.Models;
using DATN_API.ViewModels;
using DATN_GO.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IProductsService
    {
        Task<IEnumerable<Products>> GetAllAsync();
        Task<Products> GetByIdAsync(int id);
        Task<Products> CreateAsync(Products model);
        Task<bool> UpdateAsync(int id, Products model);
        Task<bool> DeleteAsync(int id);
        Task<int> GetTotalProductsAsync();
        Task<Dictionary<int, int>> GetProductCountByMonthAsync(int year);
        Task<List<StoreProductVariantViewModel>> GetAllStoreProductVariantsAsync();
        Task<List<int>> GetProductIdsByStoreIdAsync(int storeId);
        Task<List<Products>> GetProductsByStoreIdAsync(int storeId);
        Task<(bool Success, int? ProductId, string? ErrorMessage)> CreateFullProductAsync(ProductFullCreateViewModel model);
        Task<bool> DeleteProductAndRelatedAsync(int productId);

        Task<List<Products>> GetProductsByStoreAsync(int storeId);
        Task<IEnumerable<ProductAdminViewModel>> GetByStatusAsync(string status);


        Task<bool> UpdateStatusAsync(int id, string status);
        Task<int> GetProductCountByStoreIdAsync(int storeId);

    }
}
