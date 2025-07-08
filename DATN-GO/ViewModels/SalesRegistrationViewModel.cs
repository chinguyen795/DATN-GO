using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DATN_GO.ViewModels
{
    public class SalesRegistrationViewModel
    {
        public IFormFile? Avatar { get; set; }
        public IFormFile? CoverPhoto { get; set; }

        public string? CitizenIdentityCard { get; set; }
        public string? RepresentativeName { get; set; }
        public string? Address { get; set; }
        public string? Name { get; set; } // Tên cửa hàng

        public string? BankAccount { get; set; }
        public string? Bank { get; set; }
        public string? BankAccountOwner { get; set; }
    }

}
