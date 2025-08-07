using DATN_API.ViewModels.GHTK;

namespace DATN_API.Interfaces
{
    public interface IGHTKService
    {
        Task<int?> CalculateShippingFeeAsync(GHTKFeeRequestViewModel model);
    }
}
