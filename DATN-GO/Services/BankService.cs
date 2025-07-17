using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using DATN_GO.Models;

public class BankService
{
    private readonly HttpClient _httpClient;

    public BankService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<BankModel>> GetBankListAsync()
    {
        var response = await _httpClient.GetAsync("https://api.vietqr.io/v2/banks");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        var root = JsonSerializer.Deserialize<BankApiResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return root?.Data ?? new List<BankModel>();
    }

    private class BankApiResponse
    {
        public List<BankModel> Data { get; set; }
    }
}
