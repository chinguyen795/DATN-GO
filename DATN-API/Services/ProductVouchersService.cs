using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class ProductVouchersService : IProductVouchersService
    {
        private readonly ApplicationDbContext _context;
        public ProductVouchersService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductVouchers>> GetAllAsync()
        {
            return await _context.ProductVouchers.Include(pv => pv.Product).Include(pv => pv.Voucher).ToListAsync();
        }

        public async Task<ProductVouchers> GetByIdAsync(int id)
        {
            return await _context.ProductVouchers.Include(pv => pv.Product).Include(pv => pv.Voucher).FirstOrDefaultAsync(pv => pv.Id == id);
        }

        public async Task<ProductVouchers> CreateAsync(ProductVouchers model)
        {
            _context.ProductVouchers.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, ProductVouchers model)
        {
            if (id != model.Id) return false;
            var entity = await _context.ProductVouchers.FindAsync(id);
            if (entity == null) return false;
            entity.ProductId = model.ProductId;
            entity.VoucherId = model.VoucherId;
            // ... add other properties as needed
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ProductVouchers.FindAsync(id);
            if (entity == null) return false;
            _context.ProductVouchers.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
