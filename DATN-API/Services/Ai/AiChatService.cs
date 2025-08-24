using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            // 1) Chặn ngoài phạm vi TMĐT (lọc sơ bộ, phòng user hỏi lan man)
            if (!IsEcommerceQuestion(message))
            {
                return "Mình chỉ hỗ trợ các câu hỏi trong lĩnh vực sàn thương mại điện tử (sản phẩm, đơn hàng, cửa hàng, voucher, vận chuyển, khách hàng, doanh thu...) nhé.";
            }

            // 2) Lấy dữ liệu “grounding” từ DB (tóm tắt gọn + số liệu quan trọng)
            var context = await BuildDbContextAsync(message);

            // 3) Gọi Gemini
            var apiKey = _cfg["Gemini:ApiKey"];
            var model = _cfg["Gemini:Model"] ?? "gemini-1.5-pro";

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var systemInstruction =
@"Bạn là trợ lý AI của sàn TMĐT.
QUY TẮC:
- Luôn trả lời dứt khoát dựa trên dữ liệu CSDL cung cấp.
- Nếu dữ liệu không có cho đúng chủ đề, trả lời bằng KẾT LUẬN HÀNH ĐỘNG, KHÔNG dùng câu chung chung như 'chưa có dữ liệu phù hợp'.
- Kết luận hành động = nêu rõ tình trạng hiện tại + gợi ý bước tiếp theo cụ thể (ví dụ: tạo voucher mới, lọc ngày khác, vào trang cấu hình...).
- Trả lời ngắn gọn, tiếng Việt, có số liệu nếu có.

Ví dụ TRÁNH: 'Chưa có dữ liệu phù hợp để trả lời.'
Ví dụ ĐÚNG: 'Hiện không có voucher đang hiệu lực. Bạn có thể tạo voucher mới trong trang Quản trị > Voucher, hoặc thử thay đổi khoảng thời gian lọc.'";


            // Kết hợp message + dữ liệu grounding
            var userPrompt =
$@"DỮ LIỆU CSDL (tóm tắt từ SQL/EF - có thể rút gọn):
{context}

CÂU HỎI NGƯỜI DÙNG:
{message}

