using DATN_API.Models;
using DATN_API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Services
{
    public class VouchersService : IVouchersService
    {
        private readonly Data.ApplicationDbContext _context;
        public VouchersService(Data.ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Vouchers>> GetAllVouchersAsync()
            => await _context.Vouchers.ToListAsync();

        public async Task<Vouchers?> GetVoucherByIdAsync(int id)
            => await _context.Vouchers.FindAsync(id);

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

        public async Task<IEnumerable<Vouchers>> GetVouchersByStoreOrAdminAsync(int? storeId)
        {
            var query = _context.Vouchers.AsQueryable();
            if (storeId.HasValue) query = query.Where(v => v.StoreId == storeId.Value);
            else query = query.Where(v => v.StoreId == null);
            return await query.ToListAsync();
        }

        public (decimal discountOnSubtotal, decimal discountOnShipping, string reason) ApplyVoucher(
            Vouchers v,
            decimal orderSubtotal,
            IEnumerable<int> productIdsInCart,
            int? categoryIdInCart)
        {
            var now = DateTime.UtcNow;
            if (now < v.StartDate || now > v.EndDate)
                return (0, 0, "Voucher hết hạn hoặc chưa bắt đầu.");

            if (v.UsedCount >= v.Quantity)
                return (0, 0, "Voucher đã hết lượt.");

            if (orderSubtotal < v.MinOrder)
                return (0, 0, $"Đơn tối thiểu {v.MinOrder:N0}.");

            // Phạm vi áp dụng: chỉ Category hoặc danh sách ProductVouchers
            bool scopeOk =
                (v.CategoryId != null && categoryIdInCart == v.CategoryId)
                || (v.ProductVouchers?.Any(pv => productIdsInCart.Contains(pv.ProductId)) == true);

            if (!scopeOk)
                return (0, 0, "Voucher không áp dụng cho sản phẩm đã chọn.");

            decimal discountSub = 0;

            // Giảm trên subtotal (chỉ % hoặc số tiền)
            if (v.IsPercentage)
            {
                var perc = Math.Max(0, Math.Min(100, (double)v.Reduce));
                discountSub = orderSubtotal * ((decimal)perc / 100m);
                if (v.MaxDiscount is decimal cap && cap > 0 && discountSub > cap)
                    discountSub = cap;
            }
            else
            {
                discountSub = v.Reduce;
                if (discountSub > orderSubtotal) discountSub = orderSubtotal;
            }

            // Không còn freeship => DiscountOnShipping luôn 0
            return (discountSub, 0m, "OK");
        }

        public async Task<(bool ok, string reason)> RedeemVoucherAsync(int voucherId)
        {
            var v = await _context.Vouchers.FirstOrDefaultAsync(x => x.Id == voucherId);
            if (v == null) return (false, "Không tìm thấy voucher.");

            if (v.UsedCount >= v.Quantity) return (false, "Voucher đã hết lượt.");
            var now = DateTime.UtcNow;
            if (now < v.StartDate || now > v.EndDate) return (false, "Voucher đã hết hạn hoặc chưa bắt đầu.");

            v.UsedCount += 1;
            await _context.SaveChangesAsync();
            return (true, "OK");
        }

        public string? ValidateForCreateOrUpdate(Vouchers v, bool isCreate)
        {
            if (v.StartDate >= v.EndDate) return "Thời gian không hợp lệ.";
            if (v.Quantity < 1) return "Số lượng phải từ 1 trở lên.";
            if (v.MinOrder < 0) return "MinOrder không hợp lệ.";
            if (v.IsPercentage)
            {
                if (v.Reduce <= 0 || v.Reduce > 100) return "Phần trăm giảm phải trong (0,100].";
            }
            else
            {
                if (v.Reduce <= 0) return "Số tiền giảm phải > 0.";
            }

            // Role logic
            if (v.StoreId == null && v.CreatedByRoleId != 3) return "Chỉ admin (roleId=3) được tạo/sửa voucher sàn.";
            if (v.StoreId != null && v.CreatedByRoleId != 2) return "Chỉ shop (roleId=2) được tạo/sửa voucher của shop.";

            // Scope logic: KHÔNG còn "áp dụng tất cả sản phẩm" => bắt buộc có CategoryId hoặc ProductVouchers
            if (v.CategoryId == null && (v.ProductVouchers == null || !v.ProductVouchers.Any()))
                return "Voucher phải áp dụng theo Category hoặc theo danh sách sản phẩm.";

            return null;
        }
    }
}