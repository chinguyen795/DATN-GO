using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class RatingStoresService : IRatingStoresService
    {
        private readonly ApplicationDbContext _context;
        public RatingStoresService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RatingStores>> GetAllAsync()
        {
            return await _context.RatingStores.Include(r => r.User).Include(r => r.Store).ToListAsync();
        }

        public async Task<RatingStores> GetByIdAsync(int id)
        {
            return await _context.RatingStores.Include(r => r.User).Include(r => r.Store).FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<RatingStores> CreateAsync(RatingStores model)
        {
            _context.RatingStores.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, RatingStores model)
        {
            if (id != model.Id) return false;
            var rating = await _context.RatingStores.FindAsync(id);
            if (rating == null) return false;
            rating.UserId = model.UserId;
            rating.StoreId = model.StoreId;
            rating.Rating = model.Rating;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var rating = await _context.RatingStores.FindAsync(id);
            if (rating == null) return false;
            _context.RatingStores.Remove(rating);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
