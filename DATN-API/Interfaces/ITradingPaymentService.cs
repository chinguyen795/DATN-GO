using DATN_API.Models;

namespace DATN_API.Interfaces
{
    public interface ITradingPaymentService
    {
        Task<List<TradingPayment>> GetAllAsync();
        Task<TradingPayment> GetByIdAsync(int id);
        Task<TradingPayment> CreateAsync(TradingPayment payment);
        Task<TradingPayment> UpdateAsync(int id, TradingPayment payment);
        Task<bool> DeleteAsync(int id);
        Task<bool> RejectAsync(int id);
        Task<bool> ConfirmAsync(int id);
        Task<IEnumerable<TradingPayment>> GetByStoreIdAsync(int storeId);
    }
}
