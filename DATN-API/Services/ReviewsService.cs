using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class ReviewsService : IReviewsService
    {
        private readonly ApplicationDbContext _context;
        public ReviewsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Reviews>> GetAllAsync()
        {
            return await _context.Reviews.Include(r => r.User).Include(r => r.Product).ToListAsync();
        }

        public async Task<Reviews> GetByIdAsync(int id)
        {
            return await _context.Reviews.Include(r => r.User).Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Reviews> CreateAsync(Reviews model)
        {
            _context.Reviews.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Reviews model)
        {
            if (id != model.Id) return false;
            var entity = await _context.Reviews.FindAsync(id);
            if (entity == null) return false;
            entity.UserId = model.UserId;
            entity.ProductId = model.ProductId;
            entity.Rating = model.Rating;
            entity.CommentText = model.CommentText;
            entity.CreateAt = model.CreateAt;
            entity.UpdateAt = model.UpdateAt;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Reviews.FindAsync(id);
            if (entity == null) return false;
            _context.Reviews.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
