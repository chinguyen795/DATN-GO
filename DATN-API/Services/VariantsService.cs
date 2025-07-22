using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class VariantsService : IVariantsService
    {
        private readonly ApplicationDbContext _context;
        public VariantsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Variants>> GetAllAsync()
        {
            return await _context.Variants.ToListAsync();
        }

        public async Task<Variants> GetByIdAsync(int id)
        {
            return await _context.Variants.FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Variants> CreateAsync(Variants model)
        {
            _context.Variants.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Variants model)
        {
            if (id != model.Id) return false;
            var variant = await _context.Variants.FindAsync(id);
            if (variant == null) return false;
            variant.VariantName = model.VariantName;
            variant.Type = model.Type;
            // ... add other properties as needed
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var variant = await _context.Variants.FindAsync(id);
            if (variant == null) return false;
            _context.Variants.Remove(variant);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<IEnumerable<Variants>> GetByProductIdAsync(int productId)
        {
            return await _context.Variants
                .Where(v => v.ProductId == productId)
                .ToListAsync();
        }

    }
}
