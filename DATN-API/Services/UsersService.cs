using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class UsersService : IUsersService
    {
        private readonly ApplicationDbContext _context;
        public UsersService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Users>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<Users> GetByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Users> CreateAsync(Users model)
        {
            _context.Users.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Users model)
        {
            if (id != model.Id) return false;
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;
            // Update properties here (add more as needed)
            user.Status = model.Status;
            user.Email = model.Email;
            // ... add other properties as needed
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
