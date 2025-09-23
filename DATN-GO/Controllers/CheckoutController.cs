using DATN_GO.Service;
using DATN_GO.Services;
using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AddressService _service;
        private readonly StoreService _storeService;
        private readonly HttpClient _httpClient;

        public CheckoutController(AddressService service, IHttpClientFactory factory, StoreService storeService)
        {
            _service = service;
            _storeService = storeService;
            _httpClient = factory.CreateClient("api");
        }
        [HttpGet]
        public IActionResult Success(int orderId)
        {
            if (HttpContext.Session.TryGetValue("Id", out var idBytes)
    && int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out var userId))
            {
                var store = _storeService.GetStoreByUserIdAsync(userId);
                ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
            }
            ViewBag.OrderId = orderId;     // pass qua view
            return View();
        }

        [HttpGet]
        public IActionResult Failure(int? orderId, string? message)
        {
            if (HttpContext.Session.TryGetValue("Id", out var idBytes)
    && int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out var userId))
            {
                var store =  _storeService.GetStoreByUserIdAsync(userId);
                ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
            }
            ViewBag.OrderId = orderId;
            ViewBag.Message = message;
            return View();
        }
    }
}
