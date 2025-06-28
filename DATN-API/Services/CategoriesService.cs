using DATN_API.Models;
using DATN_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            return await _context.Categories.ToListAsync();
        }

        public async Task<Categories> GetByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<Categories> CreateAsync(Categories model)
        {
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
    }
}
