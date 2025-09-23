using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;

public class OrderUserController : Controller
{
    private readonly OrderService _orderService;
    private readonly StoreService _storeService;

    public OrderUserController(OrderService orders, StoreService storeService)
    {
        _orderService = orders;
        _storeService = storeService;
    }


    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var vm = await _orderService.GetDetailAsync(id);
        if (vm == null)
        {
            TempData["ToastMessage"] = "Không tìm thấy đơn hàng.";
            TempData["ToastType"] = "danger";
            return RedirectToAction("Index", "Home");
        }
        if (HttpContext.Session.TryGetValue("Id", out var idBytes)
    && int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out var userId))
        {
            var store = await _storeService.GetStoreByUserIdAsync(userId);
            ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
        }
        ViewBag.Crimson = "#dc143c";
        return View(vm);
    }
}
