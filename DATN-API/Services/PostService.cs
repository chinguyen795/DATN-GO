using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class PostsService : IPostsService
    {
        private readonly ApplicationDbContext _context;

        public PostsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Posts>> GetAllAsync()
        {
            return await _context.Posts.Include(p => p.User)
                                       .OrderByDescending(p => p.CreateAt)
                                       .ToListAsync();
        }

        public async Task<Posts?> GetByIdAsync(int id)
        {
            return await _context.Posts.Include(p => p.User)
                                       .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Posts> CreateAsync(Posts model)
        {
            model.CreateAt = DateTime.UtcNow;
            model.UpdateAt = DateTime.UtcNow;
            model.Status = PostStatus.NotApproved;

            _context.Posts.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Posts model)
        {
            if (id != model.Id) return false;
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return false;

            post.Content = model.Content;
            post.Image = model.Image;
            post.Status = model.Status;
            post.UpdateAt = DateTime.UtcNow;
            post.UserId = model.UserId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return false;

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<IEnumerable<Posts>> GetByUserIdAsync(int userId)
        {
            return await _context.Posts
                                 .Where(p => p.UserId == userId)
                                 .Include(p => p.User)
                                 .OrderByDescending(p => p.CreateAt)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Posts>> GetPendingPostsAsync()
        {
            return await _context.Posts
                                 .Include(p => p.User)
                                 .Where(p => p.Status == PostStatus.NotApproved)
                                 .ToListAsync();
        }

        public async Task<bool> ApprovePostAsync(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return false;

            post.Status = PostStatus.Approved;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectPostAsync(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return false;

            post.Status = PostStatus.NotApproved;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
