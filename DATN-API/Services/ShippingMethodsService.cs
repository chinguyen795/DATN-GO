using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class ShippingMethodsService : IShippingMethodsService
    {
        private readonly ApplicationDbContext _context;
        public ShippingMethodsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ShippingMethods>> GetAllAsync()
        {
            return await _context.ShippingMethods.Include(sm => sm.store).Include(sm => sm.Orders).ToListAsync();
        }

        public async Task<ShippingMethods> GetByIdAsync(int id)
        {
            return await _context.ShippingMethods.Include(sm => sm.store).Include(sm => sm.Orders).FirstOrDefaultAsync(sm => sm.Id == id);
        }

        public async Task<ShippingMethods> CreateAsync(ShippingMethods model)
        {
            _context.ShippingMethods.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, ShippingMethods model)
        {
            if (id != model.Id) return false;
            var entity = await _context.ShippingMethods.FindAsync(id);
            if (entity == null) return false;
            entity.StoreId = model.StoreId;
            entity.Price = model.Price;
            entity.MethodName = model.MethodName;
            // ... add other properties as needed
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ShippingMethods.FindAsync(id);
            if (entity == null) return false;
            _context.ShippingMethods.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
