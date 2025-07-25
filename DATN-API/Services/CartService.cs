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
            var existingCart = await _context.Carts.FirstOrDefaultAsync(c =>
                c.UserId == request.UserId &&
                c.ProductId == request.ProductId);

            if (existingCart != null)
            {
                existingCart.Quantity += request.Quantity;
                await _context.SaveChangesAsync();
                return true;
            }

            var cart = new Carts
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CreateAt = DateTime.Now
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            foreach (var variantValueId in request.VariantValueIds)
            {
                var cartItemVariant = new CartItemVariants
                {
                    CartId = cart.Id,
                    VariantValueId = variantValueId
                };
                _context.Set<CartItemVariants>().Add(cartItemVariant);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CartItemViewModel>> GetCartByUserIdAsync(int userId)
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
                            .OrderBy(id => id)
                            .ToListAsync();

                        if (compositionIds.Where(id => id.HasValue).Select(id => id.Value).SequenceEqual(variantValueIds))
                        {
                            price = pv.Price;
                            break;
                        }

                    }
                }
                else
                {
                    var productPrice = await _context.Prices
                        .Where(p => p.ProductId == cart.ProductId)
                        .Select(p => (decimal?)p.Price)
                        .FirstOrDefaultAsync();

                    price = productPrice ?? 0;
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
                    Variants = variantTexts
                });
            }

            return result;
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
