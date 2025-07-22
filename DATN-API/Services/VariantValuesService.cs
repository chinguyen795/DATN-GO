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
            value.VariantId = model.VariantId;
            value.ValueName = model.ValueName;
            value.Type = model.Type;
            value.Image = model.Image;
            value.colorHex = model.colorHex;

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
        public async Task<List<object>> GetGroupedVariantsByProductAsync(int productId)
        {
            var variants = await _context.Variants
                .Where(v => v.ProductId == productId)
                .ToListAsync();

            var variantIds = variants.Select(v => v.Id).ToList();

            var variantValues = await _context.VariantValues
                .Where(vv => variantIds.Contains(vv.VariantId))
                .ToListAsync();

            var result = variants.Select(variant => new
            {
                VariantName = variant.VariantName,
                Values = variantValues
                    .Where(vv => vv.VariantId == variant.Id)
                    .Select(vv => vv.ValueName)
                    .Distinct()
                    .ToList()
            }).ToList<object>();

            return result;
        }
        public async Task<IEnumerable<VariantValues>> GetByVariantIdAsync(int variantId)
        {
            return await _context.VariantValues
                .Where(vv => vv.VariantId == variantId)
                .ToListAsync();
        }

    }
}
