using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Services
{
    public class VouchersService : IVouchersService
    {
        private readonly Data.ApplicationDbContext _context;
        public VouchersService(Data.ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Vouchers>> GetAllVouchersAsync()
            => await _context.Vouchers
                .Include(v => v.Categories)
                .Include(v => v.ProductVouchers)
                .ToListAsync();

        public async Task<Vouchers?> GetVoucherByIdAsync(int id)
            => await _context.Vouchers
                .Include(v => v.Categories)
                .Include(v => v.ProductVouchers)
                .FirstOrDefaultAsync(v => v.Id == id);

        public async Task<Vouchers> CreateVoucherAsync(Vouchers voucher)
        {
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
            return voucher;
        }

        public async Task<Vouchers?> UpdateVoucherAsync(int id, Vouchers voucher)
        {
            var existing = await _context.Vouchers
                .Include(v => v.Categories)
                .Include(v => v.ProductVouchers)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (existing == null) return null;

            _context.Entry(existing).CurrentValues.SetValues(voucher);

            // categories
            existing.Categories ??= new List<Categories>();
            existing.Categories.Clear();
            foreach (var c in voucher.Categories ?? Enumerable.Empty<Categories>())
                existing.Categories.Add(c);

            // products
            _context.ProductVouchers.RemoveRange(existing.ProductVouchers ?? Enumerable.Empty<ProductVouchers>());
            existing.ProductVouchers = voucher.ProductVouchers ?? new List<ProductVouchers>();

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
            var query = _context.Vouchers
                .Include(v => v.Categories)
                .Include(v => v.ProductVouchers)
                .AsQueryable();

            if (storeId.HasValue) query = query.Where(v => v.StoreId == storeId.Value);
            else query = query.Where(v => v.StoreId == null);

            return await query.ToListAsync();
        }

        public (decimal discountOnSubtotal, decimal discountOnShipping, string reason) ApplyVoucher(
            Vouchers v,
            decimal orderSubtotal,
            IEnumerable<int> productIdsInCart,
            IEnumerable<int>? categoryIdsInCart)
        {
            var now = DateTime.UtcNow;
            if (now < v.StartDate || now > v.EndDate)
                return (0, 0, "Voucher hết hạn hoặc chưa bắt đầu.");

            if (v.UsedCount >= v.Quantity)
                return (0, 0, "Voucher đã hết lượt.");

            if (orderSubtotal < v.MinOrder)
                return (0, 0, $"Đơn tối thiểu {v.MinOrder:N0}.");

            // ===== PHẠM VI =====
            bool scopeOk = false;

            // a) All categories
            if (v.ApplyAllCategories) scopeOk = true;

            // b) any selected categories
            if (!scopeOk && v.Categories?.Any() == true && categoryIdsInCart != null)
            {
                var catSet = categoryIdsInCart.ToHashSet();
                if (v.Categories.Any(c => catSet.Contains(c.Id))) scopeOk = true;
            }

            // c) All products
            if (!scopeOk && v.ApplyAllProducts) scopeOk = true;

            // d) selected products
            if (!scopeOk && v.ProductVouchers?.Any() == true)
            {
                var pidSet = productIdsInCart.ToHashSet();
                if (v.ProductVouchers.Any(pv => pidSet.Contains(pv.ProductId))) scopeOk = true;
            }

            if (!scopeOk) return (0, 0, "Voucher không áp dụng cho sản phẩm/danh mục đã chọn.");

            // ===== TÍNH GIẢM =====
            decimal discountSub;
            if (v.IsPercentage)
            {
                var perc = Math.Clamp((double)v.Reduce, 0, 100);
                discountSub = orderSubtotal * (decimal)perc / 100m;
                if (v.MaxDiscount is decimal cap && cap > 0 && discountSub > cap)
                    discountSub = cap;
            }
            else
            {
                discountSub = Math.Min(orderSubtotal, v.Reduce);
            }

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
                if (v.MaxDiscount.HasValue) return "MaxDiscount chỉ dùng cho phần trăm. Hãy để trống.";
            }

            // Shop/Sàn
            if (v.StoreId == null && v.CreatedByRoleId != 3) return "Chỉ admin (roleId=3) tạo voucher sàn.";
            if (v.StoreId != null && v.CreatedByRoleId != 2) return "Chỉ shop (roleId=2) tạo voucher shop.";

            // PHẠM VI – cho phép 1 trong 4:
            var hasAnyScope =
                v.ApplyAllCategories ||
                v.ApplyAllProducts ||
                (v.Categories?.Any() == true) ||
                (v.ProductVouchers?.Any() == true);

            if (!hasAnyScope) return "Voucher phải áp dụng: tất cả danh mục, hoặc tất cả sản phẩm, hoặc danh sách danh mục, hoặc danh sách sản phẩm.";

            return null;
        }

        public async Task RevertRedeemAsync(int voucherId)
        {
            var v = await _context.Vouchers.FirstOrDefaultAsync(x => x.Id == voucherId);
            if (v != null && v.UsedCount > 0)
            {
                v.UsedCount = Math.Max(0, v.UsedCount - 1);
                await _context.SaveChangesAsync();
            }
        }
    }
}