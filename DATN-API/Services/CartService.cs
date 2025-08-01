using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.ViewModels.Cart;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DATN_API.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddToCartAsync(AddToCartRequest request)
        {
            var variantValueIds = request.VariantValueIds?.OrderBy(id => id).ToList() ?? new List<int>();

            var allExistingCarts = await _context.Carts
                .Where(c => c.UserId == request.UserId && c.ProductId == request.ProductId)
                .ToListAsync();

            Carts? existingCart = null;

            foreach (var existing in allExistingCarts)
            {
                var cartVariantValueIds = await _context.Set<CartItemVariants>()
                    .Where(v => v.CartId == existing.Id)
                    .Select(v => v.VariantValueId)
                    .OrderBy(id => id)
                    .ToListAsync();

                if (variantValueIds.SequenceEqual(cartVariantValueIds))
                {
                    existingCart = existing;
                    break;
                }
            }



            int availableQuantity = 0;

            if (variantValueIds.Count > 0)
            {
                var productVariants = await _context.ProductVariants
                    .Where(pv => pv.ProductId == request.ProductId)
                    .ToListAsync();

                foreach (var pv in productVariants)
                {
                    var compositionIds = await _context.VariantCompositions
                        .Where(vc => vc.ProductVariantId == pv.Id)
                        .Select(vc => vc.VariantValueId)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .OrderBy(id => id)
                        .ToListAsync();

                    if (compositionIds.SequenceEqual(variantValueIds))
                    {
                        availableQuantity = pv.Quantity;

                        // Nếu đã tồn tại cart → kiểm tra tổng quantity
                        if (existingCart != null)
                        {
                            if (existingCart.Quantity + request.Quantity > availableQuantity)
                                return false;
                        }
                        else
                        {
                            if (request.Quantity > availableQuantity)
                                return false;
                        }

                        break;
                    }
                }

                if (availableQuantity == 0)
                    return false; // Không khớp với biến thể nào → không thể thêm
            }
            else
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId);
                if (product == null) return false;

                availableQuantity = product.Quantity;

                if (existingCart != null)
                {
                    if (existingCart.Quantity + request.Quantity > availableQuantity)
                        return false;
                }
                else
                {
                    if (request.Quantity > availableQuantity)
                        return false;
                }
            }

            // Cập nhật nếu đã có
            if (existingCart != null)
            {
                existingCart.Quantity += request.Quantity;
                await _context.SaveChangesAsync();
                return true;
            }

            // Thêm mới cart
            var cart = new Carts
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CreateAt = DateTime.Now
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            foreach (var variantValueId in variantValueIds)
            {
                _context.Set<CartItemVariants>().Add(new CartItemVariants
                {
                    CartId = cart.Id,
                    VariantValueId = variantValueId
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CartSummaryViewModel> GetCartByUserIdAsync(int userId)
        {
            var carts = await _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            var result = new List<CartItemViewModel>();

            foreach (var cart in carts)
            {
                var variants = await _context.Set<CartItemVariants>()
                    .Where(cv => cv.CartId == cart.Id)
                    .Include(cv => cv.VariantValue)
                        .ThenInclude(vv => vv.Variant)
                    .ToListAsync();

                var variantValueIds = variants.Select(v => v.VariantValueId).OrderBy(id => id).ToList();
                decimal price = 0;
                int maxQuantity = 0;

                if (variantValueIds.Any())
                {
                    var productVariants = await _context.ProductVariants
                        .Where(pv => pv.ProductId == cart.ProductId)
                        .ToListAsync();

                    foreach (var pv in productVariants)
                    {
                        var compositionIds = await _context.VariantCompositions
                            .Where(vc => vc.ProductVariantId == pv.Id)
                            .Select(vc => vc.VariantValueId)
                            .Where(id => id.HasValue)
                            .Select(id => id.Value)
                            .OrderBy(id => id)
                            .ToListAsync();

                        if (compositionIds.SequenceEqual(variantValueIds))
                        {
                            price = pv.Price;
                            maxQuantity = pv.Quantity;
                            break;
                        }
                    }
                }
                else
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Id == cart.ProductId);

                    price = await _context.Prices
                        .Where(p => p.ProductId == cart.ProductId)
                        .Select(p => (decimal?)p.Price)
                        .FirstOrDefaultAsync() ?? 0;

                    maxQuantity = product?.Quantity ?? 0;
                }

                var variantTexts = variants
                    .Select(v => $"{v.VariantValue?.Variant?.VariantName}: {v.VariantValue?.ValueName}")
                    .ToList();

                result.Add(new CartItemViewModel
                {
                    CartId = cart.Id,
                    ProductId = cart.ProductId,
                    ProductName = cart.Product?.Name ?? "Sản phẩm không tồn tại",
                    Image = cart.Product?.MainImage ?? "",
                    Quantity = cart.Quantity,
                    Price = (int)price,
                    MaxQuantity = maxQuantity,
                    Variants = variantTexts
                });
            }

            // ✅ Lấy địa chỉ mặc định mới nhất
            var address = await _context.Addresses
                .Include(a => a.City)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.UpdateAt)
                .FirstOrDefaultAsync();

            string? fullAddress = null;

            if (address != null)
            {
                var district = await _context.Districts.FirstOrDefaultAsync(d => d.CityId == address.City.Id);
                var ward = district != null
                    ? await _context.Wards.FirstOrDefaultAsync(w => w.DistrictId == district.Id)
                    : null;

                fullAddress = string.Join(", ", new[]
                {
            $"{address.Name} - {address.Phone}",
            address.Description,
            ward?.WardName,
            district?.DistrictName,
            address.City?.CityName
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            // ✅ Lấy danh sách voucher đã lưu, còn hạn, chưa dùng
            var now = DateTime.Now;
            var userVouchers = await _context.UserVouchers
                .Where(uv => uv.UserId == userId && !uv.IsUsed && uv.Voucher.EndDate >= now)
                .Include(uv => uv.Voucher)
                .ThenInclude(v => v.Store)
                .ToListAsync();

            var vouchers = userVouchers.Select(uv => new UserVoucherViewModel
            {
                Id = uv.Id,
                VoucherId = uv.VoucherId,
                Reduce = uv.Voucher.Reduce,
                MinOrder = uv.Voucher.MinOrder,
                EndDate = uv.Voucher.EndDate,
                StoreName = uv.Voucher.Store?.Name ?? "Sàn TMĐT"
            }).ToList();

            return new CartSummaryViewModel
            {
                CartItems = result,
                FullAddress = fullAddress,
                Vouchers = vouchers
            };
        }

        public async Task<bool> RemoveFromCartAsync(int cartId)
        {
            var cart = await _context.Carts.FindAsync(cartId);
            if (cart == null) return false;

            var cartVariants = await _context.Set<CartItemVariants>()
                .Where(v => v.CartId == cartId)
                .ToListAsync();

            _context.Set<CartItemVariants>().RemoveRange(cartVariants);

            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateQuantityAsync(int cartId, int newQuantity)
        {
            var cart = await _context.Carts.FindAsync(cartId);
            if (cart == null || newQuantity < 1) return false;

            cart.Quantity = newQuantity;
            await _context.SaveChangesAsync();
            return true;
        }


    }

}
