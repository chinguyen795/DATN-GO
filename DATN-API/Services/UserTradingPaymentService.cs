using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Services
{
    public class UserTradingPaymentService : IUserTradingPaymentService
    {
        private readonly ApplicationDbContext _db;

        public UserTradingPaymentService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<UserTradingPayment>> GetAllAsync()
        {
            return await _db.UserTradingPayments
                .Include(tp => tp.User)
                .ToListAsync();
        }

        public async Task<UserTradingPayment?> GetByIdAsync(int id)
        {
            return await _db.UserTradingPayments
                            .Include(tp => tp.User)
                            .FirstOrDefaultAsync(tp => tp.Id == id);
        }

        public async Task<UserTradingPayment> CreateAsync(UserTradingPayment payment)
        {
            _db.UserTradingPayments.Add(payment);
            await _db.SaveChangesAsync();
            return payment;
        }

        public async Task<UserTradingPayment?> UpdateAsync(int id, UserTradingPayment payment)
        {
            var existing = await _db.UserTradingPayments.FindAsync(id);
            if (existing == null) return null;

            existing.UserId = payment.UserId;
            existing.Cost = payment.Cost;
            existing.Date = payment.Date;
            existing.Status = payment.Status;
            existing.Bank = payment.Bank;
            existing.BankAccount = payment.BankAccount;
            existing.BankAccountOwner = payment.BankAccountOwner;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.UserTradingPayments.FindAsync(id);
            if (existing == null) return false;

            _db.UserTradingPayments.Remove(existing);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectAsync(int id)
        {
            var payment = await _db.UserTradingPayments.FindAsync(id);
            if (payment == null) return false;

            payment.Status = TradingPaymentStatus.TuChoi;
            _db.UserTradingPayments.Update(payment);
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ConfirmAsync(int id)
        {
            var payment = await _db.UserTradingPayments.FindAsync(id);
            if (payment == null) return false;

            payment.Status = TradingPaymentStatus.DaXacNhan;
            _db.UserTradingPayments.Update(payment);

            // Nếu cần cập nhật số dư User thì làm thêm tại đây
            var user = await _db.Users.FindAsync(payment.UserId);
            if (user != null)
            {
                // Ví dụ: user có Balance
                var pendingAmount = await _db.UserTradingPayments
                    .Where(p => p.UserId == user.Id && p.Status == TradingPaymentStatus.ChoXuLy)
                    .SumAsync(p => (decimal?)p.Cost ?? 0);

                // giả sử user có Balance
                user.Balance = (user.Balance ?? 0) - pendingAmount;
                if (user.Balance < 0) user.Balance = 0;

                _db.Users.Update(user);
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserTradingPayment>> GetByUserIdAsync(int userId)
        {
            return await _db.UserTradingPayments
                .Where(tp => tp.UserId == userId)
                .Include(tp => tp.User)
                .ToListAsync();
        }
    }
}
