using DATN_API.ViewModels.GHTK;

namespace DATN_API.Interfaces
{
    public interface IGHTKService
    {
        
        Task<int?> CalculateShippingFeeAsync(GHTKFeeRequestViewModel model);
        Task<string?> CreateOrderAsync(GHTKCreateOrderRequest payload);
        Task<(bool Success, string? Label, string Raw)> CreateOrderDebugAsync(GHTKCreateOrderRequest payload);
    }
}
