using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DATN_GO.Models;
using System.Collections.Generic;
using System.Text;

namespace DATN_GO.Services
{
    public class AddressService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://localhost:7096/api/addresses"; // sửa đúng route API

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
        public async Task<bool> AddAddressAsync(Addresses address)
        {
            var json = JsonConvert.SerializeObject(address);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiUrl, content);
            return response.IsSuccessStatusCode;
        }

        // Cập nhật địa chỉ
        public async Task<bool> UpdateAddressAsync(Addresses address)
        {
            var json = JsonConvert.SerializeObject(address);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_apiUrl}/{address.Id}", content);
            return response.IsSuccessStatusCode;
        }

        // Xoá địa chỉ
        public async Task<bool> DeleteAddressAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_apiUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
