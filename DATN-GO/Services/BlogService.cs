using DATN_GO.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace DATN_GO.Service
{
    public class BlogService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public BlogService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        // ✅ Lấy tất cả bài viết đã duyệt, parse status từ enum dạng số hoặc chuỗi
        public async Task<List<Posts>> GetAllPostsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Posts");
            if (!response.IsSuccessStatusCode)
                return new List<Posts>();

            var json = await response.Content.ReadAsStringAsync();

            var posts = JsonSerializer.Deserialize<List<Posts>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Posts>();

            // 👉 Lọc Approved
            //posts = posts.Where(p => p.Status == PostStatus.Approved).ToList();

            // ✅ Gắn thêm thông tin User bằng UserId
            foreach (var post in posts)
            {
                post.User = await GetUserByIdAsync(post.UserId);
            }
            return posts;
        }

        public async Task<Categories?> GetCategoryByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Categories/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Categories>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<Users?> GetUserByIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Users/{userId}");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Users>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }


        // ✅ Lấy bài viết theo ID
        public async Task<Posts?> GetPostByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Posts/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var post = JsonSerializer.Deserialize<Posts>(root.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (post != null && root.TryGetProperty("status", out var statusProp))
            {
                if (statusProp.ValueKind == JsonValueKind.Number)
                    post.Status = (PostStatus)statusProp.GetInt32();
                else if (statusProp.ValueKind == JsonValueKind.String &&
                         Enum.TryParse(statusProp.GetString(), true, out PostStatus parsedStatus))
                    post.Status = parsedStatus;
            }

            return post;
        }

        // ✅ Lọc bài viết theo từ khoá (content, tên user...)
        public List<Posts> FilterPosts(List<Posts> posts, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return posts;

            var keyword = search.Trim().ToLower();

            return posts.Where(p =>
                (!string.IsNullOrEmpty(p.Content) && p.Content.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(p.User?.FullName) && p.User.FullName.ToLower().Contains(keyword))
            ).ToList();
        }
        public List<Posts> FilterByCategory(List<Posts> posts, string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return posts;

            var keyword = category.ToLower();

            return posts.Where(p =>
                (!string.IsNullOrEmpty(p.Content) && p.Content.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(p.User?.FullName) && p.User.FullName.ToLower().Contains(keyword))
            ).ToList();
        }

        public List<Posts> FilterByTime(List<Posts> posts, string? time)
        {
            if (string.IsNullOrEmpty(time)) return posts;
            var now = DateTime.Now;

            return time switch
            {
                "today" => posts.Where(p => p.CreateAt.Date == now.Date).ToList(),
                "week" => posts.Where(p => p.CreateAt >= now.AddDays(-7)).ToList(),
                "month" => posts.Where(p => p.CreateAt >= now.AddMonths(-1)).ToList(),
                _ => posts
            };
        }

        public List<Posts> SortPosts(List<Posts> posts, string? sort)
        {
            return sort switch
            {
                "newest" => posts.OrderByDescending(p => p.CreateAt).ToList(),
                "popular" => posts.OrderByDescending(p => p.Content.Length).ToList(), // hoặc view count
                "commented" => posts.OrderByDescending(p => p.Id).ToList(), // nếu chưa có comment count
                _ => posts
            };
        }





    }
}