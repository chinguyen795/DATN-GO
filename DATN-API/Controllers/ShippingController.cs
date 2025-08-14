using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.ViewModels.GHTK;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShippingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGHTKService _ghtkService;
        private readonly IConfiguration _configuration;

        public ShippingController(
            ApplicationDbContext context,
            IGHTKService ghtkService,
            IConfiguration configuration)
        {
            _context = context;
            _ghtkService = ghtkService;
            _configuration = configuration;
        }

        /// <summary>
        /// Tính phí GHTK từ AddressId (người nhận) + StoreId (điểm lấy)
        /// </summary>
        [HttpPost("ghtk/calculate-fee")]
        public async Task<IActionResult> CalculateGHTKFee([FromBody] GHTKShippingFeeFromDbRequest request)
        {
            // Lấy địa chỉ người nhận
            var address = await _context.Addresses
                .Include(a => a.City)
                .FirstOrDefaultAsync(a => a.Id == request.AddressId);

            if (address == null || address.City == null)
                return NotFound("Không tìm thấy địa chỉ người nhận hoặc thành phố.");

            // Quận/phường: lấy bản ghi đầu tiên theo city/district (nếu bạn có cột cụ thể hãy map lại cho đúng)
            var district = await _context.Districts
                .FirstOrDefaultAsync(d => d.CityId == address.City.Id);
            if (district == null)
                return NotFound("Không tìm thấy quận cho địa chỉ này.");

            var ward = await _context.Wards
                .FirstOrDefaultAsync(w => w.DistrictId == district.Id);
            if (ward == null)
                return NotFound("Không tìm thấy phường cho địa chỉ này.");

            // Lấy điểm lấy (shop)
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == request.StoreId);
            if (store == null || string.IsNullOrWhiteSpace(store.Province) || string.IsNullOrWhiteSpace(store.District))
                return NotFound("Không tìm thấy thông tin địa chỉ cửa hàng.");

            // Gọi trực tiếp endpoint fee của GHTK (theo bạn đang làm)
            var apiUrl = $"{_configuration["GHTK:BaseUrl"]}/services/shipment/fee" +
                $"?address={Uri.EscapeDataString(address.Description ?? "")}" +
                $"&province={Uri.EscapeDataString(address.City.CityName)}" +
                $"&district={Uri.EscapeDataString(district.DistrictName)}" +
                $"&ward={Uri.EscapeDataString(ward.WardName)}" +
                $"&pick_province={Uri.EscapeDataString(store.Province)}" +
                $"&pick_district={Uri.EscapeDataString(store.District)}" +
                $"&weight={request.Weight}" +
                $"&value={request.Value}" +
                $"&deliver_option=none&tags[]=1";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Token", _configuration["GHTK:Token"]);

            var response = await httpClient.GetAsync(apiUrl);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, content);

            // Trả JSON đúng kiểu object
            try
            {
                var json = JObject.Parse(content);
                return Ok(json);
            }
            catch
            {
                return Ok(content);
            }
        }

        /// <summary>
        /// Đẩy đơn hàng (orderId) lên GHTK, lấy label và lưu vào Orders.LabelId
        /// </summary>
        [HttpPost("ghtk/push/{orderId:int}")]
        public async Task<IActionResult> PushOrderToGhtk(int orderId)
        {
            // Lấy đơn + chi tiết + user
            var o = await _context.Orders
                .Include(x => x.User)
                .Include(x => x.OrderDetails).ThenInclude(od => od.Product)
                .Include(x => x.ShippingMethod)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (o == null)
                return NotFound("Không tìm thấy đơn hàng.");

            // Lấy địa chỉ nhận (tùy app: nếu bạn có AddressId trên Orders, hãy dùng nó thay vì lấy theo UserId)
            var addr = await _context.Addresses
                .Where(a => a.UserId == o.UserId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            if (addr == null)
                return NotFound("Không tìm thấy địa chỉ người nhận.");

            // Build danh sách sản phẩm (kg; tối thiểu 0.1kg)
            var products = o.OrderDetails.Select(od => new GHTKProduct
            {
                Name = od.Product?.Name ?? "Sản phẩm",
                Weight = Math.Max(0.1m, ((decimal)(od.Product?.Weight ?? 0)) / 1000m),
                Quantity = od.Quantity
            }).ToList();

            // Thông tin lấy hàng (shop). Bạn có thể thay bằng thông tin từ Store của ShippingMethod nếu muốn map chính xác.
            var pickName = "Shop GO";
            var pickAddress = "123 Đường A, Quận 1";
            var pickProvince = "TP. Hồ Chí Minh";
            var pickDistrict = "Quận 1";
            var pickWard = "Phường A";
            var pickTel = "0900000000";

            // TODO: map đúng province/district/ward của người nhận nếu bạn có lưu chi tiết trong Address
            var payload = new GHTKCreateOrderRequest
            {
                Products = products,
                Order = new GHTKOrder
                {
                    Id = $"ORD-{o.Id}",

                    PickName = pickName,
                    PickAddress = pickAddress,
                    PickProvince = pickProvince,
                    PickDistrict = pickDistrict,
                    PickWard = pickWard,
                    PickTel = pickTel,

                    Name = o.User?.FullName ?? "Khách hàng",
                    Address = addr.Description ?? "Địa chỉ nhận",
                    Province = "TP. Hồ Chí Minh",
                    District = "Quận 1",
                    Ward = "Phường A",
                    Tel = o.User?.Phone ?? "0000000000",

                    Hamlet = "Khác",
                    DeliverOption = "none",
                    Transport = "road",

                    PickMoney = (o.PaymentMethod?.ToLower() == "cod") ? o.TotalPrice : 0,
                    Value = o.TotalPrice,
                    Note = ""
                }
            };

            // Gọi GHTK
            var label = await _ghtkService.CreateOrderAsync(payload);
            if (string.IsNullOrWhiteSpace(label))
                return BadRequest("Tạo đơn GHTK thất bại. Vui lòng kiểm tra lại địa chỉ (tỉnh/quận/phường), trọng lượng, token...");

            // Lưu LabelId vào Orders
            o.LabelId = label;
            await _context.SaveChangesAsync();

            return Ok(new { orderId = o.Id, label });
        }
        // ở đầu file đã có using DATN_API.ViewModels.GHTK;

        [HttpPost("ghtk/push-debug/{orderId:int}")]
        public async Task<IActionResult> PushOrderToGhtkDebug(int orderId)
        {
            // 1) Lấy đơn + user + chi tiết + shipping
            var o = await _context.Orders
                .Include(x => x.User)
                .Include(x => x.OrderDetails).ThenInclude(od => od.Product)
                .Include(x => x.ShippingMethod)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (o == null)
                return NotFound(new { error = "Không tìm thấy đơn hàng." });

            // 2) Lấy địa chỉ nhận (tạm thời lấy cái mới nhất của user)
            var addr = await _context.Addresses
                .Include(a => a.City)
                .Where(a => a.UserId == o.UserId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();

            if (addr == null)
                return NotFound(new { error = "Không tìm thấy địa chỉ người nhận." });

            // Suy ra district/ward tối thiểu (tránh ?. trong biểu thức LINQ)
            int cityId = addr.City != null ? addr.City.Id : 0;
            var district = await _context.Districts.FirstOrDefaultAsync(d => d.CityId == cityId);
            int districtId = district != null ? district.Id : 0;
            var ward = await _context.Wards.FirstOrDefaultAsync(w => w.DistrictId == districtId);

            var receiverProvince = addr.City?.CityName ?? "";
            var receiverDistrict = district != null ? district.DistrictName : "";
            var receiverWard = ward != null ? ward.WardName : "";

            // 3) Điểm lấy (store) theo ShippingMethod
            Stores? store = null;
            if (o.ShippingMethodId != 0)
            {
                store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == o.ShippingMethod!.StoreId);
            }
            if (store == null)
            {
                // fallback an toàn — về sau nên cấu hình Store chuẩn
                store = new Stores
                {
                    Name = "Shop GO",
                    RepresentativeName = "Shop GO",
                    Phone = "0900000000",
                    Address = "Kho tổng",
                    PickupAddress = "Kho tổng",
                    Province = "TP. Hồ Chí Minh",
                    District = "Quận 1",
                    Ward = "Phường Bến Nghé"
                };
            }

            // 4) Build products (kg tối thiểu 0.1)
            var products = o.OrderDetails.Select(od => new GHTKProduct
            {
                Name = od.Product?.Name ?? "Sản phẩm",
                Weight = Math.Max(0.1m, ((decimal)(od.Product?.Weight ?? 0)) / 1000m),
                Quantity = od.Quantity
            }).ToList();

            // 5) Payload GHTK (VNPay => PickMoney = 0)
            var payload = new GHTKCreateOrderRequest
            {
                Products = products,
                Order = new GHTKOrder
                {
                    Id = $"ORD-{o.Id}",

                    PickName = store.RepresentativeName ?? store.Name ?? "Shop",
                    PickAddress = store.PickupAddress ?? store.Address ?? "",
                    PickProvince = store.Province ?? "",
                    PickDistrict = store.District ?? "",
                    PickWard = store.Ward ?? "",
                    PickTel = store.Phone ?? "0000000000",

                    Name = o.User?.FullName ?? "Khách hàng",
                    Address = addr.Description ?? "Địa chỉ nhận",
                    Province = receiverProvince,
                    District = receiverDistrict,
                    Ward = receiverWard,
                    Tel = o.User?.Phone ?? "0000000000",

                    Hamlet = "Khác",
                    DeliverOption = "none",
                    Transport = "road",

                    PickMoney = 0,
                    Value = o.TotalPrice,
                    Note = ""
                }
            };

            // 6) Gọi GHTK (debug)
            var (ok, label, raw) = await _ghtkService.CreateOrderDebugAsync(payload);

            // 7) Nếu tạo OK, lưu LabelId để lần sau không tạo trùng
            if (ok && !string.IsNullOrWhiteSpace(label))
            {
                o.LabelId = label;
                await _context.SaveChangesAsync();
            }

            // 8) Trả payload + phản hồi raw để bạn nhìn thấy lý do
            return Ok(new
            {
                success = ok,
                label,
                receiver = new { receiverProvince, receiverDistrict, receiverWard },
                pick = new { store.Province, store.District, store.Ward, store.PickupAddress },
                payload,
                ghtkRaw = raw
            });
        }

    }
}