using DATN_GO.Models;
using DATN_GO.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class HomeController : Controller
    {
        private readonly DinerService _service;

        public HomeController(DinerService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index([FromForm] DinerCOUModel diner)
        {
            var model = new DinnerModel
            {
                DinerName = diner.DinerName,
                DinerAddress = diner.DinerAddress,
                Longitude = diner.Longitude,
                Latitude = diner.Latitude,
                PhoneNumber = diner.PhoneNumber,
                Email = diner.Email,
            };
            var openTime = diner.OpenTime.Split(':');
            model.OpenHouse = Int32.Parse(openTime[0]);
            model.OpenMinute = Int32.Parse(openTime[1]);
            var closeTime = diner.CloseTime.Split(':');
            model.CloseHouse = Int32.Parse(closeTime[0]);
            model.CloseMinute = Int32.Parse(closeTime[1]);
            if (diner.Id.HasValue)
                await _service.Update(diner.Id.Value, model);
            else
                await _service.Create(model);
            return RedirectToAction("Index");
        }

        [HttpPut("/Seller/Diners/Image")]
        public async Task<IActionResult> UpdateImage([FromBody] ChangeImageModel model)
        {
            await _service.UpdateImage(model);
            return NoContent();
        }
    }

    public class DinerCOUModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Tên quán là bắt buộc.")]
        [MaxLength(50)]
        public string DinerName { get; set; }

        [Required(ErrorMessage = "Địa chỉ là bắt buộc.")]
        [MaxLength(50)]
        public string DinerAddress { get; set; }

        [Required(ErrorMessage = "Kinh độ là bắt buộc.")]
        public float Longitude { get; set; }

        [Required(ErrorMessage = "Vĩ độ là bắt buộc.")]
        public float Latitude { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Giờ mở cửa là bắt buộc.")]
        [RegularExpression(@"^\d{1,2}:\d{2}$", ErrorMessage = "Định dạng giờ không hợp lệ (HH:mm).")]
        public string OpenTime { get; set; } = "08:00";

        [Required(ErrorMessage = "Giờ đóng cửa là bắt buộc.")]
        [RegularExpression(@"^\d{1,2}:\d{2}$", ErrorMessage = "Định dạng giờ không hợp lệ (HH:mm).")]
        public string CloseTime { get; set; } = "20:00";

        [Required(ErrorMessage = "Họ tên là bắt buộc.")]
        public string Fullname { get; set; }

        [Required(ErrorMessage = "CCCD là bắt buộc.")]
        [MaxLength(20)]
        public string CitizenIdentityCard { get; set; }
    }


    public class ChangeImageModel
    {
        public bool IsAvatar { get; set; }
        public string Data { get; set; }
    }
}
