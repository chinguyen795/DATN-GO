using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IPostsService
    {
        Task<IEnumerable<Posts>> GetAllAsync();
        Task<Posts> GetByIdAsync(int id);
        Task<Posts> CreateAsync(Posts model);
        Task<bool> UpdateAsync(int id, Posts model);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Posts>> GetByUserIdAsync(int userId);

        Task<IEnumerable<Posts>> GetPendingPostsAsync();
        Task<bool> ApprovePostAsync(int id);
        Task<bool> RejectPostAsync(int id);
    }
}
