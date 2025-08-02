using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IUsersService
    {
        Task<IEnumerable<Users>> GetAllAsync();
        Task<Users> GetByIdAsync(int id);
        Task<Users> CreateAsync(Users model);
        Task<bool> UpdateAsync(int id, Users model);
        Task<bool> DeleteAsync(int id);
        Task<Users> GetByEmailAsync(string email);
    }
}
