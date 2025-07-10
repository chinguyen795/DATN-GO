namespace DATN_GO.ViewModels
{
    public class StoreProfileViewModel
    {
        // Từ bảng Users
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? UserAvatar { get; set; }
        public string Phone { get; set; }

        // Từ bảng Stores
        public string StoreName { get; set; }
        public string Address { get; set; }
        public DateTime? CreateAt { get; set; }
        public string Avatar { get; set; }
        public string CoverImage { get; set; }
        public string Bank { get; set; }
        public string BankAccount { get; set; }
    }
}
