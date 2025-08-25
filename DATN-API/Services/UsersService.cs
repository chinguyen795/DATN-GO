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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return false;

            // Map các field được phép sửa
            user.FullName = model.FullName?.Trim();
            user.Email = model.Email?.Trim();
            user.Phone = model.Phone?.Trim();
            user.Avatar = model.Avatar?.Trim();
            user.Status = model.Status;
            user.Gender = model.Gender;
            user.CitizenIdentityCard = model.CitizenIdentityCard?.Trim();
            user.BirthDay = model.BirthDay;

            // Tùy chính sách: chỉ cho admin sửa Role/Password
            user.RoleId = model.RoleId;
            user.Password = model.Password; // nếu nhận plaintext, hãy hash trước khi gán!

            // Luôn do server quyết định
            user.UpdateAt = DateTime.UtcNow;

            // Không cho phép sửa CreateAt
            _context.Entry(user).Property(u => u.CreateAt).IsModified = false;

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

        public async Task<Users> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
