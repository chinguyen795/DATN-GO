using DATN_API.Models;
using DATN_API.ViewModels.Request;

namespace DATN_API.Interfaces
{
    public interface IVariantCompositionService
    {
        Task<List<VariantComposition>> GetAllAsync();
        Task<VariantComposition?> GetByIdAsync(int id);
        Task<List<VariantComposition>> GetByProductVariantIdAsync(int productVariantId);
        Task AddMultipleAsync(int productId, int productVariantId, List<(int variantId, int variantValueId)> pairs);
        Task UpdateAsync(VariantComposition variantComposition);
        Task DeleteAsync(int id);
        Task<List<VariantCombinationViewModel>> GetVariantCombinationsByProductIdAsync(int productId);

    }
}
