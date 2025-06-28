/*using DATN_GO.Areas.Seller.Controllers;
using DATN_GO.Models;
using DATN_GO.Services;
using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class SalesRegistrationController : Controller
    {
        private readonly DinerService _service;

        public SalesRegistrationController(DinerService service)
        {
            _service = service;
        }

        public IActionResult SalesRegistration()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SalesRegistration([FromForm] DinerCOUModel diner)
        {
            var model = new DinnerModel
            {
                DinerName = diner.DinerName,
                DinerAddress = diner.DinerAddress,
                Longitude = diner.Longitude,
                Latitude = diner.Latitude
            };
            await _service.Create(model);
            return RedirectToAction("Index", "Home", new { Area = "Seller" });
        }
    }
}
*/