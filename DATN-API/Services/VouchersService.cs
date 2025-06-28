using DATN_API.Models;
using DATN_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class VouchersService : IVouchersService
    {
        private readonly Data.ApplicationDbContext _context;
        public VouchersService(Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Vouchers>> GetAllVouchersAsync()
        {
            return await _context.Vouchers.ToListAsync();
        }

        public async Task<Vouchers?> GetVoucherByIdAsync(int id)
        {
            return await _context.Vouchers.FindAsync(id);
        }

        public async Task<Vouchers> CreateVoucherAsync(Vouchers voucher)
        {
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
            return voucher;
        }

        public async Task<Vouchers?> UpdateVoucherAsync(int id, Vouchers voucher)
        {
            var existing = await _context.Vouchers.FindAsync(id);
            if (existing == null) return null;
            _context.Entry(existing).CurrentValues.SetValues(voucher);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteVoucherAsync(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return false;
            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
