using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class ProductImagesService : IProductImagesService
    {
        private readonly ApplicationDbContext _context;
        public ProductImagesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductImages>> GetAllAsync()
        {
            return await _context.ProductImages.Include(p => p.Product).ToListAsync();
        }

        public async Task<ProductImages> GetByIdAsync(int id)
        {
            return await _context.ProductImages.Include(p => p.Product).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<ProductImages> CreateAsync(ProductImages model)
        {
            _context.ProductImages.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, ProductImages model)
        {
            if (id != model.Id) return false;
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null) return false;
            image.ProductId = model.ProductId;
            image.Media = model.Media;
            // ... add other properties as needed
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null) return false;
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
