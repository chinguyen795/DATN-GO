using DATN_API.ViewModels.Vnpay;
using Microsoft.AspNetCore.Http;

namespace DATN_API.Interfaces
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(HttpContext context, VNPayPaymentRequestModel model);
        bool ValidateReturnUrl(IQueryCollection queryCollection, out string transactionStatus);
    }
}
