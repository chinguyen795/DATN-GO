using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.ViewModels.Request;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Services
{
    public class VariantCompositionService : IVariantCompositionService
    {
        private readonly ApplicationDbContext _context;

        public VariantCompositionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<VariantComposition>> GetAllAsync()
        {
            return await _context.VariantCompositions
                .Include(vc => vc.Variant)
                .Include(vc => vc.VariantValue)
                .Include(vc => vc.Product)
                .Include(vc => vc.ProductVariant)
                .ToListAsync();
        }

        public async Task<VariantComposition?> GetByIdAsync(int id)
        {
            return await _context.VariantCompositions
                .Include(vc => vc.Variant)
                .Include(vc => vc.VariantValue)
                .FirstOrDefaultAsync(vc => vc.Id == id);
        }

        public async Task<List<VariantComposition>> GetByProductVariantIdAsync(int productVariantId)
        {
            return await _context.VariantCompositions
                .Where(vc => vc.ProductVariantId == productVariantId)
                .Include(vc => vc.Variant)
                .Include(vc => vc.VariantValue)
                .ToListAsync();
        }

        public async Task AddMultipleAsync(int productId, int productVariantId, List<(int variantId, int variantValueId)> pairs)
        {
            var compositions = pairs.Select(pair => new VariantComposition
            {
                ProductId = productId,
                ProductVariantId = productVariantId,
                VariantId = pair.variantId,
                VariantValueId = pair.variantValueId
            }).ToList();

            _context.VariantCompositions.AddRange(compositions);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(VariantComposition variantComposition)
        {
            var existing = await _context.VariantCompositions.FindAsync(variantComposition.Id);
            if (existing == null)
                return;

            existing.ProductId = variantComposition.ProductId;
            existing.ProductVariantId = variantComposition.ProductVariantId;
            existing.VariantId = variantComposition.VariantId;
            existing.VariantValueId = variantComposition.VariantValueId;

            await _context.SaveChangesAsync();
        }


        public async Task DeleteAsync(int id)
        {
            var item = await _context.VariantCompositions.FindAsync(id);
            if (item != null)
            {
                _context.VariantCompositions.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<VariantCombinationViewModel>> GetVariantCombinationsByProductIdAsync(int productId)
        {
            var productVariants = await _context.ProductVariants
                .Where(pv => pv.ProductId == productId)
                .ToListAsync();

            var variantCompositions = await _context.VariantCompositions
                .Where(vc => vc.VariantValueId.HasValue)
                .ToListAsync();

            var result = productVariants.Select(pv => new VariantCombinationViewModel
            {
                ProductVariantId = pv.Id,
                Price = pv.Price,
                Quantity = pv.Quantity,
                Image = pv.Image,
                VariantValueIds = variantCompositions
                    .Where(vc => vc.ProductVariantId == pv.Id)
                    .Select(vc => vc.VariantValueId.Value)
                    .Distinct()
                    .ToList()
            }).ToList();

            return result;
        }
        public async Task<List<VariantComposition>> GetByProductIdAsync(int productId)
        {
            return await _context.VariantCompositions
                .Where(vc => vc.ProductId == productId)
                .Include(vc => vc.Variant)
                .Include(vc => vc.VariantValue)
                .Include(vc => vc.ProductVariant)
                .ToListAsync();
        }
        public async Task<VariantComposition> CreateAsync(VariantComposition variantComposition)
        {
            

            _context.VariantCompositions.Add(variantComposition);
            await _context.SaveChangesAsync();
            return variantComposition;
        }


    }

}