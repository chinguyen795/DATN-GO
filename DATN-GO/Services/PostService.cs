using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DATN_GO.Models;
using System.Configuration;

namespace DATN_GO.Service
{
    public class PostService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public PostService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<List<Posts>?> GetAllPostsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Posts");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Posts>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Error fetching posts: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<Posts?> GetPostByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Posts/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Posts>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Post with ID {id} not found.");
            }
            else
            {
                Console.WriteLine($"Error fetching post by ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
            return null;
        }
        public async Task<Posts?> CreatePostAsync(Posts post)
        {
            var content = new StringContent(JsonSerializer.Serialize(post), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Posts", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Posts>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Error creating post: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<bool> UpdatePostAsync(int id, Posts post)
        {
            var content = new StringContent(JsonSerializer.Serialize(post), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Posts/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Error updating post with ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<bool> DeletePostAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Posts/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Error deleting post with ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }
        public async Task<List<Posts>?> GetPostsByUserIdAsync(int userId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}Posts/user/{userId}");

                // ❌ Xoá dòng này vì không còn cần token
                // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Posts>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                Console.WriteLine($"Lỗi khi lấy bài đăng theo User ID {userId}: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception khi lấy bài đăng: {ex.Message}");
                return null;
            }
        }

    }
}