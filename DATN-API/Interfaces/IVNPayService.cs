using DATN_API.ViewModels.Vnpay;
using Microsoft.AspNetCore.Http;

namespace DATN_API.Interfaces
{
    // Interfaces/IVNPayService.cs
    public interface IVNPayService
    {
        string CreatePaymentUrl(VnpCreatePaymentRequest req);
        bool ValidateReturn(IDictionary<string, string> query);
    }

    public class VnpCreatePaymentRequest
    {
        public string OrderId { get; set; } = default!; // your internal order code
        public long Amount { get; set; }               // VND, not multiplied
        public string OrderInfo { get; set; } = default!;
        public string IpAddress { get; set; } = "127.0.0.1";
        public string? BankCode { get; set; }
        public string Locale { get; set; } = "vn";
    }

}
