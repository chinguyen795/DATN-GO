using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class UserVouchersController : Controller
    {
        private readonly StoreService _storeService;
        public UserVouchersController( StoreService storeService)
        {
            _storeService = storeService;
        }
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.TryGetValue("Id", out var idBytes)
    && int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out var userId))
            {
                var store = await _storeService.GetStoreByUserIdAsync(userId);
                ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
            }
            return View();
        }
    }
}
