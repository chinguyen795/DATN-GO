using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class ProductVariantsService : IProductVariantsService
    {
        private readonly ApplicationDbContext _context;
        public ProductVariantsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductVariants>> GetAllAsync()
        {
            return await _context.ProductVariants.ToListAsync();
        }

        public async Task<ProductVariants> GetByIdAsync(int id)
        {
            return await _context.ProductVariants.FirstOrDefaultAsync(pv => pv.Id == id);
        }

        public async Task<ProductVariants> CreateAsync(ProductVariants model)
        {
            _context.ProductVariants.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, ProductVariants model)
        {
            if (id != model.Id) return false;

            var pv = await _context.ProductVariants.FindAsync(id);
            if (pv == null) return false;

            // Cập nhật đầy đủ các thuộc tính
            pv.ProductId = model.ProductId;
            pv.Price = model.Price;
            pv.Weight = model.Weight;
            pv.CostPrice = model.CostPrice;
            pv.Quantity = model.Quantity;
            pv.Height = model.Height;
            pv.Width = model.Width;
            pv.Length = model.Length;
            pv.Image = model.Image;
            // Cập nhật thời gian
            pv.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var pv = await _context.ProductVariants.FindAsync(id);
            if (pv == null) return false;
            _context.ProductVariants.Remove(pv);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<List<ProductVariants>> GetByProductIdAsync(int productId)
        {
            return await _context.ProductVariants
                .Include(pv => pv.ProductImages)
                .Where(pv => pv.ProductId == productId)
                .ToListAsync();
        }
        public async Task<List<string>> GetAllImagesByProductIdAsync(int productId)
        {
            var product = await _context.Products
                .Where(p => p.Id == productId)
                .Select(p => new { p.MainImage })
                .FirstOrDefaultAsync();

            if (product == null)
                return new List<string>();

            var variantImages = await _context.ProductVariants
                .Where(pv => pv.ProductId == productId && !string.IsNullOrEmpty(pv.Image))
                .Select(pv => pv.Image)
                .ToListAsync();

            var result = new List<string>();

            if (!string.IsNullOrEmpty(product.MainImage))
                result.Add(product.MainImage); // ảnh chính từ bảng Products

            result.AddRange(variantImages);    // ảnh từ các ProductVariants

            return result;
        }

    }
}
