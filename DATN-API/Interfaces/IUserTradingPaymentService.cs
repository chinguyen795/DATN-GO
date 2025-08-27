using DATN_API.Models;

namespace DATN_API.Interfaces
{
    public interface IUserTradingPaymentService
    {
        Task<List<UserTradingPayment>> GetAllAsync();
        Task<UserTradingPayment?> GetByIdAsync(int id);
        Task<UserTradingPayment> CreateAsync(UserTradingPayment payment);
        Task<UserTradingPayment?> UpdateAsync(int id, UserTradingPayment payment);
        Task<bool> DeleteAsync(int id);
        Task<bool> RejectAsync(int id);
        Task<bool> ConfirmAsync(int id);
        Task<IEnumerable<UserTradingPayment>> GetByUserIdAsync(int userId);
    }
}
