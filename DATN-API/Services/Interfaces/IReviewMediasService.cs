using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IReviewMediasService
    {
        Task<IEnumerable<ReviewMedias>> GetAllAsync();
        Task<ReviewMedias> GetByIdAsync(int id);
        Task<ReviewMedias> CreateAsync(ReviewMedias model);
        Task<bool> UpdateAsync(int id, ReviewMedias model);
        Task<bool> DeleteAsync(int id);
    }
}
