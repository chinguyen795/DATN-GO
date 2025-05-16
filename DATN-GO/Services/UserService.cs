using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DATN_GO.Models;

namespace DATN_GO.Service
{
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public UserService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<List<Users>?> GetUsersAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Users");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Users>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Error fetching users: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<Users?> GetUserByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Users/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Users>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"User with ID {id} not found.");
            }
            else
            {
                Console.WriteLine($"Error fetching user by ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
            return null;
        }

        public async Task<Users?> CreateUserAsync(Users user)
        {
            var content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Users", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Users>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            // Log lỗi
            Console.WriteLine($"Error creating user: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

      
        public async Task<bool> UpdateUserAsync(int id, Users user)
        {
            var content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Users/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            // Log lỗi
            Console.WriteLine($"Error updating user with ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

      
        public async Task<bool> DeleteUserAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Users/{id}");

            if (response.IsSuccessStatusCode) 
            {
                return true;
            }
            Console.WriteLine($"Error deleting user with ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }
    }
}