using DATN_API.Interfaces;
using DATN_API.Services;
using DATN_API.ViewModels.Vnpay;
using Microsoft.AspNetCore.Mvc;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IVNPayService _vnPayService;

        public PaymentController(IVNPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        [HttpPost("create-payment-url")]
        public IActionResult CreatePaymentUrl([FromBody] VNPayPaymentRequestModel model)
        {
            var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, model);
            return Ok(new { url = paymentUrl });
        }

        [HttpGet("vnpay-return")]
        public IActionResult VNPayReturn()
        {
            var isValid = _vnPayService.ValidateReturnUrl(Request.Query, out string transactionStatus);

            if (isValid && transactionStatus == "00")
            {
                return Ok("Thanh toán thành công!");
            }

            return BadRequest($"Thanh toán thất bại hoặc bị từ chối. Mã: {transactionStatus}");
        }

    }
}
