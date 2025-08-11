using DATN_API.ViewModels.Cart;
using DATN_API.ViewModels.GHTK;

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
        Task<string> CreateGHTKOrderAsync(int userId, int addressId);
        Task<bool> CancelGHTKOrderAsync(string orderCode, int userId);
        Task<GHTKOrderStatusViewModel> GetGHTKOrderStatusAsync(string orderCode);
        Task<int> ClearSelectedAsync(int userId);

    }
}
