using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.ViewModels.Cart;
using DATN_API.ViewModels.GHTK;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;

namespace DATN_API.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CartService> _logger;

        public CartService(ApplicationDbContext context, IConfiguration configuration, ILogger<CartService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CartSummaryViewModel> GetCartByUserIdAsync(int userId)
        {
            var carts = await _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ThenInclude(p => p.Store)
                .ToListAsync();

            var result = new List<CartItemViewModel>();

            foreach (var cart in carts)
            {
                var variants = await _context.Set<CartItemVariants>()
                    .Where(cv => cv.CartId == cart.Id)
                    .Include(cv => cv.VariantValue)
                        .ThenInclude(vv => vv.Variant)
                    .ToListAsync();

                var variantValueIds = variants.Select(v => v.VariantValueId).OrderBy(id => id).ToList();
                decimal price = 0;
                int maxQuantity = 0;
                decimal weight = 0;

                if (variantValueIds.Any())
                {
                    var productVariants = await _context.ProductVariants
                        .Where(pv => pv.ProductId == cart.ProductId)
                        .ToListAsync();

                    foreach (var pv in productVariants)
                    {
                        var compositionIds = await _context.VariantCompositions
                            .Where(vc => vc.ProductVariantId == pv.Id)
                            .Select(vc => vc.VariantValueId)
                            .Where(id => id.HasValue)
                            .Select(id => id.Value)
                            .OrderBy(id => id)
                            .ToListAsync();

                        if (compositionIds.SequenceEqual(variantValueIds))
                        {
                            price = pv.Price;
                            maxQuantity = pv.Quantity;
                            weight = (decimal)pv.Weight; break;
                        }
                    }
                }
                else
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == cart.ProductId);
                    price = await _context.Prices
                        .Where(p => p.ProductId == cart.ProductId)
                        .Select(p => (decimal?)p.Price)
                        .FirstOrDefaultAsync() ?? 0;
                    maxQuantity = product?.Quantity ?? 0;
                    weight = (decimal)(product?.Weight ?? 500);
                }

                var variantTexts = variants
                    .Select(v => $"{v.VariantValue?.Variant?.VariantName}: {v.VariantValue?.ValueName}")
                    .ToList();

                result.Add(new CartItemViewModel
                {
                    CartId = cart.Id,
                    ProductId = cart.ProductId,
                    ProductName = cart.Product?.Name ?? "Sản phẩm không tồn tại",
                    Image = cart.Product?.MainImage ?? "",
                    Quantity = cart.Quantity,
                    Price = price,
                    MaxQuantity = maxQuantity,
                    Variants = variantTexts,
                    IsSelected = cart.IsSelected,
                    TotalWeight = weight * cart.Quantity,
                    TotalValue = price * cart.Quantity,
                    StoreId = cart.Product?.StoreId ?? 0,
                    StoreName = cart.Product?.Store?.Name ?? "Cửa hàng không tồn tại",
                    StoreAvatar = cart.Product?.Store?.Avatar ?? ""
                });
            }

            var addresses = await _context.Addresses
                .Include(a => a.City)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.UpdateAt)
                .ToListAsync();

            var addressViewModels = new List<AddressViewModel>();
            foreach (var address in addresses)
            {
                var district = await _context.Districts.FirstOrDefaultAsync(d => d.CityId == address.City.Id);
                var ward = district != null
                    ? await _context.Wards.FirstOrDefaultAsync(w => w.DistrictId == district.Id)
                    : null;

                var full = string.Join(", ", new[]
                {
            $"{address.Name} - {address.Phone}",
            address.Description,
            ward?.WardName,
            district?.DistrictName,
            address.City?.CityName
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

                addressViewModels.Add(new AddressViewModel
                {
                    Id = address.Id,
                    FullAddress = full
                });
            }

            var now = DateTime.UtcNow;
            var selectedCartItems = result.Where(c => c.IsSelected).ToList();
            var selectedProductIds = selectedCartItems.Select(c => c.ProductId).ToList();

            var selectedProducts = await _context.Products
                .Where(p => selectedProductIds.Contains(p.Id))
                .Select(p => new { p.Id, p.CategoryId, p.StoreId })
                .ToListAsync();

            var selectedCategoryIds = selectedProducts.Select(p => p.CategoryId).Distinct().ToList();
            var selectedStoreIds = selectedProducts.Select(p => p.StoreId).Distinct().ToList();

            var productAmountDict = selectedCartItems
                .Join(selectedProducts,
                      cart => cart.ProductId,
                      prod => prod.Id,
                      (cart, prod) => new
                      {
                          cart.ProductId,
                          cart.Quantity,
                          prod.CategoryId,
                          prod.StoreId,
                          Amount = cart.Quantity * cart.Price
                      }).ToList();

            var userVouchers = await _context.UserVouchers
                .Where(uv =>
                    uv.UserId == userId &&
                    !uv.IsUsed &&
                    uv.Voucher.EndDate.Date >= now.Date &&
                    (
                        (uv.Voucher.StoreId == null || selectedStoreIds.Contains(uv.Voucher.StoreId.Value)) &&
                        (uv.Voucher.CategoryId == null || selectedCategoryIds.Contains(uv.Voucher.CategoryId.Value))
                    ))
                .Include(uv => uv.Voucher)
                .ThenInclude(v => v.Store)
                .ToListAsync();

            var validVouchers = userVouchers
                .Where(uv =>
                {
                    var total = productAmountDict
                        .Where(p =>
                            (uv.Voucher.CategoryId == null || p.CategoryId == uv.Voucher.CategoryId) &&
                            (uv.Voucher.StoreId == null || p.StoreId == uv.Voucher.StoreId))
                        .Sum(p => p.Amount);

                    return total >= uv.Voucher.MinOrder;
                })
                .Select(uv => new UserVoucherViewModel
                {
                    Id = uv.Id,
                    VoucherId = uv.VoucherId,
                    Reduce = uv.Voucher.Reduce,
                    MinOrder = uv.Voucher.MinOrder,
                    EndDate = uv.Voucher.EndDate,
                    StoreName = uv.Voucher.Store?.Name ?? "Sàn TMĐT"
                })

      .GroupBy(v => v.VoucherId)
      .Select(g => g.First())
      .ToList();

            var totalWeight = result.Where(c => c.IsSelected).Sum(c => c.TotalWeight);
            var totalValue = result.Where(c => c.IsSelected).Sum(c => c.TotalValue);

            // Nhóm sản phẩm đã chọn theo cửa hàng
            var selectedItems = result.Where(c => c.IsSelected).ToList();

            var selectedWithStore = await _context.Products
                .Where(p => selectedProductIds.Contains(p.Id))
                .Select(p => new { p.Id, p.StoreId })
                .ToListAsync();

            var groupedByStore = selectedItems
                .Join(selectedWithStore,
                      cart => cart.ProductId,
                      prod => prod.Id,
                      (cart, prod) => new
                      {
                          StoreId = prod.StoreId,
                          cart.ProductId,
                          cart.Quantity,
                          cart.Price,
                          cart.TotalWeight
                      })
                .GroupBy(x => x.StoreId)
                .Select(g => new StoreCartGroup
                {
                    StoreId = g.Key,
                    Products = g.Select(p => new StoreCartItem
                    {
                        ProductId = p.ProductId,
                        Quantity = p.Quantity,
                        Price = p.Price,
                        TotalWeight = p.TotalWeight
                    }).ToList()
                }).ToList();
            if (result.Any())
            {
                result = result
                    .OrderByDescending(x => x.CartId)
                    .ThenBy(x => x.StoreName)
                    .ToList();
            }
            return new CartSummaryViewModel
            {
                CartItems = result,
                Addresses = addressViewModels,
                Vouchers = validVouchers,
                TotalWeight = totalWeight,
                TotalValue = totalValue
            };


        }


        public async Task<List<ShippingGroupViewModel>> GetShippingGroupsByUserIdAsync(int userId, int addressId)
        {
            var address = await _context.Addresses
                .Include(a => a.City)
                .FirstOrDefaultAsync(a => a.Id == addressId);

            if (address == null || address.City == null)
                throw new Exception("Không tìm thấy địa chỉ người nhận.");

            var district = await _context.Districts.FirstOrDefaultAsync(d => d.CityId == address.City.Id);
            if (district == null)
                throw new Exception("Không tìm thấy quận cho địa chỉ.");

            var ward = await _context.Wards.FirstOrDefaultAsync(w => w.DistrictId == district.Id);
            if (ward == null)
                throw new Exception("Không tìm thấy phường cho địa chỉ.");

            var carts = await _context.Carts
                .Where(c => c.UserId == userId && c.IsSelected)
                .Include(c => c.Product)
                .ToListAsync();

            var result = new List<ShippingGroupViewModel>();

            foreach (var group in carts.GroupBy(c => c.Product.StoreId))
            {
                decimal totalWeightKg = 0; // Tổng trọng lượng theo kg
                decimal totalValue = 0;
                var productIdList = new List<int>();

                var storeId = group.Key;
                var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId);

                if (store == null || string.IsNullOrEmpty(store.Province) || string.IsNullOrEmpty(store.District))
                    throw new Exception($"Không tìm thấy địa chỉ của cửa hàng {storeId}");

                foreach (var item in group)
                {
                    var quantity = item.Quantity;

                    // Lấy trọng lượng từ variant trước, nếu không có thì lấy từ product
                    var variantValueIds = await _context.Set<CartItemVariants>()
                        .Where(cv => cv.CartId == item.Id)
                        .Select(v => v.VariantValueId)
                        .OrderBy(id => id)
                        .ToListAsync();

                    decimal itemWeightKg = 0; // Trọng lượng từng sp theo kg
                    decimal price = 0;

                    if (variantValueIds.Any())
                    {
                        var productVariants = await _context.ProductVariants
                            .Where(pv => pv.ProductId == item.ProductId)
                            .ToListAsync();

                        foreach (var pv in productVariants)
                        {
                            var compositionIds = await _context.VariantCompositions
                                .Where(vc => vc.ProductVariantId == pv.Id)
                                .Select(vc => vc.VariantValueId)
                                .Where(id => id.HasValue)
                                .Select(id => id.Value)
                                .OrderBy(id => id)
                                .ToListAsync();

                            if (compositionIds.SequenceEqual(variantValueIds))
                            {
                                price = pv.Price;
                                itemWeightKg = pv.Weight; // DB đã lưu kg
                                break;
                            }
                        }

                        if (itemWeightKg == 0)
                        {
                            itemWeightKg = item.Product?.Weight ?? 0.5m; // fallback 0.5kg
                            price = await _context.Prices
                                .Where(p => p.ProductId == item.ProductId)
                                .Select(p => (decimal?)p.Price)
                                .FirstOrDefaultAsync() ?? 0;
                        }
                    }
                    else
                    {
                        itemWeightKg = item.Product?.Weight ?? 0.5m;
                        price = await _context.Prices
                            .Where(p => p.ProductId == item.ProductId)
                            .Select(p => (decimal?)p.Price)
                            .FirstOrDefaultAsync() ?? 0;
                    }

                    totalWeightKg += itemWeightKg * quantity;
                    totalValue += price * quantity;
                    productIdList.Add(item.ProductId);
                }

                // Convert sang gram để gọi GHTK
                int totalWeightInGram = (int)Math.Round(totalWeightKg * 1000);

                if (totalWeightInGram < 100)
                    totalWeightInGram = 100; // GHTK yêu cầu min 100g

                _logger.LogInformation($"Store {storeId}: {totalWeightInGram}g = {totalWeightKg:F2}kg");

                // Tạo URL gọi GHTK
                var feeUrl = $"{_configuration["GHTK:BaseUrlProduction"]}/services/shipment/fee" +
                    $"?address={Uri.EscapeDataString(address.Description ?? "")}" +
                    $"&province={Uri.EscapeDataString(address.City.CityName)}" +
                    $"&district={Uri.EscapeDataString(district.DistrictName)}" +
                    $"&ward={Uri.EscapeDataString(ward.WardName)}" +
                    $"&pick_province={Uri.EscapeDataString(store.Province)}" +
                    $"&pick_district={Uri.EscapeDataString(store.District)}" +
                    $"&weight={totalWeightInGram}" + // gửi gram
                    $"&value={totalValue}" +
                    $"&deliver_option=none&tags[]=1";

                _logger.LogInformation($"GHTK URL: {feeUrl}");

                int fee = 0;

                try
                {
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("Token", _configuration["GHTK:TokenProduction"]);

                        var response = await client.GetAsync(feeUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            _logger.LogInformation($"GHTK Response: {content}");

                            var json = JsonDocument.Parse(content);

                            if (json.RootElement.TryGetProperty("success", out var successElement) &&
                                successElement.GetBoolean())
                            {
                                if (json.RootElement.TryGetProperty("fee", out var feeElement) &&
                                    feeElement.TryGetProperty("ship_fee_only", out var shipFeeOnly))
                                {
                                    fee = shipFeeOnly.GetInt32();
                                }
                            }
                            else
                            {
                                if (json.RootElement.TryGetProperty("message", out var messageElement))
                                {
                                    throw new Exception($"GHTK Error: {messageElement.GetString()}");
                                }
                                else
                                {
                                    throw new Exception("GHTK API trả về success=false");
                                }
                            }
                        }
                        else
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            _logger.LogError($"GHTK HTTP Error: {content}");
                            throw new Exception($"Lỗi gọi GHTK: {response.StatusCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi GHTK cho store {storeId}");
                    fee = 15000 + (int)((totalWeightInGram > 500 ? (totalWeightInGram - 500) / 500 * 5000 : 0));
                }

                result.Add(new ShippingGroupViewModel
                {
                    StoreId = storeId,
                    TotalWeight = totalWeightKg, // trả về kg cho FE
                    TotalValue = totalValue,
                    ProductIds = productIdList.Distinct().ToList(),
                    ShippingFee = fee
                });
            }

            return result;
        }

        public async Task<string> CreateGHTKOrderAsync(int userId, int addressId)
        {
            var address = await _context.Addresses
                .Include(a => a.City)
                .FirstOrDefaultAsync(a => a.Id == addressId);

            if (address == null || address.City == null)
                throw new Exception("Không tìm thấy địa chỉ người nhận.");

            var district = await _context.Districts.FirstOrDefaultAsync(d => d.CityId == address.City.Id);
            if (district == null)
                throw new Exception("Không tìm thấy quận cho địa chỉ.");

            var ward = await _context.Wards.FirstOrDefaultAsync(w => w.DistrictId == district.Id);
            if (ward == null)
                throw new Exception("Không tìm thấy phường cho địa chỉ.");

            // Lấy giỏ hàng
            var carts = await _context.Carts
                .Where(c => c.UserId == userId && c.IsSelected)
                .Include(c => c.Product)
                .ToListAsync();

            if (!carts.Any())
                throw new Exception("Giỏ hàng trống.");

            var storeId = carts.First().Product.StoreId;
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
            if (store == null)
                throw new Exception("Không tìm thấy cửa hàng.");

            if (string.IsNullOrWhiteSpace(store.Phone))
                throw new Exception("Số điện thoại cửa hàng trống.");

            if (store.Phone == address.Phone)
                throw new Exception("Số điện thoại cửa hàng trùng với số người nhận.");

            if (string.IsNullOrWhiteSpace(store.Name) ||
                string.IsNullOrWhiteSpace(store.PickupAddress) ||
                string.IsNullOrWhiteSpace(store.Province) ||
                string.IsNullOrWhiteSpace(store.District) ||
                string.IsNullOrWhiteSpace(store.Ward))
            {
                throw new Exception("Thiếu thông tin địa chỉ cửa hàng.");
            }

            if (string.IsNullOrWhiteSpace(address.Name) ||
                string.IsNullOrWhiteSpace(address.Description) ||
                string.IsNullOrWhiteSpace(address.City.CityName) ||
                string.IsNullOrWhiteSpace(district.DistrictName) ||
                string.IsNullOrWhiteSpace(ward.WardName) ||
                string.IsNullOrWhiteSpace(address.Phone))
            {
                throw new Exception("Thiếu thông tin địa chỉ người nhận.");
            }

            var totalValue = carts.Sum(c =>
                (_context.Prices.Where(p => p.ProductId == c.ProductId)
                    .Select(p => (decimal?)p.Price)
                    .FirstOrDefault() ?? 0) * c.Quantity
            );

            if (totalValue <= 0)
                throw new Exception("Giá trị đơn hàng không hợp lệ.");

            // FIX: Tạo product list với xử lý variants và weight chính xác
            var productList = new List<GHTKProduct>();

            foreach (var cartItem in carts)
            {
                // Lấy thông tin variant nếu có
                var variantValueIds = await _context.Set<CartItemVariants>()
                    .Where(cv => cv.CartId == cartItem.Id)
                    .Select(v => v.VariantValueId)
                    .OrderBy(id => id)
                    .ToListAsync();

                decimal itemWeight = 0; // Thay đổi từ int sang decimal

                if (variantValueIds.Any())
                {
                    // Tìm ProductVariant phù hợp
                    var productVariants = await _context.ProductVariants
                        .Where(pv => pv.ProductId == cartItem.ProductId)
                        .ToListAsync();

                    foreach (var pv in productVariants)
                    {
                        var compositionIds = await _context.VariantCompositions
                            .Where(vc => vc.ProductVariantId == pv.Id)
                            .Select(vc => vc.VariantValueId)
                            .Where(id => id.HasValue)
                            .Select(id => id.Value)
                            .OrderBy(id => id)
                            .ToListAsync();

                        if (compositionIds.SequenceEqual(variantValueIds))
                        {
                            itemWeight = (decimal)pv.Weight; // Cast sang decimal
                            break;
                        }
                    }

                    // Fallback nếu không tìm thấy variant
                    if (itemWeight == 0)
                    {
                        itemWeight = (decimal)(cartItem.Product?.Weight ?? 500);
                    }
                }
                else
                {
                    // Sản phẩm không có variant
                    itemWeight = (decimal)(cartItem.Product?.Weight ?? 500);
                }

                if (itemWeight <= 0)
                    throw new Exception($"Sản phẩm '{cartItem.Product.Name}' chưa có trọng lượng hợp lệ.");

                decimal totalItemWeightKg = itemWeight * cartItem.Quantity;

                // Đổi kg → gram trước khi gửi
                int totalItemWeightGram = (int)Math.Round(totalItemWeightKg * 1000);

                productList.Add(new GHTKProduct
                {
                    Name = cartItem.Product.Name,
                    Weight = totalItemWeightKg, // ✅ gửi đúng kg
                    Quantity = cartItem.Quantity,
                    ProductCode = cartItem.Product.Id.ToString()
                });

            }

            // Log tổng trọng lượng để debug
            var totalWeight = productList.Sum(p => p.Weight);
            _logger.LogInformation($"Total order weight: {totalWeight}");

            var ghtkRequest = new GHTKCreateOrderRequest
            {
                Products = productList,
                Order = new GHTKOrder
                {
                    Id = Guid.NewGuid().ToString(),

                    PickName = store.Name,
                    PickAddress = store.PickupAddress,
                    PickProvince = store.Province,
                    PickDistrict = store.District,
                    PickWard = store.Ward,
                    PickTel = store.Phone,

                    Name = address.Name,
                    Address = address.Description,
                    Province = address.City.CityName,
                    District = district.DistrictName,
                    Ward = ward.WardName,
                    Tel = address.Phone,

                    PickMoney = totalValue,
                    Note = "Giao hàng COD",
                    Value = totalValue,
                    Transport = "road",
                    DeliverOption = "none"
                }
            };

            // Gửi request đến GHTK
            var baseUrl = _configuration["GHTK:BaseUrl"];
            var token = _configuration["GHTK:Token"];

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token))
                throw new Exception("Thiếu cấu hình GHTK.");

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("Token", token);

            var payload = JsonConvert.SerializeObject(ghtkRequest, Formatting.Indented);

            _logger.LogInformation("GHTK Create Order Payload: {Json}", payload);
            Console.WriteLine(payload);

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            try
            {
                var response = await http.PostAsync($"{baseUrl}/services/shipment/order", content);
                var result = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("GHTK Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("GHTK Response: {Result}", result);
                Console.WriteLine(result);

                // Kiểm tra response để đảm bảo đơn hàng được tạo thành công
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"GHTK API Error: {response.StatusCode} - {result}");
                }

                // Parse response để kiểm tra success
                try
                {
                    var jsonResponse = JsonDocument.Parse(result);
                    if (jsonResponse.RootElement.TryGetProperty("success", out var successElement) &&
                        !successElement.GetBoolean())
                    {
                        var errorMessage = "Tạo đơn GHTK thất bại";
                        if (jsonResponse.RootElement.TryGetProperty("message", out var messageElement))
                        {
                            errorMessage = messageElement.GetString() ?? errorMessage;
                        }
                        throw new Exception($"GHTK Error: {errorMessage}");
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogWarning("Không thể parse GHTK response JSON: {Exception}", ex.Message);
                    // Tiếp tục nếu không parse được JSON nhưng HTTP status OK
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error khi gọi GHTK API");
                throw new Exception($"Lỗi kết nối GHTK API: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout khi gọi GHTK API");
                throw new Exception("Timeout khi gọi GHTK API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi tạo đơn GHTK");
                throw;
            }
        }

        public async Task<bool> AddToCartAsync(AddToCartRequest request)
        {
            var variantValueIds = request.VariantValueIds?.OrderBy(id => id).ToList() ?? new List<int>();

            var allExistingCarts = await _context.Carts
                .Where(c => c.UserId == request.UserId && c.ProductId == request.ProductId)
                .ToListAsync();

            Carts? existingCart = null;

            foreach (var existing in allExistingCarts)
            {
                var cartVariantValueIds = await _context.Set<CartItemVariants>()
                    .Where(v => v.CartId == existing.Id)
                    .Select(v => v.VariantValueId)
                    .OrderBy(id => id)
                    .ToListAsync();

                if (variantValueIds.SequenceEqual(cartVariantValueIds))
                {
                    existingCart = existing;
                    break;
                }
            }



            int availableQuantity = 0;

            if (variantValueIds.Count > 0)
            {
                var productVariants = await _context.ProductVariants
                    .Where(pv => pv.ProductId == request.ProductId)
                    .ToListAsync();

                foreach (var pv in productVariants)
                {
                    var compositionIds = await _context.VariantCompositions
                        .Where(vc => vc.ProductVariantId == pv.Id)
                        .Select(vc => vc.VariantValueId)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .OrderBy(id => id)
                        .ToListAsync();

                    if (compositionIds.SequenceEqual(variantValueIds))
                    {
                        availableQuantity = pv.Quantity;

                        // Nếu đã tồn tại cart → kiểm tra tổng quantity
                        if (existingCart != null)
                        {
                            if (existingCart.Quantity + request.Quantity > availableQuantity)
                                return false;
                        }
                        else
                        {
                            if (request.Quantity > availableQuantity)
                                return false;
                        }

                        break;
                    }
                }

                if (availableQuantity == 0)
                    return false; // Không khớp với biến thể nào → không thể thêm
            }
            else
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId);
                if (product == null) return false;

                availableQuantity = product.Quantity;

                if (existingCart != null)
                {
                    if (existingCart.Quantity + request.Quantity > availableQuantity)
                        return false;
                }
                else
                {
                    if (request.Quantity > availableQuantity)
                        return false;
                }
            }

            // Cập nhật nếu đã có
            if (existingCart != null)
            {
                existingCart.Quantity += request.Quantity;
                await _context.SaveChangesAsync();
                return true;
            }

            // Thêm mới cart
            var cart = new Carts
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CreateAt = DateTime.Now
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            foreach (var variantValueId in variantValueIds)
            {
                _context.Set<CartItemVariants>().Add(new CartItemVariants
                {
                    CartId = cart.Id,
                    VariantValueId = variantValueId
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> CancelGHTKOrderAsync(string orderCode, int userId)
        {
            try
            {
                var token = _configuration["GHTK:Token"];

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://services.giaohangtietkiem.vn");
                    client.DefaultRequestHeaders.Add("Token", token);

                    var response = await client.PostAsync($"/services/shipment/cancel/{orderCode}", null);

                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Kết quả hủy đơn {orderCode}: {content}");

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi hủy đơn {orderCode}");
                return false;
            }
        }

        public async Task<GHTKOrderStatusViewModel> GetGHTKOrderStatusAsync(string orderCode)
        {
            var token = _configuration["GHTK:Token"];
            var baseUrl = _configuration["GHTK:BaseUrl"];

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Token", token);

            var response = await client.GetAsync($"{baseUrl}/services/shipment/v2/{orderCode}");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Lỗi gọi GHTK: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("order", out var orderElement))
                throw new Exception("Không tìm thấy thông tin đơn hàng");

            var result = new GHTKOrderStatusViewModel
            {
                OrderCode = orderElement.GetProperty("label_id").GetString(),
                PartnerId = orderElement.GetProperty("partner_id").GetString(),
                Status = orderElement.GetProperty("status").GetInt32(),
                StatusText = orderElement.GetProperty("status_text").GetString(),
                Created = DateTime.Parse(orderElement.GetProperty("created").GetString()),
                Modified = DateTime.Parse(orderElement.GetProperty("modified").GetString()),
                PickDate = DateTime.TryParse(orderElement.GetProperty("pick_date").GetString(), out var pd) ? pd : null,
                DeliverDate = DateTime.TryParse(orderElement.GetProperty("deliver_date").GetString(), out var dd) ? dd : null,
                ShipMoney = orderElement.GetProperty("ship_money").GetDecimal(),
                Insurance = orderElement.GetProperty("insurance").GetDecimal(),
                Value = orderElement.GetProperty("value").GetDecimal(),
                Weight = orderElement.GetProperty("weight").GetDecimal(),
                PickMoney = orderElement.GetProperty("pick_money").GetDecimal(),
                IsFreeship = orderElement.GetProperty("is_freeship").GetInt32() == 1
            };

            // Lấy danh sách sản phẩm
            if (orderElement.TryGetProperty("products", out var productsElement) && productsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in productsElement.EnumerateArray())
                {
                    result.Products.Add(new GHTKProductStatusViewModel
                    {
                        FullName = p.GetProperty("full_name").GetString(),
                        ProductCode = p.GetProperty("product_code").GetString(),
                        Weight = p.GetProperty("weight").GetDecimal(),
                        Quantity = p.GetProperty("quantity").GetInt32(),
                        Cost = p.GetProperty("cost").GetDecimal()
                    });
                }
            }

            return result;
        }


        public async Task<bool> RemoveFromCartAsync(int cartId)
        {
            var cart = await _context.Carts.FindAsync(cartId);
            if (cart == null) return false;

            var cartVariants = await _context.Set<CartItemVariants>()
                .Where(v => v.CartId == cartId)
                .ToListAsync();

            _context.Set<CartItemVariants>().RemoveRange(cartVariants);

            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateQuantityAsync(int cartId, int newQuantity)
        {
            var cart = await _context.Carts.FindAsync(cartId);
            if (cart == null || newQuantity < 1) return false;

            cart.Quantity = newQuantity;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateSelectionAsync(List<int> selectedCartIds)
        {
            var carts = await _context.Carts.ToListAsync();

            foreach (var cart in carts)
            {
                cart.IsSelected = selectedCartIds.Contains(cart.Id);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> ClearSelectedAsync(int userId)
        {
            var items = await _context.Carts
                .Where(x => x.UserId == userId && x.IsSelected)
                .ToListAsync();

            if (items.Count == 0) return 0;

            _context.Carts.RemoveRange(items);
            return await _context.SaveChangesAsync();
        }
    }

}
