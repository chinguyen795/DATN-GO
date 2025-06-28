using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class ReviewMediasService : IReviewMediasService
    {
        private readonly ApplicationDbContext _context;
        public ReviewMediasService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReviewMedias>> GetAllAsync()
        {
            return await _context.ReviewMedias.Include(rm => rm.Review).ToListAsync();
        }

        public async Task<ReviewMedias> GetByIdAsync(int id)
        {
            return await _context.ReviewMedias.Include(rm => rm.Review).FirstOrDefaultAsync(rm => rm.Id == id);
        }

        public async Task<ReviewMedias> CreateAsync(ReviewMedias model)
        {
            _context.ReviewMedias.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, ReviewMedias model)
        {
            if (id != model.Id) return false;
            var media = await _context.ReviewMedias.FindAsync(id);
            if (media == null) return false;
            media.ReviewId = model.ReviewId;
            media.Media = model.Media;
            // ... add other properties as needed
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var media = await _context.ReviewMedias.FindAsync(id);
            if (media == null) return false;
            _context.ReviewMedias.Remove(media);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
