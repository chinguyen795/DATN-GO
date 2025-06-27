using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class StoresService : IStoresService
    {
        private readonly ApplicationDbContext _context;
        public StoresService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Stores>> GetAllAsync()
        {
            return await _context.Stores.ToListAsync();
        }

        public async Task<Stores> GetByIdAsync(int id)
        {
            return await _context.Stores.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Stores> CreateAsync(Stores model)
        {
            _context.Stores.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Stores model)
        {
            if (id != model.Id) return false;
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return false;
            store.Name = model.Name;
            store.Status = model.Status;
            // ... add other properties as needed
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return false;
            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
