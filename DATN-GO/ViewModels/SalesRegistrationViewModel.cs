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

        [Required(ErrorMessage = "Vui l?ng nh?p tên c?a hàng.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Vui l?ng nh?p s? tài kho?n.")]
        public string? BankAccount { get; set; }

        [Required(ErrorMessage = "Vui l?ng nh?p tên ch? tài kho?n.")]
        public string? BankAccountOwner { get; set; }

        [Required(ErrorMessage = "Vui l?ng ch?n ngân hàng.")]
        public string? Bank { get; set; }

        [Required(ErrorMessage = "Vui l?ng ch?n t?nh/thành ph?.")]
        public string? Province { get; set; }

        [Required(ErrorMessage = "Vui l?ng ch?n qu?n/huy?n.")]
        public string? District { get; set; }

        [Required(ErrorMessage = "Vui l?ng ch?n phý?ng/x?.")]
        public string? Ward { get; set; }

        [Required(ErrorMessage = "Vui l?ng nh?p ð?a ch? l?y hàng.")]
        public string? PickupAddress { get; set; }
        public decimal? MoneyAmout { get; set; }
    }
}