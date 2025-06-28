using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class DecoratesService : IDecoratesService
    {
        private readonly ApplicationDbContext _context;
        public DecoratesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Decorates>> GetAllAsync()
        {
            return await _context.Decorates.Include(d => d.User).ToListAsync();
        }

        public async Task<Decorates> GetByIdAsync(int id)
        {
            return await _context.Decorates.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Decorates> CreateAsync(Decorates model)
        {
            _context.Decorates.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Decorates model)
        {
            if (id != model.Id) return false;
            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null) return false;
            decorate.UserId = model.UserId;
            decorate.Title = model.Title;
            decorate.Image = model.Image;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null) return false;
            _context.Decorates.Remove(decorate);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
