using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class CitiesService : ICitiesService
    {
        private readonly ApplicationDbContext _context;
        public CitiesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cities>> GetAllAsync()
        {
            return await _context.Cities.Include(c => c.Districts).ToListAsync();
        }

        public async Task<Cities> GetByIdAsync(int id)
        {
            return await _context.Cities.Include(c => c.Districts).FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cities> CreateAsync(Cities model)
        {
            _context.Cities.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Cities model)
        {
            if (id != model.Id) return false;
            var city = await _context.Cities.FindAsync(id);
            if (city == null) return false;
            city.CityName = model.CityName;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null) return false;
            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
