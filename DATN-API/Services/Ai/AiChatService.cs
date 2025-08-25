using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using DATN_API.Data;
using Microsoft.EntityFrameworkCore;
using DATN_API.Models;

namespace DATN_API.Services.Ai
{
    public class AiChatService : IAiChatService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _cfg;

        public AiChatService(ApplicationDbContext db, IHttpClientFactory http, IConfiguration cfg)
        {
            _db = db;
            _http = http;
            _cfg = cfg;
        }

        public async Task<string> AskAsync(string message)
        {
            // 0) Trả lời nhanh cho các câu quen thuộc (không tốn gọi AI)
            var quick = QuickAnswers(message);
            if (!string.IsNullOrEmpty(quick)) return quick;

            // 1) Chỉ nhận câu hỏi về THỜI TRANG
            if (!IsCustomerFashionQuestion(message))
            {
                return "Xin lỗi, TRỢ LÝ GỜ Ô chỉ hỗ trợ các câu hỏi liên quan **thời trang** (áo, quần, váy, giày dép, túi xách, phụ kiện) trong sàn của chúng tôi ạ.";
            }

            // 1.1) THỬ TRẢ LỜI TRỰC TIẾP THEO DANH MỤC/TẦM GIÁ (không gọi AI)
            var direct = await TryAnswerWithCatalogAsync(message);
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            // 2) Ảnh chụp catalog thời trang (KHÔNG nhắc CSDL/DB), có kèm URL sản phẩm
            var context = await BuildFashionCatalogContextAsync();

            // 3) Gọi Gemini (ưu tiên model tiết kiệm quota)
            var apiKey = _cfg["Gemini:ApiKey"];
            var model = _cfg["Gemini:Model"] ?? "gemini-1.5-flash";
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var systemInstruction =
@"Bạn là **TRỢ LÝ GỜ Ô**, chuyên tư vấn mua sắm **thời trang**.
- Chỉ nói về quần áo, váy đầm, giày dép, túi xách, balo, phụ kiện (mũ, thắt lưng, vớ/tất, trang sức thời trang...).
- Khi khách hỏi danh mục/sản phẩm (ví dụ 'áo thun', 'quần jean', 'giày sneaker'): hãy gợi ý tên sản phẩm, giá, số lượng nếu có.
- Khi liệt kê sản phẩm, hiển thị dạng: 
  🛍️ **Tên sản phẩm** — Giá ~ SL
  🔗 URL
- Không tự bịa link; chỉ dùng trường Url đã cung cấp.
- Khi khách hỏi cách mua: hướng dẫn 'Thêm vào giỏ' → 'Thanh toán' → chọn 'VNPay' hoặc 'COD (trả khi nhận hàng)'.
- Có thể gợi ý size & phối đồ cơ bản nếu khách cần.
- Tuyệt đối không nhắc tới nguồn dữ liệu kỹ thuật, CSDL, SQL, schema, backend.
- Không trả lời câu hỏi ngoài thời trang.";

            var userPrompt =
$@"THÔNG TIN THỜI TRANG (tóm tắt để tư vấn):
{context}

CÂU HỎI CỦA KHÁCH:
{message}

HƯỚNG DẪN TRẢ LỜI:
- Trả lời ngắn gọn, tiếng Việt, ưu tiên format có icon.
- Format sản phẩm: 🛍️ **Tên** — Giá ~ SL, dòng tiếp theo: 🔗 URL
- Có thể gợi ý sản phẩm thay thế cùng danh mục nếu từ khóa không đúng chính tả.
- Nếu khách muốn đặt hàng: nhắc 2 cách thanh toán **VNPay** / **COD** và có thể nhập **voucher** nếu đủ điều kiện.";

            var payload = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = userPrompt } } }
                },
                generationConfig = new { temperature = 0.2, topK = 40, topP = 0.95, maxOutputTokens = 1024 }
            };

            var json = JsonSerializer.Serialize(payload);
            var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var client = _http.CreateClient();
            var res = await client.SendAsync(req);
            var resText = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                // Fallback mềm khi quota/lỗi, vẫn trả lời hữu ích cho khách
                return SoftFallback(message) + $"\n\n_(Thông báo kỹ thuật: {res.StatusCode})_";
            }

            try
            {
                using var doc = JsonDocument.Parse(resText);
                var root = doc.RootElement;
                var textParts = new List<string>();
                foreach (var cand in root.GetProperty("candidates").EnumerateArray())
                {
                    if (cand.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts))
                    {
                        foreach (var p in parts.EnumerateArray())
                        {
                            if (p.TryGetProperty("text", out var t))
                                textParts.Add(t.GetString() ?? "");
                        }
                    }
                }
                var answer = string.Join("\n", textParts).Trim();
                answer = SanitizeTechnicalHints(answer);

                if (string.IsNullOrWhiteSpace(answer))
                    answer = SoftFallback(message);

                return answer;
            }
            catch
            {
                return SoftFallback(message);
            }
        }

        // ================== Helpers ==================

        // Dựng URL sản phẩm từ cấu hình
        private string BuildProductUrl(int id, string? slug = null)
        {
            var baseUrl = (_cfg["PublicSite:BaseUrl"] ?? "").TrimEnd('/');

            // Nếu không có baseUrl trong config, dùng default
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = "https://localhost:7180"; // Thay bằng domain thật của bạn
            }

            var pattern = _cfg["PublicSite:ProductDetailPattern"] ?? "/Products/Detailproducts/{id}";
            var url = pattern.Replace("{id}", id.ToString())
                             .Replace("{slug}", string.IsNullOrWhiteSpace(slug) ? id.ToString() : slug);

            var fullUrl = $"{baseUrl}{url}";

            // Debug log để kiểm tra
            Console.WriteLine($"BuildProductUrl - BaseUrl: {baseUrl}, Pattern: {pattern}, FullUrl: {fullUrl}");

            return fullUrl;
        }

        // Trả lời nhanh các câu phổ biến
        private string QuickAnswers(string msgRaw)
        {
            var msg = (msgRaw ?? "").ToLowerInvariant();

            // Cách đặt hàng
            if (Regex.IsMatch(msg, @"(đặt|mua).*(hàng|sp|sản phẩm)") || msg.Contains("cách mua") || msg.Contains("mua sao"))
            {
                return "📋 **Cách đặt hàng**:\n" +
                       "1️⃣ Vào trang sản phẩm → bấm **Thêm vào giỏ**\n" +
                       "2️⃣ Mở **Giỏ hàng** → bấm **Thanh toán**\n" +
                       "3️⃣ Chọn **VNPay** (online) **hoặc COD** (trả khi nhận hàng)\n" +
                       "4️⃣ Nhập **voucher** (nếu có) → kiểm tra phí ship & địa chỉ → **Đặt hàng**";
            }

            // Thanh toán
            if (msg.Contains("thanh toán") || msg.Contains("payment") || msg.Contains("vnpay") || msg.Contains("cod"))
            {
                return "💳 **Thanh toán hỗ trợ**: **VNPay** (online) & **COD** (trả khi nhận hàng)\n" +
                       "🎫 Nhập **voucher** ở bước thanh toán nếu đơn đủ điều kiện";
            }

            // Thời gian giao
            if (msg.Contains("bao lâu") || msg.Contains("mấy ngày") || msg.Contains("thời gian giao"))
            {
                return "🚚 **Thời gian giao hàng**: nội thành ~1–3 ngày, ngoại tỉnh ~3–7 ngày (tùy tuyến & hãng vận chuyển)\n" +
                       "📍 Dự kiến chính xác hiển thị ở bước **Thanh toán** sau khi nhập địa chỉ";
            }

            // Đổi trả
            if (msg.Contains("đổi trả") || msg.Contains("trả hàng") || msg.Contains("đổi hàng"))
            {
                return "🔄 **Đổi/Trả hàng**: trong 7 ngày, sản phẩm còn nguyên tag/mác; liên hệ CSKH để được hướng dẫn chi tiết";
            }

            // Tra cứu đơn
            if (msg.Contains("tra cứu") || msg.Contains("xem đơn") || msg.Contains("đơn của tôi") || msg.Contains("trạng thái đơn"))
            {
                return "📦 **Xem đơn hàng**: vào **Tài khoản → Đơn hàng** để theo dõi trạng thái vận chuyển & chi tiết đơn";
            }

            // Size
            if (msg.Contains("size") || msg.Contains("kích cỡ") || msg.Contains("vừa không"))
            {
                return "📏 **Tư vấn size**: bạn cho mình chiều cao/cân nặng/thói quen mặc (ôm/thoải mái) để mình gợi ý size chuẩn\n" +
                       "👟 Với giày: bạn cho số đo chiều dài bàn chân (cm) nhé";
            }

            return string.Empty;
        }

        // Chỉ cho câu hỏi THỜI TRANG
        private bool IsCustomerFashionQuestion(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return false;
            string[] keys =
            {
                // Sản phẩm & danh mục thời trang
                "áo","quần","váy","đầm","sơ mi","hoodie","áo khoác","quần jean","jeans","chân váy","áo thun",
                "giày","dép","sandal","sneaker","boot","túi","balo","phụ kiện","mũ","nón","thắt lưng","tất","vớ",
                "thời trang","size","kích cỡ","phối đồ","shop","cửa hàng",
                // Quy trình mua sắm
                "giỏ hàng","đặt hàng","mua hàng","thanh toán","vnpay","cod","voucher","khuyến mãi","giảm giá","ship","giao hàng","phí ship","đổi trả","bảo hành"
            };
            return keys.Any(k => msg.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) || msg.Length >= 6;
        }

        // Build context CHỈ thời trang (có kèm URL sản phẩm)
        private async Task<string> BuildFashionCatalogContextAsync()
        {
            var fashionKeys = new[] {
                "áo","quần","váy","đầm","giày","dép","sandal","sneaker","boot","túi","balo","phụ kiện",
                "thời trang","jean","sơ mi","hoodie","khoác","chân váy","áo thun"
            };

            // Danh mục thời trang + 5 sản phẩm mới nhất (kèm URL)
            var categoriesRaw = await _db.Categories
                .Where(c => fashionKeys.Any(k => c.Name.Contains(k)))
                .Select(c => new {
                    c.Name,
                    Products = c.Products
                        .OrderByDescending(p => p.UpdateAt)
                        .Select(p => new { p.Id, p.Slug, p.Name, Price = p.CostPrice, p.Quantity })
                        .Take(5)
                        .ToList()
                })
                .ToListAsync();

            var categories = categoriesRaw.Select(c => new {
                c.Name,
                Products = c.Products.Select(p => new {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Quantity,
                    Url = BuildProductUrl(p.Id, p.Slug)
                }).ToList()
            }).ToList();

            // Top bán chạy 30 ngày (chỉ thời trang) + URL
            var from30 = DateTime.UtcNow.AddDays(-30);
            var topProducts30Raw = await _db.OrderDetails
                .Where(od => od.Order.OrderDate >= from30 &&
                             od.Product.Category != null &&
                             fashionKeys.Any(k => od.Product.Category.Name.Contains(k)))
                .GroupBy(od => new { od.ProductId, od.Product.Name, od.Product.Slug })
                .Select(g => new { g.Key.ProductId, g.Key.Name, g.Key.Slug, Qty = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Qty)
                .Take(5)
                .ToListAsync();

            var topProductsLast30Days = topProducts30Raw.Select(t => new {
                t.ProductId,
                t.Name,
                t.Qty,
                Url = BuildProductUrl(t.ProductId, t.Slug)
            }).ToList();

            // Voucher còn hiệu lực
            var today = DateTime.UtcNow;
            var activeVouchers = await _db.Vouchers
                .Where(v => v.StartDate <= today &&
                            v.EndDate >= today &&
                            v.Status == VoucherStatus.Valid &&
                            v.Quantity > 0)
                .OrderByDescending(v => v.Reduce)
                .Select(v => new { v.Id, v.MinOrder, v.Reduce, v.Quantity, v.EndDate })
                .Take(5)
                .ToListAsync();

            var overview = new
            {
                Categories = categories,
                TopSellersLast30Days = topProductsLast30Days,
                VouchersAvailable = activeVouchers
            };

            return JsonSerializer.Serialize(overview, new JsonSerializerOptions { WriteIndented = true });
        }

        // ======== Các helper bổ sung để trả lời trực tiếp không gọi AI ========

        // Cố gắng trả lời theo danh mục + tầm giá với link sản phẩm (Format mới)
        private async Task<string?> TryAnswerWithCatalogAsync(string message)
        {
            var cats = await FindMatchingCategoriesAsync(message);
            var (min, max) = ExtractBudget(message);

            if (!cats.Any() && min == null && max == null)
                return null; // để AI xử lý

            // Không có danh mục nhưng có tầm giá → gợi ý top thời trang theo giá
            if (!cats.Any())
            {
                var fashionKeys = new[] { "áo", "quần", "váy", "đầm", "jean", "giày", "dép", "sandal", "sneaker", "boot", "túi", "balo", "phụ kiện", "sơ mi", "hoodie", "khoác", "áo thun" };
                var q = _db.Products.AsQueryable()
                    .Where(p => p.Category != null && fashionKeys.Any(k => p.Category.Name.Contains(k)));

                if (min != null) q = q.Where(p => p.CostPrice >= min);
                if (max != null) q = q.Where(p => p.CostPrice <= max);

                var items = await q.OrderByDescending(p => p.UpdateAt)
                    .Take(5)
                    .Select(p => new { p.Id, p.Slug, p.Name, p.CostPrice, p.Quantity })
                    .ToListAsync();

                if (!items.Any())
                    return "❌ Mình chưa tìm thấy sản phẩm thời trang trong tầm giá bạn nêu. Bạn cho mình biết loại (áo/quần/váy/giày…) để mình gợi ý chính xác hơn nhé.";

                var sbAny = new StringBuilder();
                sbAny.AppendLine("💡 Mình gợi ý một vài mẫu trong tầm giá bạn quan tâm:");
                sbAny.AppendLine();
                foreach (var p in items)
                {
                    var url = BuildProductUrl(p.Id, p.Slug);
                    sbAny.AppendLine($"🛍️ **{p.Name}** — Giá {p.CostPrice:N0}đ ~ SL {p.Quantity}");
                    sbAny.AppendLine($"🔗 {url}");
                    sbAny.AppendLine();
                }
                sbAny.AppendLine("📋 **Đặt hàng**: Thêm vào giỏ → Thanh toán → **VNPay** hoặc **COD**");
                return sbAny.ToString().Trim();
            }

            // Có danh mục → trả lời từng danh mục
            var sb = new StringBuilder();
            foreach (var (catId, catName) in cats)
            {
                var q = _db.Products.Where(p => p.CategoryId == catId);
                if (min != null) q = q.Where(p => p.CostPrice >= min);
                if (max != null) q = q.Where(p => p.CostPrice <= max);

                var products = await q.OrderByDescending(p => p.UpdateAt)
                    .Take(6)
                    .Select(p => new { p.Id, p.Slug, p.Name, p.CostPrice, p.Quantity })
                    .ToListAsync();

                if (!products.Any())
                {
                    sb.AppendLine($"❌ Hiện danh mục **{catName}** chưa có sản phẩm phù hợp{(min != null || max != null ? " theo tầm giá bạn nêu" : "")}. Bạn muốn xem danh mục khác không?");
                    sb.AppendLine();
                    continue;
                }

                sb.AppendLine($"📂 **{catName}**{(min != null || max != null ? " theo tầm giá" : "")}:");
                sb.AppendLine();
                foreach (var p in products)
                {
                    var url = BuildProductUrl(p.Id, p.Slug);
                    sb.AppendLine($"🛍️ **{p.Name}** — Giá {p.CostPrice:N0}đ ~ SL {p.Quantity}");
                    sb.AppendLine($"🔗 {url}");
                    sb.AppendLine();
                }
            }

            if (sb.Length == 0) return null;
            sb.AppendLine("📋 **Đặt hàng**: Thêm vào giỏ → Thanh toán → **VNPay** hoặc **COD**");
            return sb.ToString().Trim();
        }

        // Bóc tách tầm giá (dưới/trên/khoảng/200-400k/1 triệu…)
        private (decimal? min, decimal? max) ExtractBudget(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return (null, null);
            var s = raw.ToLowerInvariant();

            decimal ToVnd(decimal x, string unit)
            {
                unit = unit?.Trim() ?? "";
                if (unit.StartsWith("k")) return x * 1000m;
                if (unit.StartsWith("ngh") || unit.StartsWith("ngà")) return x * 1000m;
                if (unit.StartsWith("tri")) return x * 1_000_000m;
                if (unit.StartsWith("m")) return x * 1_000_000m; // phòng người dùng ghi "1m"
                return x;
            }

            var matches = Regex.Matches(s, @"(\d+(?:[\.,]\d+)?)\s*(k|nghìn|ngàn|triệu|m)?");
            var values = new List<decimal>();
            foreach (Match m in matches)
            {
                if (decimal.TryParse(m.Groups[1].Value.Replace(".", "").Replace(",", "."), out var num))
                {
                    var unit = m.Groups[2].Success ? m.Groups[2].Value : "";
                    values.Add(ToVnd(num, unit));
                }
            }

            decimal? min = null, max = null;
            if (values.Count >= 2)
            {
                min = Math.Min(values[0], values[1]);
                max = Math.Max(values[0], values[1]);
            }
            else if (values.Count == 1)
            {
                if (s.Contains("dưới") || s.Contains("<") || s.Contains("<= "))
                    max = values[0];
                else if (s.Contains("trên") || s.Contains(">") || s.Contains(">= "))
                    min = values[0];
                else if (s.Contains("khoảng") || s.Contains("tầm") || s.Contains("cỡ"))
                {
                    min = values[0] * 0.8m; max = values[0] * 1.2m;
                }
                else max = values[0];
            }

            return (min, max);
        }

        // Bỏ dấu tiếng Việt
        private string StripDiacritics(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // Tìm danh mục khớp với câu hỏi
        private async Task<List<(int Id, string Name)>> FindMatchingCategoriesAsync(string message)
        {
            var m = StripDiacritics((message ?? "").ToLowerInvariant());
            var allCats = await _db.Categories.Select(c => new { c.Id, c.Name }).ToListAsync();

            var result = new List<(int, string)>();
            foreach (var c in allCats)
            {
                var nameStripped = StripDiacritics((c.Name ?? "").ToLowerInvariant());
                if (!string.IsNullOrWhiteSpace(nameStripped) && m.Contains(nameStripped))
                    result.Add((c.Id, c.Name));
            }

            // Fallback heuristics: nếu người dùng nói chung chung, vẫn cố match theo từ khóa thời trang
            if (!result.Any())
            {
                var fallbacks = new[] { "áo", "quần", "váy", "đầm", "jean", "giày", "dép", "sandal", "sneaker", "boot", "túi", "balo", "phụ kiện", "sơ mi", "hoodie", "khoác", "áo thun" };
                foreach (var c in allCats)
                {
                    var ns = StripDiacritics((c.Name ?? "").ToLowerInvariant());
                    if (fallbacks.Any(k => ns.Contains(StripDiacritics(k))) && m.Any())
                    {
                        // Nếu câu hỏi có chứa bất kỳ từ khóa fallback, và tên danh mục chứa cùng nhóm
                        if (fallbacks.Any(k => m.Contains(StripDiacritics(k)) && ns.Contains(StripDiacritics(k))))
                            result.Add((c.Id, c.Name));
                    }
                }
            }

            return result.Distinct().Take(3).ToList();
        }

        // Ẩn mọi từ khóa kỹ thuật nếu model lỡ nói
        private string SanitizeTechnicalHints(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            string[] banned = { "csdl", "cơ sở dữ liệu", "database", "sql", "bảng", "schema", "ef core", "context", "truy vấn", "grounding", "dữ liệu nội bộ" };
            foreach (var b in banned)
                s = Regex.Replace(s, b, "hệ thống", RegexOptions.IgnoreCase);
            return s;
        }

        // Fallback hữu ích khi AI lỗi/hết quota
        private string SoftFallback(string msg)
        {
            if (Regex.IsMatch(msg, "(quần áo|thời trang|áo|quần|váy|đầm)", RegexOptions.IgnoreCase))
                return "📂 Danh mục **Thời trang** đang có nhiều mẫu mới (áo thun, sơ mi, quần jean, váy/đầm...). Bạn thích kiểu nào và tầm giá bao nhiêu ạ?\n" +
                       "📋 **Cách mua**: Thêm vào giỏ → Thanh toán → **VNPay** hoặc **COD**. Có thể nhập **voucher** nếu có.";
            if (Regex.IsMatch(msg, "(giày|dép|sandal|sneaker|boot)", RegexOptions.IgnoreCase))
                return "👟 **Giày dép** có sneaker, sandal, boot,... Bạn cần kiểu nào và size bao nhiêu ạ?\n" +
                       "📋 **Đặt hàng**: Thêm vào giỏ → Thanh toán → **VNPay**/**COD**.";
            if (Regex.IsMatch(msg, "(túi|balo|phụ kiện|mũ|nón|thắt lưng|tất|vớ)", RegexOptions.IgnoreCase))
                return "👜 **Túi/balo/phụ kiện** đang có sẵn. Bạn cần loại nào và tầm giá ạ?\n" +
                       "📋 **Đặt hàng**: Thêm vào giỏ → Thanh toán → **VNPay**/**COD**.";

            return "💬 Bạn muốn tìm sản phẩm thời trang nào (áo, quần, váy, giày, túi, phụ kiện)? Hãy cho mình biết tầm giá hoặc size để mình gợi ý phù hợp nhé.\n" +
                   "📋 Khi chọn được, bấm **Thêm vào giỏ** rồi **Thanh toán** bằng **VNPay** hoặc **COD**.";
        }
    }
}