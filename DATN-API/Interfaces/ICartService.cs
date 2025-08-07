using DATN_API.ViewModels.Cart;

namespace DATN_API.Interfaces
{
    public interface ICartService
    {
        Task<bool> AddToCartAsync(AddToCartRequest request);
        Task<CartSummaryViewModel> GetCartByUserIdAsync(int userId);
        Task<bool> RemoveFromCartAsync(int cartId);
        Task<bool> UpdateQuantityAsync(int cartId, int newQuantity);
        Task UpdateSelectionAsync(List<int> selectedCartIds);
        Task<List<ShippingGroupViewModel>> GetShippingGroupsByUserIdAsync(int userId, int addressId);

    }
}
