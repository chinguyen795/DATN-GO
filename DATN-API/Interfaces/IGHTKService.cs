using DATN_API.ViewModels.GHTK;

namespace DATN_API.Interfaces
{
    public interface IGHTKService
    {
        string MapStatusText(int statusId);
        Task<GHTKOrderStatusViewModel> GetStatusByLabelIdAsync(string labelId);
        Task<int?> CalculateShippingFeeAsync(GHTKFeeRequestViewModel model);
    }
}
