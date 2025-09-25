using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Services
{
    public class TradingPaymentService : ITradingPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public TradingPaymentService(ApplicationDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
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
            // 1) Lưu đơn mới vào DB
            _db.TradingPayments.Add(payment);
            await _db.SaveChangesAsync();

            // 2) Lấy thông tin store (giả sử TradingPayment có StoreId)
            var store = await _db.Stores
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == payment.StoreId);

            // 3) Lấy user nhận mail (ví dụ tất cả user có Id = 3)
            var recipients = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == 3)
                .ToListAsync();

            if (store != null && recipients.Any())
            {
                var subject = "🏬 Yêu cầu rút tiền mới từ Store";
                var costFormatted = (payment.Cost).ToString("N0");

                var body = $@"
<div style=""font-family:Arial,sans-serif;line-height:1.6"">
  <h2>💸 Đơn yêu cầu rút tiền từ Store</h2>
  <p><b>🏬 Tên cửa hàng:</b> {store.Name}</p>
  <p><b>📱 Số điện thoại:</b> {store.Phone}</p>
  <p><b>🏦 Ngân hàng:</b> {store.Bank}</p>
  <p><b>💳 Số tài khoản:</b> {store.BankAccount}</p>
  {(string.IsNullOrWhiteSpace(store.BankAccountOwner) ? "" : $"<p><b>🪪 Chủ tài khoản:</b> {store.BankAccountOwner}</p>")}
  <p><b>💰 Số tiền yêu cầu:</b> {costFormatted} VND</p>
  <hr/>
  <p>✅ Vui lòng xử lý giao dịch này sớm nhất có thể.</p>
</div>";

                foreach (var rcpt in recipients)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(rcpt.Email))
                            await _emailService.SendEmailAsync(rcpt.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        // TODO: log lỗi
                    }
                }
            }

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
