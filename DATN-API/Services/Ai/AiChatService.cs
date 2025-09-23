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
            if (string.IsNullOrWhiteSpace(message))
                return "Xin lỗi, bạn chưa nhập câu hỏi. Hãy hỏi tôi về thời trang nhé!";

            // Làm sạch input
            var cleanMessage = CleanInput(message);

            // 0) Trả lời nhanh cho các câu quen thuộc
            var quickResponse = GetQuickAnswer(cleanMessage);
            if (!string.IsNullOrEmpty(quickResponse))
                return quickResponse;

            // 1) Kiểm tra có phải câu hỏi thời trang không
            if (!IsFashionRelatedQuestion(cleanMessage))
            {
                return "Xin lỗi, **TRỢ LÝ GỜ Ô** chỉ hỗ trợ tư vấn về **thời trang** (áo quần, giày dép, túi xách, phụ kiện) và quy trình mua hàng. Bạn có muốn hỏi về sản phẩm thời trang nào không?";
            }

            // 2) Thử trả lời trực tiếp từ database
            var directAnswer = await TryDirectAnswerAsync(cleanMessage);
            if (!string.IsNullOrEmpty(directAnswer))
                return directAnswer;

            // 3) Gọi AI với context được tối ưu
            return (await CallAiWithContextAsync(cleanMessage)).Trim();
        }

        private string CleanInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            // Xóa các ký tự không cần thiết, giữ lại dấu câu cơ bản
            return Regex.Replace(input.Trim(), @"\s+", " ");
        }

        private string GetQuickAnswer(string message)
        {
            var msg = message.ToLowerInvariant();

            // Chào hỏi
            if (Regex.IsMatch(msg, @"\b(xin chào|chào|hello|hi)\b"))
            {
                return "Chào bạn! Tôi là **TRỢ LÝ GỜ Ô** - chuyên tư vấn thời trang. Bạn muốn tìm loại sản phẩm nào? (áo, quần, váy, giày, túi...)";
            }

            // Cảm ơn
            if (Regex.IsMatch(msg, @"\b(cảm ơn|cám ơn|thank|thanks)\b"))
            {
                return "Không có gì! Tôi luôn sẵn sàng hỗ trợ bạn tìm kiếm sản phẩm thời trang phù hợp. Còn cần tư vấn gì khác không?";
            }

            // Thống kê cửa hàng
            if (Regex.IsMatch(msg, @"(bao nhiêu|có mấy).*(cửa hàng|shop|store)"))
            {
                return "**🏪 Thông tin Cửa hàng:**\n" +
                       "Hiện tại hệ thống **GỜ Ô** đang vận hành với nhiều cửa hàng trên toàn quốc.\n\n" +
                       "Để biết thông tin chi tiết về địa điểm cửa hàng gần bạn nhất, vui lòng:\n" +
                       "• Truy cập mục **Cửa hàng** trên website\n" +
                       "• Hoặc liên hệ hotline để được hỗ trợ\n\n" +
                       "Bạn cần tìm sản phẩm gì không?";
            }

            // Thống kê sản phẩm
            if (Regex.IsMatch(msg, @"(bao nhiêu|có mấy).*(sản phẩm|sp|món|mặt hàng)"))
            {
                return "**📦 Kho sản phẩm:**\n" +
                       "Hệ thống **GỜ Ô** hiện có hàng ngàn sản phẩm thời trang đa dạng:\n" +
                       "• **Áo**: thun, sơ mi, khoác, hoodie...\n" +
                       "• **Quần**: jean, âu, short, legging...\n" +
                       "• **Váy đầm**: công sở, dự tiệc, casual...\n" +
                       "• **Giày dép**: sneaker, sandal, boot...\n" +
                       "• **Phụ kiện**: túi xách, mũ, trang sức...\n\n" +
                       "Bạn quan tâm loại sản phẩm nào?";
            }

            // Cách đặt hàng
            if (Regex.IsMatch(msg, @"(cách|làm sao).*(đặt|mua).*(hàng|sản phẩm)"))
            {
                return "**🛒 Cách đặt hàng:**\n" +
                       "1️⃣ Chọn sản phẩm → **Thêm vào giỏ**\n" +
                       "2️⃣ Vào **Giỏ hàng** → **Thanh toán**\n" +
                       "3️⃣ Chọn phương thức: **VNPay** (online), **MoMo** (ví điện tử), hoặc **COD** (tiền mặt)\n" +
                       "4️⃣ Nhập **voucher** (nếu có) → Xác nhận đặt hàng\n\n" +
                       "Bạn cần hỗ trợ tìm sản phẩm nào không?";
            }

            // Thanh toán
            if (Regex.IsMatch(msg, @"\b(thanh toán|payment|vnpay|momo|cod|tiền)\b"))
            {
                return "**💳 Phương thức thanh toán:**\n" +
                       "• **VNPay**: Thanh toán online qua thẻ/QR\n" +
                       "• **MoMo**: Thanh toán qua ví điện tử MoMo\n" +
                       "• **COD**: Trả tiền mặt khi nhận hàng\n" +
                       "• **Voucher**: Giảm giá khi đơn hàng đủ điều kiện\n\n" +
                       "Bạn muốn xem sản phẩm nào để đặt hàng?";
            }

            // Giao hàng
            if (Regex.IsMatch(msg, @"(giao hàng|ship|vận chuyển|bao lâu|mấy ngày)"))
            {
                return "**🚚 Thông tin giao hàng:**\n" +
                       "• **Nội thành**: 1-3 ngày làm việc\n" +
                       "• **Ngoại tỉnh**: 3-7 ngày làm việc\n" +
                       "• **Phí ship**: Tính theo địa chỉ (hiển thị khi thanh toán)\n\n" +
                       "Bạn đang quan tâm sản phẩm nào để đặt hàng?";
            }

            return string.Empty;
        }

        private bool IsFashionRelatedQuestion(string message)
        {
            var msg = StripDiacritics(message).ToLowerInvariant();

            // Từ chối rõ ràng các chủ đề không phải thời trang
            string[] excludedTopics = {
                "nguoi yeu", "yeu duong", "tinh yeu", "ban gai", "ban trai", "crush",
                "hoc tap", "lam bai tap", "bai hoc", "thi cu", "diem so",
                "lam viec", "cong viec", "luong", "phong van", "xin viec",
                "suc khoe", "benh tat", "thuoc", "bac si", "benh vien",
                "gia dinh", "cha me", "anh em", "con cai", "cuoi hoi",
                "thoi tiet", "du lich", "xe cong", "giao thong",
                "am thuc", "mon an", "nau an", "quan com", "nha hang",
                "dien thoai", "may tinh", "cong nghe", "internet", "game",
                "chinh tri", "phap luat", "luat", "toa an", "canh sat"
            };

            // Nếu chứa chủ đề bị loại trừ, không phải thời trang
            if (excludedTopics.Any(topic => msg.Contains(topic)))
            {
                return false;
            }

            // Từ khóa thời trang chính (phải chính xác)
            string[] fashionTerms = {
                "ao thun", "ao so mi", "ao khoac", "ao hoodie", "ao vest", "ao polo",
                "quan jean", "quan dai", "quan ngan", "quan tay", "quan ong rong",
                "vay", "dam", "chan vay", "vay maxi", "vay ngan",
                "giay", "dep", "sandal", "sneaker", "boot", "giay cao got", "giay the thao",
                "tui xach", "balo", "vi tien", "tui deo cheo",
                "phu kien", "mu", "non", "that lung", "tat", "vo", "trang suc",
                "dong ho", "kinh mat", "nhan", "day chuyen", "bong tai"
            };

            // Từ khóa quy trình mua hàng (chỉ khi có ngữ cảnh thời trang)
            string[] shoppingTerms = {
                "mua ao", "mua quan", "mua giay", "mua tui", "mua vay", "mua dam",
                "dat hang", "gio hang", "thanh toan", "giao hang", "ship hang",
                "voucher thoi trang", "khuyen mai", "giam gia", "sale off",
                "doi tra", "bao hanh", "vnpay", "momo", "cod"
            };

            // Từ khóa kỹ thuật về thời trang
            string[] fashionSpecs = {
                "size ao", "size quan", "size giay", "size vay", "kich co",
                "mau sac", "chat lieu", "form ao", "form quan",
                "phoi do", "mix do", "style", "thoi trang"
            };
            string[] genericFashionTerms = { "ao", "quan", "giay", "vay", "tui" };
            bool hasGenericTerms = genericFashionTerms.Any(term => msg.Contains(term));
            // Kiểm tra có từ khóa thời trang cụ thể
            bool hasFashionTerms = fashionTerms.Any(term => msg.Contains(term));
            bool hasShoppingTerms = shoppingTerms.Any(term => msg.Contains(term));
            bool hasFashionSpecs = fashionSpecs.Any(term => msg.Contains(term));

            // Kiểm tra tầm giá với ngữ cảnh thời trang
            bool hasPriceWithFashion = Regex.IsMatch(msg, @"\b\d+([.,]\d+)?\s*(k|ngan|nghin|trieu|dong)\b") &&
                                     (msg.Contains("ao") || msg.Contains("quan") || msg.Contains("giay") ||
                                      msg.Contains("tui") || msg.Contains("vay") || msg.Contains("phu kien"));

            // Chỉ return true khi có bằng chứng rõ ràng về thời trang
            bool hasFindWithGeneric = (msg.Contains("tim") || msg.Contains("mua")) && hasGenericTerms;

            return hasFashionTerms
                || hasShoppingTerms
                || hasFashionSpecs
                || hasPriceWithFashion
                || hasGenericTerms
                || hasFindWithGeneric;
        }

        private async Task<string> TryDirectAnswerAsync(string message)
        {
            // Tìm danh mục phù hợp
            var categories = await FindMatchingCategoriesAsync(message);
            var (minPrice, maxPrice) = ExtractPriceRange(message);

            if (categories.Any() || minPrice.HasValue || maxPrice.HasValue)
            {
                return await BuildDirectProductResponseAsync(categories, minPrice, maxPrice, message);
            }

            return string.Empty;
        }

        private async Task<List<(int Id, string Name)>> FindMatchingCategoriesAsync(string message)
        {
            var normalizedMessage = StripDiacritics(message.ToLowerInvariant());

            var allCategories = await _db.Categories
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            var matchedCategories = new List<(int, string)>();

            // Kiểm tra giới tính trong tin nhắn
            bool isFemale = normalizedMessage.Contains("nu") || normalizedMessage.Contains("nữ") ||
                           normalizedMessage.Contains("cho nu") || normalizedMessage.Contains("cho nữ") ||
                           normalizedMessage.Contains("danh cho nu") || normalizedMessage.Contains("dành cho nữ");
            bool isMale = normalizedMessage.Contains("nam") && !isFemale; // Ưu tiên nữ nếu có cả hai

            foreach (var category in allCategories)
            {
                var normalizedCategoryName = StripDiacritics(category.Name.ToLowerInvariant());

                // Kiểm tra match trực tiếp
                if (normalizedMessage.Contains(normalizedCategoryName))
                {
                    matchedCategories.Add((category.Id, category.Name));
                    continue;
                }

                // Kiểm tra với từ khóa thời trang và giới tính
                var fashionKeywords = new Dictionary<string, string[]>
                {
                    ["ao"] = new[] { "áo", "shirt", "top" },
                    ["quan"] = new[] { "quần", "pant", "jean" },
                    ["vay"] = new[] { "váy", "skirt", "dress" },
                    ["giay"] = new[] { "giày", "shoe", "sneaker" },
                    ["tui"] = new[] { "túi", "bag", "balo" }
                };

                foreach (var kvp in fashionKeywords)
                {
                    if (kvp.Value.Any(keyword => normalizedMessage.Contains(StripDiacritics(keyword))) &&
                        normalizedCategoryName.Contains(kvp.Key))
                    {
                        // Kiểm tra giới tính để lọc category phù hợp
                        if (isFemale && normalizedCategoryName.Contains("nam"))
                        {
                            continue; // Bỏ qua category nam khi tìm cho nữ
                        }
                        if (isMale && normalizedCategoryName.Contains("nu"))
                        {
                            continue; // Bỏ qua category nữ khi tìm cho nam
                        }

                        matchedCategories.Add((category.Id, category.Name));
                        break;
                    }
                }
            }

            // Nếu có yêu cầu giới tính cụ thể nhưng không tìm thấy, tìm category phù hợp
            if (!matchedCategories.Any() && (isFemale || isMale))
            {
                var genderKeyword = isFemale ? "nu" : "nam";
                var alternativeCategories = allCategories.Where(c =>
                    StripDiacritics(c.Name.ToLowerInvariant()).Contains(genderKeyword)).ToList();

                foreach (var cat in alternativeCategories)
                {
                    matchedCategories.Add((cat.Id, cat.Name));
                }
            }

            return matchedCategories.Distinct().Take(3).ToList();
        }

        private (decimal? min, decimal? max) ExtractPriceRange(string message)
        {
            var normalizedMessage = message.ToLowerInvariant();

            // Tìm các số trong tin nhắn
            var numberMatches = Regex.Matches(normalizedMessage, @"(\d+(?:[.,]\d+)?)\s*(k|nghin|ngan|trieu|dong|đ)?");
            var prices = new List<decimal>();

            foreach (Match match in numberMatches)
            {
                if (decimal.TryParse(match.Groups[1].Value.Replace(",", "."), out decimal number))
                {
                    var unit = match.Groups[2].Value;
                    decimal multiplier = unit switch
                    {
                        "k" or "nghin" or "ngan" => 1000,
                        "trieu" => 1000000,
                        _ => 1
                    };
                    prices.Add(number * multiplier);
                }
            }

            if (!prices.Any()) return (null, null);

            // Xử lý các trường hợp khác nhau
            if (prices.Count == 1)
            {
                var price = prices[0];
                if (normalizedMessage.Contains("duoi") || normalizedMessage.Contains("<"))
                    return (null, price);
                if (normalizedMessage.Contains("tren") || normalizedMessage.Contains(">"))
                    return (price, null);
                if (normalizedMessage.Contains("khoang") || normalizedMessage.Contains("tam"))
                    return (price * 0.8m, price * 1.2m);
                return (null, price); // Mặc định là giá tối đa
            }

            // Nhiều giá -> lấy min, max
            return (prices.Min(), prices.Max());
        }

        private async Task<string> BuildDirectProductResponseAsync(
            List<(int Id, string Name)> categories,
            decimal? minPrice,
            decimal? maxPrice,
            string originalMessage)
        {
            var responseBuilder = new StringBuilder();

            if (categories.Any())
            {
                foreach (var (categoryId, categoryName) in categories)
                {
                    var query = _db.Products.Where(p => p.CategoryId == categoryId);

                    if (minPrice.HasValue) query = query.Where(p => p.CostPrice >= minPrice.Value);
                    if (maxPrice.HasValue) query = query.Where(p => p.CostPrice <= maxPrice.Value);

                    var products = await query
                        .OrderByDescending(p => p.UpdateAt)
                        .Take(5)
                        .Select(p => new { p.Id, p.Slug, p.Name })
                        .ToListAsync();

                    if (products.Any())
                    {
                        responseBuilder.AppendLine($"**📂 {categoryName}**");
                        if (minPrice.HasValue || maxPrice.HasValue)
                        {
                            var priceInfo = minPrice.HasValue && maxPrice.HasValue
                                ? $"{minPrice:N0}đ - {maxPrice:N0}đ"
                                : minPrice.HasValue ? $"từ {minPrice:N0}đ" : $"dưới {maxPrice:N0}đ";
                            responseBuilder.AppendLine($"*(Tầm giá: {priceInfo})*");
                        }
                        responseBuilder.AppendLine();

                        foreach (var product in products)
                        {
                            var url = BuildProductUrl(product.Id, product.Slug);
                            responseBuilder.AppendLine($"🛍️ **{product.Name}**");
                            responseBuilder.AppendLine($"🔗 {url}");
                            responseBuilder.AppendLine();
                        }
                    }
                }
            }
            else if (minPrice.HasValue || maxPrice.HasValue)
            {
                // Chỉ có tầm giá, không có danh mục cụ thể
                var fashionQuery = _db.Products.Where(p => p.Category != null);

                if (minPrice.HasValue) fashionQuery = fashionQuery.Where(p => p.CostPrice >= minPrice.Value);
                if (maxPrice.HasValue) fashionQuery = fashionQuery.Where(p => p.CostPrice <= maxPrice.Value);

                var fashionProducts = await fashionQuery
                    .OrderByDescending(p => p.UpdateAt)
                    .Take(6)
                    .Select(p => new { p.Id, p.Slug, p.Name, CategoryName = p.Category.Name })
                    .ToListAsync();

                if (fashionProducts.Any())
                {
                    responseBuilder.AppendLine("**💡 Sản phẩm theo tầm giá bạn quan tâm:**");
                    responseBuilder.AppendLine();

                    foreach (var product in fashionProducts)
                    {
                        var url = BuildProductUrl(product.Id, product.Slug);
                        responseBuilder.AppendLine($"🛍️ **{product.Name}** ({product.CategoryName})");
                        responseBuilder.AppendLine($"🔗 {url}");
                        responseBuilder.AppendLine();
                    }
                }
            }

            if (responseBuilder.Length > 0)
            {
                responseBuilder.AppendLine("---");
                responseBuilder.AppendLine("**📋 Cách đặt hàng:** Bấm link → Thêm vào giỏ → Thanh toán → Chọn **VNPay**, **MoMo**, hoặc **COD**");
                return responseBuilder.ToString().Trim();
            }

            return string.Empty;
        }

        private async Task<string> CallAiWithContextAsync(string message)
        {
            try
            {
                var context = await BuildOptimizedContextAsync();
                var systemPrompt = BuildSystemPrompt();
                var userPrompt = BuildUserPrompt(context, message);

                var apiKey = _cfg["Gemini:ApiKey"];
                var model = _cfg["Gemini:Model"] ?? "gemini-1.5-flash";
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                var payload = new
                {
                    system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = userPrompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.3,
                        topK = 40,
                        topP = 0.9,
                        maxOutputTokens = 800
                    }
                };

                using var client = _http.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(15);

                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return GetFallbackResponse(message);
                }

                var aiResponse = ExtractAiResponse(responseText);
                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    return GetFallbackResponse(message);
                }

                return SanitizeResponse(aiResponse);
            }
            catch (Exception)
            {
                return GetFallbackResponse(message);
            }
        }

        private string BuildSystemPrompt()
        {
            return @"Bạn là TRỢ LÝ GỜ Ô - chuyên gia tư vấn thời trang.

NHIỆM VỤ:
- Tư vấn sản phẩm thời trang: áo, quần, váy, giày, túi, phụ kiện
- Hướng dẫn mua hàng và thanh toán
- Gợi ý phối đồ và size phù hợp

QUY TẮC TRẢ LỜI:
1. Ngắn gọn, dễ hiểu, sử dụng emoji phù hợp
2. Format sản phẩm: 
   🛍️ **Tên sản phẩm**
   🔗 [Link URL]
3. Chỉ sử dụng thông tin được cung cấp, không bịa đặt
4. Kết thúc bằng hướng dẫn đặt hàng nếu có sản phẩm: **VNPay**, **MoMo**, hoặc **COD**
5. Không nhắc đến kỹ thuật, database hay hệ thống backend

PHONG CÁCH: Thân thiện, chuyên nghiệp, hỗ trợ tích cực";
        }

        private string BuildUserPrompt(string context, string message)
        {
            return $@"THÔNG TIN SẢN PHẨM HIỆN TẠI:
{context}

CÂU HỎI KHÁCH HÀNG: {message}

Hãy trả lời câu hỏi dựa trên thông tin sản phẩm trên. Nếu không tìm thấy sản phẩm phù hợp, hãy gợi ý khách hàng cung cấp thêm thông tin (loại sản phẩm, tầm giá, size) để tư vấn tốt hơn.";
        }

        private async Task<string> BuildOptimizedContextAsync()
        {
            // Lấy top categories thời trang
            var categories = await _db.Categories
                .Where(c => c.Products.Any())
                .Select(c => new {
                    c.Name,
                    ProductCount = c.Products.Count(),
                    TopProducts = c.Products
                        .OrderByDescending(p => p.UpdateAt)
                        .Take(3)
                        .Select(p => new { p.Id, p.Slug, p.Name })
                        .ToList()
                })
                .OrderByDescending(c => c.ProductCount)
                .Take(8)
                .ToListAsync();

            // Thêm URL cho sản phẩm
            var categoriesWithUrls = categories.Select(c => new {
                c.Name,
                c.ProductCount,
                TopProducts = c.TopProducts.Select(p => new {
                    p.Id,
                    p.Name,
                    Url = BuildProductUrl(p.Id, p.Slug)
                }).ToList()
            }).ToList();

            // Lấy voucher đang có hiệu lực
            var activeVouchers = await _db.Vouchers
                .Where(v => v.StartDate <= DateTime.UtcNow &&
                           v.EndDate >= DateTime.UtcNow &&
                           v.Status == VoucherStatus.Valid &&
                           v.Quantity > 0)
                .OrderByDescending(v => v.Reduce)
                .Take(3)
                .Select(v => new { v.Id, v.MinOrder, v.Reduce, v.Quantity })
                .ToListAsync();

            var contextData = new
            {
                Categories = categoriesWithUrls,
                ActiveVouchers = activeVouchers
            };

            return JsonSerializer.Serialize(contextData, new JsonSerializerOptions { WriteIndented = true });
        }

        private string ExtractAiResponse(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var candidates = doc.RootElement.GetProperty("candidates");

                foreach (var candidate in candidates.EnumerateArray())
                {
                    if (candidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts))
                    {
                        foreach (var part in parts.EnumerateArray())
                        {
                            if (part.TryGetProperty("text", out var text))
                            {
                                return text.GetString() ?? "";
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return string.Empty;
        }

        private string SanitizeResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return response;

            // Loại bỏ các từ khóa kỹ thuật
            var technicalTerms = new[] {
                "database", "csdl", "cơ sở dữ liệu", "sql", "query", "backend",
                "api", "json", "context", "grounding", "dữ liệu nội bộ"
            };

            foreach (var term in technicalTerms)
            {
                response = Regex.Replace(response, term, "hệ thống", RegexOptions.IgnoreCase);
            }

            return response.Trim();
        }

        private string GetFallbackResponse(string message)
        {
            var msg = message.ToLowerInvariant();

            if (Regex.IsMatch(msg, @"(áo|quần|thời trang)"))
            {
                return "**🛍️ Danh mục Thời trang**\n\n" +
                       "Hiện tại shop có nhiều sản phẩm đa dạng: áo thun, sơ mi, quần jean, váy đầm...\n\n" +
                       "Bạn cho mình biết:\n" +
                       "• **Loại sản phẩm**: áo/quần/váy/giày/túi?\n" +
                       "• **Tầm giá**: khoảng bao nhiêu?\n" +
                       "• **Size**: S/M/L hay size số?\n\n" +
                       "**📋 Đặt hàng**: Thêm vào giỏ → Thanh toán → **VNPay**/**MoMo**/**COD**";
            }

            if (Regex.IsMatch(msg, @"(giày|dép)"))
            {
                return "**👟 Danh mục Giày dép**\n\n" +
                       "Shop có: sneaker, sandal, boot, giày cao gót...\n\n" +
                       "Bạn cho mình biết size chân để tư vấn chính xác nhé!\n\n" +
                       "**📋 Đặt hàng**: Chọn sản phẩm → **VNPay**/**MoMo**/**COD**";
            }

            return "**💬 Tôi là TRỢ LÝ GỜ Ô**\n\n" +
                   "Tôi có thể hỗ trợ bạn:\n" +
                   "• 🛍️ Tìm sản phẩm thời trang\n" +
                   "• 💰 Tư vấn tầm giá phù hợp\n" +
                   "• 📏 Gợi ý size chuẩn\n" +
                   "• 🛒 Hướng dẫn đặt hàng\n\n" +
                   "Bạn đang tìm loại sản phẩm nào?";
        }

        private string BuildProductUrl(int id, string? slug = null)
        {
            var baseUrl = _cfg["PublicSite:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:7180";
            var pattern = _cfg["PublicSite:ProductDetailPattern"] ?? "/Products/Detailproducts/{id}";

            var url = pattern.Replace("{id}", id.ToString())
                             .Replace("{slug}", string.IsNullOrWhiteSpace(slug) ? id.ToString() : slug);

            return $"{baseUrl}{url}";
        }

        private string StripDiacritics(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var normalized = input.Normalize(NormalizationForm.FormD);
            var result = new StringBuilder();

            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    result.Append(c);
            }

            return result.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}