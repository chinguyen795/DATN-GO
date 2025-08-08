using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;

public class OrderUserController : Controller
{
    private readonly OrderService _orderService;
    public OrderUserController(OrderService orderService) => _orderService = orderService;

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

        ViewBag.Crimson = "#dc143c";
        return View(vm);
    }
}
