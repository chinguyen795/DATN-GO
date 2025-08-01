using DATN_API.ViewModels.Cart;

namespace DATN_API.Interfaces
{
    public interface ICartService
    {
        Task<bool> AddToCartAsync(AddToCartRequest request);
        Task<CartSummaryViewModel> GetCartByUserIdAsync(int userId);
        Task<bool> RemoveFromCartAsync(int cartId);
        Task<bool> UpdateQuantityAsync(int cartId, int newQuantity);

    }
}
