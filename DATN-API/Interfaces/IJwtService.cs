using DATN_API.Models;

namespace DATN_API.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(Users user);
    }
}