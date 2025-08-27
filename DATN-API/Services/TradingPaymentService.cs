using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Services
{
    public class TradingPaymentService : ITradingPaymentService
    {
        private readonly ApplicationDbContext _db;

        public TradingPaymentService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<TradingPayment>> GetAllAsync()
        {
            return await _db.TradingPayments
                .Include(tp => tp.Store) // nạp dữ liệu Store
                .ToListAsync();
        }

        public async Task<TradingPayment> GetByIdAsync(int id)
        {
            return await _db.TradingPayments
                            .Include(tp => tp.Store)
                            .FirstOrDefaultAsync(tp => tp.Id == id);
        }

        public async Task<TradingPayment> CreateAsync(TradingPayment payment)
        {
            _db.TradingPayments.Add(payment);
            await _db.SaveChangesAsync();
            return payment;
        }

        public async Task<TradingPayment> UpdateAsync(int id, TradingPayment payment)
        {
            var existing = await _db.TradingPayments.FindAsync(id);
            if (existing == null) return null;

            // update fields
            existing.StoreId = payment.StoreId;
            existing.Cost = payment.Cost;
            existing.Date = payment.Date;
            existing.Status = payment.Status;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.TradingPayments.FindAsync(id);
            if (existing == null) return false;

            _db.TradingPayments.Remove(existing);
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<bool> RejectAsync(int id)
        {
            var payment = await _db.TradingPayments.FindAsync(id);
            if (payment == null) return false;

            payment.Status = TradingPaymentStatus.TuChoi;
            _db.TradingPayments.Update(payment);
            await _db.SaveChangesAsync();

            return true;
        }

        // Đồng ý
        public async Task<bool> ConfirmAsync(int id)
        {
            var payment = await _db.TradingPayments.FindAsync(id);
            if (payment == null) return false;

            // cập nhật trạng thái của payment
            payment.Status = TradingPaymentStatus.DaXacNhan;
            _db.TradingPayments.Update(payment);

            // cập nhật lại số dư của store
            var store = await _db.Stores.FindAsync(payment.StoreId);
            if (store != null)
            {
                // lấy tổng số tiền các payment đang Chờ Xử Lý
                var pendingAmount = await _db.TradingPayments
                    .Where(p => p.StoreId == store.Id && p.Status == TradingPaymentStatus.ChoXuLy)
                    .SumAsync(p => (decimal?)p.Cost ?? 0);

                // tính lại số dư = tiền hiện tại - số tiền đang chờ xử lý
                store.MoneyAmout = (store.MoneyAmout ?? 0) - pendingAmount;

                if (store.MoneyAmout < 0)
                    store.MoneyAmout = 0; // tránh âm tiền

                _db.Stores.Update(store);
            }

            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<IEnumerable<TradingPayment>> GetByStoreIdAsync(int storeId)
        {
            return await _db.TradingPayments
                .Where(tp => tp.StoreId == storeId)
                .Include(tp => tp.Store)
                .ToListAsync();
        }
    }
}
