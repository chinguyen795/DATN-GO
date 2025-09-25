using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Twilio.TwiML.Voice;

namespace DATN_API.Services
{
    public class CategoriesService : ICategoriesService
    {
        private readonly Data.ApplicationDbContext _context;
        public CategoriesService(Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Categories>> GetAllAsync()
        {
            return await _context.Categories
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<CategoryWithUsageViewModel>> GetAllWithUsageAsync()
        {
            return await _context.Categories
                .Select(c => new CategoryWithUsageViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Hashtag = c.Hashtag,
                    Image = c.Image,
                    Status = (int)c.Status,
                    Description = c.Description,
                    UsageCount = _context.Products.Count(p => p.CategoryId == c.Id) // đếm số sản phẩm thuộc danh mục
                })
                .ToListAsync();
        }

        public async Task<Categories> GetByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<Categories> CreateAsync(Categories model)
        {
            if (model.CreateAt == default)
                model.CreateAt = DateTime.UtcNow;
            _context.Categories.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Categories model)
        {
            var existing = await _context.Categories.FindAsync(id);
            if (existing == null) return false;
            _context.Entry(existing).CurrentValues.SetValues(model);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.Categories.FindAsync(id);
            if (existing == null) return false;
            _context.Categories.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<Categories> GetCategoryByProductIdAsync(int productId)
        {
            return await _context.Products
                .Where(p => p.Id == productId)
                .Include(p => p.Category)
                .Select(p => p.Category)
                .FirstOrDefaultAsync();
        }
    }
}
