using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class RolesService : IRolesService
    {
        private readonly ApplicationDbContext _context;
        public RolesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Roles>> GetAllAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task<Roles> GetByIdAsync(int id)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Roles> CreateAsync(Roles model)
        {
            _context.Roles.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Roles model)
        {
            if (id != model.Id) return false;
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;
            role.RoleName = model.RoleName;
            role.Status = model.Status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
