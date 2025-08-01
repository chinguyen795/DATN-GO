using DATN_GO.Models;
using Microsoft.OpenApi.Extensions;
using System.ComponentModel.DataAnnotations;

namespace DATN_GO.ViewModels.Store
{
    public class StoreAdminViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Avatar { get; set; }
        public string? CoverPhoto { get; set; }
        public string? Address { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Rating { get; set; }
        public string? District { get; set; }  // Add District here

        public StoreStatus Status { get; set; }
        public string? StatusText => Status.GetDisplayName();
        public string? Bank { get; set; }
        public string? BankAccount { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }

        public string? OwnerName { get; set; }
        public string? OwnerEmail { get; set; }
        public List<ProductAdminViewModel> Products { get; set; } = new List<ProductAdminViewModel>();  // Initialize here

        public List<Vouchers> Vouchers { get; set; } = new List<Vouchers>();

    }

}