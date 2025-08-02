using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class ProductsService : IProductsService
    {
        private readonly ApplicationDbContext _context;
        public ProductsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Products>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Products> GetByIdAsync(int id)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Products> CreateAsync(Products model)
        {
            _context.Products.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Products model)
        {
            if (id != model.Id) return false;

            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            // Cập nhật đầy đủ các thuộc tính theo đúng Models
            product.CategoryId = model.CategoryId;
            product.StoreId = model.StoreId;
            product.Name = model.Name;
            product.Brand = model.Brand;
            product.Weight = model.Weight;
            product.Slug = model.Slug;
            product.Description = model.Description;
            product.MainImage = model.MainImage;
            product.Status = model.Status;
            product.Quantity = model.Quantity;
            product.Views = model.Views;
            product.Rating = model.Rating;
            product.CostPrice = model.CostPrice;
            product.Length = model.Length;
            product.Width = model.Width;
            product.Height = model.Height;
            product.PlaceOfOrigin = model.PlaceOfOrigin;
            product.Hashtag = model.Hashtag;

            product.UpdateAt = DateTime.Now; // Cập nhật thời gian sửa đổi

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<int> GetTotalProductsAsync()
        {
            return await _context.Products.CountAsync();
        }
        public async Task<Dictionary<int, int>> GetProductCountByMonthAsync(int year)
        {
            var rawData = await _context.Products
                .Where(p => p.CreateAt.Year == year)
                .GroupBy(p => p.CreateAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Month, x => x.Count);

            // Đảm bảo đủ 12 tháng
            var result = Enumerable.Range(1, 12)
                .ToDictionary(m => m, m => rawData.ContainsKey(m) ? rawData[m] : 0);

            return result;
        }

        public async Task<List<Products>> GetProductsByStoreAsync(int storeId)
        {
            return await _context.Products
                .Include(p => p.Store)
                .Include(p => p.Category)
                .Where(p => p.StoreId == storeId && p.Status == ProductStatus.Approved)
                .ToListAsync();
        }
    }
}
