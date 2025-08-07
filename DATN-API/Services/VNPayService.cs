using DATN_API.Helpers;
using DATN_API.Interfaces;
using DATN_API.ViewModels.Vnpay;

namespace DATN_API.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly IConfiguration _config;

        public VNPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(HttpContext context, VNPayPaymentRequestModel model)
        {
            var tick = DateTime.Now.Ticks.ToString();
            var pay = new VNPayLibrary();
            pay.AddRequestData("vnp_Version", "2.1.0");
            pay.AddRequestData("vnp_Command", "pay");
            pay.AddRequestData("vnp_TmnCode", _config["VNPay:TmnCode"]);
            pay.AddRequestData("vnp_Amount", (model.Amount * 100).ToString());
            pay.AddRequestData("vnp_CurrCode", "VND");
            pay.AddRequestData("vnp_TxnRef", tick);
            pay.AddRequestData("vnp_OrderInfo", model.OrderDescription);
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_Locale", "vn");
            pay.AddRequestData("vnp_ReturnUrl", _config["VNPay:ReturnUrl"]);
            pay.AddRequestData("vnp_IpAddr", context.Connection.RemoteIpAddress?.ToString());
            pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

            var paymentUrl = pay.CreateRequestUrl(_config["VNPay:BaseUrl"], _config["VNPay:HashSecret"]);
            return paymentUrl;
        }


        public bool ValidateReturnUrl(IQueryCollection queryCollection, out string transactionStatus)
        {
            transactionStatus = string.Empty;

            var vnpay = new VNPayLibrary();

            foreach (var (key, value) in queryCollection)
            {
                vnpay.AddResponseData(key, value);
            }

            var isValid = vnpay.ValidateSignature(_config["VNPay:HashSecret"]);

            if (isValid)
            {
                transactionStatus = vnpay.GetResponseData("vnp_ResponseCode");
                return true;
            }

            return false;
        }

    }
}
