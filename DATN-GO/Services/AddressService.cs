using DATN_GO.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DATN_GO.Services
{
    public class AddressService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://localhost:7096/api/addresses";

        public AddressService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Lấy tất cả địa chỉ
        public async Task<List<Addresses>> GetAddressesAsync()
        {
            var response = await _httpClient.GetAsync(_apiUrl);

            if (!response.IsSuccessStatusCode)
                return new List<Addresses>();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Addresses>>(content);
        }

        // Lấy địa chỉ theo ID
        public async Task<Addresses?> GetAddressByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_apiUrl}/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Addresses>(content);
        }

        // Thêm địa chỉ
        public async Task<(bool Success, string? ErrorMessage)> AddAddressAsync(Addresses model)
        {
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return (true, null);

            Debug.WriteLine("📡 API lỗi:");
            Debug.WriteLine(responseContent);


            try
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string[]>>>(responseContent);
                if (parsed != null && parsed.ContainsKey("errors"))
                {
                    var errors = parsed["errors"]
                        .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                        .ToList();
                    return (false, string.Join(" | ", errors));
                }
            }
            catch
            {
            }

            return (false, responseContent);
        }



        public async Task<bool> UpdateAddressAsync(Addresses address)
        {
            try
            {
                var json = JsonConvert.SerializeObject(address);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_apiUrl}/{address.Id}", content);

                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"👉 PUT Response Status: {response.StatusCode}");
                Console.WriteLine($"👉 PUT Response Body: {responseBody}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 EXCEPTION: {ex.Message}");
                return false;
            }
        }



        // Xoá địa chỉ
        public async Task<bool> DeleteAddressAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_apiUrl}/{id}");
            return response.IsSuccessStatusCode;
        }

        // Thêm địa chỉ và trả về ID
        public async Task<(bool Success, string ErrorMessage, int Id)> AddAddressAndReturnIdAsync(Addresses model)
        {
            try
            {
                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var created = JsonConvert.DeserializeObject<Addresses>(responseContent);
                        if (created != null && created.Id > 0)
                            return (true, "", created.Id);
                        else
                            return (false, "Không nhận được ID hợp lệ từ API sau khi tạo.", 0);
                    }
                    catch (Exception ex)
                    {
                        return (false, $"❌ JSON parse lỗi: {ex.Message}", 0);
                    }
                }

                try
                {
                    var parsed = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string[]>>>(responseContent);
                    if (parsed != null && parsed.ContainsKey("errors"))
                    {
                        var errors = parsed["errors"]
                            .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                            .ToList();
                        return (false, string.Join(" | ", errors), 0);
                    }
                }
                catch
                {

                }

                return (false, $"❌ API Error: {response.StatusCode} - {responseContent}", 0);
            }
            catch (Exception ex)
            {
                return (false, $"💥 Exception: {ex.Message}", 0);
            }
        }

    }
}