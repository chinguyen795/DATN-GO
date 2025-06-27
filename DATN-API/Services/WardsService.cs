using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DATN_API.Services
{
    public class WardsService : IWardsService
    {
        private readonly ApplicationDbContext _context;
        public WardsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Wards>> GetAllAsync()
        {
            return await _context.Wards.Include(w => w.District).ToListAsync();
        }

        public async Task<Wards> GetByIdAsync(int id)
        {
            return await _context.Wards.Include(w => w.District).FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<IEnumerable<Wards>> GetByDistrictIdAsync(int districtId)
        {
            return await _context.Wards.Where(w => w.DistrictId == districtId).ToListAsync();
        }

        public async Task<Wards> CreateAsync(Wards model)
        {
            _context.Wards.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Wards model)
        {
            if (id != model.Id) return false;
            var ward = await _context.Wards.FindAsync(id);
            if (ward == null) return false;
            ward.DistrictId = model.DistrictId;
            ward.WardName = model.WardName;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var ward = await _context.Wards.FindAsync(id);
            if (ward == null) return false;
            _context.Wards.Remove(ward);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
