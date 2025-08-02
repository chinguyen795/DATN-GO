using DATN_API.Models;
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
        Task<List<Products>> GetProductsByStoreAsync(int storeId);


    }
}
