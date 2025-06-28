using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class VariantValuesService : IVariantValuesService
    {
        private readonly ApplicationDbContext _context;
        public VariantValuesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<VariantValues>> GetAllAsync()
        {
            return await _context.VariantValues.ToListAsync();
        }

        public async Task<VariantValues> GetByIdAsync(int id)
        {
            return await _context.VariantValues.FirstOrDefaultAsync(vv => vv.Id == id);
        }

        public async Task<VariantValues> CreateAsync(VariantValues model)
        {
            _context.VariantValues.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, VariantValues model)
        {
            if (id != model.Id) return false;
            var value = await _context.VariantValues.FindAsync(id);
            if (value == null) return false;
            value.ValueName = model.ValueName;
            value.Type = model.Type;
            value.VariantId = model.VariantId;
            // ... add other properties as needed
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var value = await _context.VariantValues.FindAsync(id);
            if (value == null) return false;
            _context.VariantValues.Remove(value);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
