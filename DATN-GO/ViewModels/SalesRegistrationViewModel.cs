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

        [Required(ErrorMessage = "Vui l?ng nh?p t�n c?a h�ng.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Vui l?ng nh?p s? t�i kho?n.")]
        public string? BankAccount { get; set; }

        [Required(ErrorMessage = "Vui l?ng nh?p t�n ch? t�i kho?n.")]
        public string? BankAccountOwner { get; set; }

        [Required(ErrorMessage = "Vui l?ng ch?n ng�n h�ng.")]
        public string? Bank { get; set; }

        [Required(ErrorMessage = "Vui l?ng ch?n t?nh/th�nh ph?.")]
        public string? Province { get; set; }

        [Required(ErrorMessage = "Vui l?ng ch?n qu?n/huy?n.")]
        public string? District { get; set; }

        [Required(ErrorMessage = "Vui l?ng ch?n ph�?ng/x?.")]
        public string? Ward { get; set; }

        [Required(ErrorMessage = "Vui l?ng nh?p �?a ch? l?y h�ng.")]
        public string? PickupAddress { get; set; }
        public decimal? MoneyAmout { get; set; }
    }
}