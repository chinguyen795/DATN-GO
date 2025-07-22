
using DATN_GO.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace DATN_GO.Services
{
    public class VariantCompositionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public VariantCompositionService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] + "/api/VariantComposition";
        }

        public async Task<List<VariantComposition>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync(_baseUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<VariantComposition>>(content);
        }

        public async Task<VariantComposition?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/{id}");
            if (!response.IsSuccessStatusCode) return null;
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<VariantComposition>(content);
        }

        public async Task<List<VariantComposition>> GetByProductVariantIdAsync(int productVariantId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/product-variant/{productVariantId}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<VariantComposition>>(content);
        }

        public async Task<bool> AddMultipleAsync(int productId, int productVariantId, List<(int VariantId, int VariantValueId)> pairs)
        {
            var dto = new
            {
                ProductId = productId,
                ProductVariantId = productVariantId,
                Pairs = pairs.Select(p => new { VariantId = p.VariantId, VariantValueId = p.VariantValueId }).ToList()
            };

            var json = JsonConvert.SerializeObject(dto);
            var response = await _httpClient.PostAsync($"{_baseUrl}/add-multiple", new StringContent(json, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(VariantComposition model)
        {
            var json = JsonConvert.SerializeObject(model);
            var response = await _httpClient.PutAsync($"{_baseUrl}/{model.Id}", new StringContent(json, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
