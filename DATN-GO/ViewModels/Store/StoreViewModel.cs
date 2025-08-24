using System.ComponentModel.DataAnnotations;
using DATN_GO.Models;

namespace DATN_GO.ViewModels.Store
{
    public class StoreViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string? RepresentativeName { get; set; }
        public string? Address { get; set; }
        public float Longitude { get; set; }
        public float Latitude { get; set; }
        public string? Avatar { get; set; }
        public StoreStatus Status { get; set; }
        public string? Slug { get; set; }
        public string? CoverPhoto { get; set; }
        public string? BankAccount { get; set; }
        public string? Bank { get; set; }
        public string? BankAccountOwner { get; set; }

        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }

        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public int TotalProductQuantity { get; set; }
        public int TotalSoldProducts { get; set; }
    }
}