YÊU CẦU:
- Chỉ trả lời dựa trên DỮ LIỆU CSDL nếu có.
- Nếu cần tính toán, hãy tính chính xác từ số liệu đã cung cấp.
- Nếu thiếu dữ liệu, nói rõ: 'Chưa có dữ liệu phù hợp để trả lời.' và gợi ý truy vấn/bộ lọc cụ thể (ví dụ khoảng ngày, mã đơn...).";

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents = new[]
                {
                    new {
                        role = "user",
                        parts = new[] { new { text = userPrompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 1024
                }
                // Bạn có thể bổ sung function-calling/tools nếu muốn mô hình chủ động gọi hàm (tài liệu: function calling). 
                // Ở bản đơn giản này, mình feed thẳng số liệu vào prompt. :contentReference[oaicite:3]{index=3}
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
                return $"[AI lỗi] Không gọi được Gemini: {res.StatusCode} - {resText}";
            }

            try
            {
                using var doc = JsonDocument.Parse(resText);
                // Trích xuất text từ candidates[0].content.parts[*].text
                var root = doc.RootElement;
                var textParts = new List<string>();
                foreach (var cand in root.GetProperty("candidates").EnumerateArray())
                {
                    if (cand.TryGetProperty("content", out var content))
                    {
                        if (content.TryGetProperty("parts", out var parts))
                        {
                            foreach (var p in parts.EnumerateArray())
                            {
                                if (p.TryGetProperty("text", out var t))
                                    textParts.Add(t.GetString() ?? "");
                            }
                        }
                    }
                }
                var answer = string.Join("\n", textParts).Trim();
                return string.IsNullOrWhiteSpace(answer) ? "Mình chưa nhận được nội dung trả lời từ AI." : answer;
            }
            catch
            {
                return "Mình chưa trích xuất được câu trả lời từ AI (format response thay đổi?). Kiểm tra lại payload/response ở API Gemini.";
            }
        }

        private bool IsEcommerceQuestion(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return false;
            // Heuristics đơn giản: chứa một số từ khóa TMĐT phổ biến (tiếng Việt & biến thể)
            var keywords = new[]
            {
                "sản phẩm","đơn hàng","đơn","order","doanh thu","doanh số","revenue","voucher",
                "mã giảm","khách hàng","người dùng","user","cửa hàng","store","ship","vận chuyển",
                "giỏ hàng","cart","biến thể","variant","danh mục","category","review","đánh giá",
                "tồn kho","inventory","kho","giá bán","giá vốn","lợi nhuận","top bán","bán chạy"
            };
            var hit = keywords.Any(k => msg.Contains(k, StringComparison.OrdinalIgnoreCase));
            // Cho phép câu hỏi ngắn gọn nhưng có ý: ví dụ "doanh thu tháng này?"
            return hit || msg.Length >= 8;
        }
        private bool IsVoucherQuestion(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return false;
            var keys = new[] { "voucher", "mã giảm", "mgg", "khuyến mãi", "phiếu giảm", "ưu đãi" };
            return keys.Any(k => msg.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<string> BuildDbContextAsync(string message)
        {
            // Bạn có thể tùy biến theo nhu cầu: nếu câu hỏi chứa "doanh thu", lấy số liệu doanh thu; nếu "top bán", tính top...
            // Ở bản mẫu, mình gom một snapshot gọn cho đa số câu hỏi phân tích:
            var now = DateTime.UtcNow;
            var from30 = now.AddDays(-30);

            var productCount = await _db.Products.CountAsync();
            var orderCount30 = await _db.Orders.Where(o => o.OrderDate >= from30).CountAsync();
            var revenue30 = await _db.OrderDetails
                                         .Where(od => od.Order.OrderDate >= from30)
                                         .SumAsync(od => (decimal?)(od.Price * od.Quantity)) ?? 0m;

            // Top 5 sản phẩm bán chạy 30 ngày (theo quantity)
            var topProducts30 = await _db.OrderDetails
                .Where(od => od.Order.OrderDate >= from30)
                .GroupBy(od => new { od.ProductId, od.Product.Name })
                .Select(g => new { g.Key.ProductId, g.Key.Name, Qty = g.Sum(x => x.Quantity), Sales = g.Sum(x => x.Price * x.Quantity) })
                .OrderByDescending(x => x.Qty).Take(5).ToListAsync();

            // Đếm đơn theo trạng thái
            var statusCounts = await _db.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // Số SP theo danh mục (Top 10)
            var catCounts = await _db.Products
                .GroupBy(p => new { p.CategoryId, Cat = p.Category.Name })
                .Select(g => new { g.Key.CategoryId, g.Key.Cat, Count = g.Count() })
                .OrderByDescending(x => x.Count).Take(10).ToListAsync();

            // Cửa hàng top rating (Top 5)
            var topStores = await _db.Stores
                .OrderByDescending(s => s.Rating).Take(5)
                .Select(s => new { s.Id, s.Name, s.Rating, s.Status })
                .ToListAsync();

            // Voucher đang hoạt động (còn hạn & còn số lượng & trạng thái Valid)
            var today = DateTime.UtcNow;
            var activeVouchers = await _db.Vouchers
                .Where(v => v.StartDate <= today
                         && v.EndDate >= today
                         && v.Status == VoucherStatus.Valid
                         && v.Quantity > 0)
                .Select(v => new
                {
                    v.Id,
                    v.Type,          // VoucherType (Platform/Shop)
                    v.MinOrder,
                    v.Reduce,
                    v.Quantity,
                    v.CategoryId,
                    v.StoreId,
                    v.StartDate,
                    v.EndDate,
                    v.Status         // để AI biết rõ trạng thái hiện tại là Valid
                })
                .ToListAsync();
            // Categories với danh sách sản phẩm
            var categoryProducts = await _db.Categories
                .Select(c => new {
                    c.Id,
                    c.Name,
                    Products = c.Products.Select(p => new {
                        p.Id,
                        p.Name,
                        p.CostPrice,
                        p.Quantity,
                        p.Status,
                        p.CreateAt
                    }).ToList()
                })
                .ToListAsync();



            // Gợi ý mở rộng: thêm các mảng dữ liệu khác khi cần (shipping, reviews, tồn kho, ...)

            var obj = new
            {
                Meta = new
                {
                    GeneratedAtUtc = now,
                    Note = "Dữ liệu rút gọn để phục vụ trả lời; có độ trễ theo truy vấn."
                },
                Overview = new
                {
                    ProductCount = productCount,
                    OrdersLast30Days = orderCount30,
                    RevenueLast30Days = revenue30
                },
                StatusCounts = statusCounts,
                TopProductsLast30Days = topProducts30,
                CategoryProductCounts = catCounts,
                TopStoresByRating = topStores,
                ActiveVouchers = activeVouchers
            };

            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}