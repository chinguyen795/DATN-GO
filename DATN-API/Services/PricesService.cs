using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class PricesService : IPricesService
    {
        private readonly ApplicationDbContext _context;
        public PricesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Prices>> GetAllAsync()
        {
            return await _context.Prices.ToListAsync();
        }

        public async Task<Prices> GetByIdAsync(int id)
        {
            return await _context.Prices.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Prices> CreateAsync(Prices model)
        {
            _context.Prices.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Prices model)
        {
            if (id != model.Id) return false;
            var entity = await _context.Prices.FindAsync(id);
            if (entity == null) return false;
            entity.ProductId = model.ProductId;
            entity.Price = model.Price;
            
            // ... add other properties as needed
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Prices.FindAsync(id);
            if (entity == null) return false;
            _context.Prices.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<decimal?> GetMinPriceByProductIdAsync(int productId)
        {
            return await _context.Prices
                .Where(p => p.ProductId == productId)
                .OrderBy(p => p.Price)
                .Select(p => (decimal?)(p.Price / 100))
                .FirstOrDefaultAsync();
        }
    }
}
