using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DATN_GO.ViewModels
{
    public class SalesRegistrationViewModel
    {
        public string? Avatar { get; set; }
        public string? CoverPhoto { get; set; }
        public string? CitizenIdentityCard { get; set; }
        public string? RepresentativeName { get; set; }
        public string? Address { get; set; }
        public string? Name { get; set; }

        public string? BankAccount { get; set; }
        public string? Bank { get; set; }
        public string? BankAccountOwner { get; set; }

        // ===== NEW =====
        public string? Ward { get; set; }
        public string? District { get; set; }
        public string? Province { get; set; }
    }
}