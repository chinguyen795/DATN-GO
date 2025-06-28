using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class MessageMediasService : IMessageMediasService
    {
        private readonly ApplicationDbContext _context;
        public MessageMediasService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MessageMedias>> GetAllAsync()
        {
            return await _context.MessageMedias.Include(m => m.Message).ToListAsync();
        }

        public async Task<MessageMedias> GetByIdAsync(int id)
        {
            return await _context.MessageMedias.Include(m => m.Message).FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<MessageMedias> CreateAsync(MessageMedias model)
        {
            _context.MessageMedias.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, MessageMedias model)
        {
            if (id != model.Id) return false;
            var media = await _context.MessageMedias.FindAsync(id);
            if (media == null) return false;
            media.MessageId = model.MessageId;
            media.Media = model.Media;
            // ... add other properties as needed
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var media = await _context.MessageMedias.FindAsync(id);
            if (media == null) return false;
            _context.MessageMedias.Remove(media);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
