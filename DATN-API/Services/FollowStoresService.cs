using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class FollowStoresService : IFollowStoresService
    {
        private readonly ApplicationDbContext _context;
        public FollowStoresService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FollowStores>> GetAllAsync()
        {
            return await _context.FollowStores.Include(f => f.User).Include(f => f.Store).ToListAsync();
        }

        public async Task<FollowStores> GetByIdAsync(int id)
        {
            return await _context.FollowStores.Include(f => f.User).Include(f => f.Store).FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<FollowStores> CreateAsync(FollowStores model)
        {
            _context.FollowStores.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, FollowStores model)
        {
            if (id != model.Id) return false;
            var follow = await _context.FollowStores.FindAsync(id);
            if (follow == null) return false;
            follow.UserId = model.UserId;
            follow.StoreId = model.StoreId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var follow = await _context.FollowStores.FindAsync(id);
            if (follow == null) return false;
            _context.FollowStores.Remove(follow);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
