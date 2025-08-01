using DATN_GO.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class UserManagerController : Controller
    {
        private readonly HttpClient _httpClient;
        public UserManagerController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7096");

        }
        public async Task<IActionResult> UserManager()
        {
            var response = await _httpClient.GetAsync("api/users");
            if (!response.IsSuccessStatusCode)
            {
                return View(new List<Users>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<Users>>(json);
            return View(users);

        }
    }
}