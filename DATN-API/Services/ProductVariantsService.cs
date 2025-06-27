using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
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
            pv.ProductId = model.ProductId;
           
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
    }
}
