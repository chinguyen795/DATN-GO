using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class ProductsService : IProductsService
    {
        private readonly ApplicationDbContext _context;
        public ProductsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Products>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Products> GetByIdAsync(int id)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Products> CreateAsync(Products model)
        {
            _context.Products.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Products model)
        {
            if (id != model.Id) return false;
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;
            // Update basic properties (add more as needed)
            product.Name = model.Name;
            product.Status = model.Status;
            product.Description = model.Description;
            // ... add other properties as needed
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
