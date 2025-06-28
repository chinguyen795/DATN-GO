using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IMessageMediasService
    {
        Task<IEnumerable<MessageMedias>> GetAllAsync();
        Task<MessageMedias> GetByIdAsync(int id);
        Task<MessageMedias> CreateAsync(MessageMedias model);
        Task<bool> UpdateAsync(int id, MessageMedias model);
        Task<bool> DeleteAsync(int id);
    }
}
