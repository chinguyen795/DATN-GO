using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Services
{
    public class UserTradingPaymentService : IUserTradingPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public UserTradingPaymentService(ApplicationDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
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

        public async Task<UserTradingPayment?> CreateAsync(UserTradingPayment payment)
        {
            // 1) Tạo đơn
            _db.UserTradingPayments.Add(payment);
            await _db.SaveChangesAsync();

            // 2) Load user tạo yêu cầu để lấy tên/email show vào mail
            var requester = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == payment.UserId);

            // 3) Lấy tất cả user có Id = 3 để gửi mail
            var recipients = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == 3)
                .ToListAsync();

            if (requester != null && recipients.Any())
            {
                var subject = "💸 Yêu cầu rút tiền mới";
                var costFormatted = (payment.Cost ?? 0).ToString("N0"); // ví dụ 1,000,000

                var body = $@"
<div style=""font-family:Arial,sans-serif;line-height:1.6"">
  <h2>💸 Đơn yêu cầu rút tiền</h2>
  <p><b>👤 Người yêu cầu:</b> {requester.FullName}</p>
  <p><b>📧 Email:</b> {requester.Email}</p>
  <p><b>🏦 Ngân hàng:</b> {payment.Bank}</p>
  <p><b>💳 Số tài khoản:</b> {payment.BankAccount}</p>
  {(string.IsNullOrWhiteSpace(payment.BankAccountOwner) ? "" : $"<p><b>🪪 Chủ tài khoản:</b> {payment.BankAccountOwner}</p>")}
  <p><b>💰 Số tiền yêu cầu:</b> {costFormatted} VND</p>
  <hr/>
  <p>✅ Vui lòng xử lý giao dịch này sớm nhất có thể.</p>
</div>";

                // 4) Gửi mail cho từng recipient; không chặn luồng tạo đơn nếu mail lỗi
                foreach (var rcpt in recipients)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(rcpt.Email))
                            await _emailService.SendEmailAsync(rcpt.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        // TODO: log lỗi (Serilog/NLog/ILogger...)
                        // Không throw để không ảnh hưởng đến việc tạo đơn
                    }
                }
            }

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
