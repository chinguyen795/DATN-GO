namespace DATN_API.ViewModels
{
    public class AdminStorelViewModels
    {
        // Thông tin cơ bản cửa hàng
        public int Id { get; set; }
        public string Name { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
        public string Avatar { get; set; }
        public string CoverPhoto { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        public string BankAccount { get; set; }
        public string AccountHolder { get; set; }
        public string BankName { get; set; }
        // Danh sách sản phẩm
        public List<StoreProductViewModel> Products { get; set; } = new();

        // Danh sách đơn hàng
        public List<StoreOrderViewModel> Orders { get; set; } = new();
    }

    public class StoreProductViewModel
    {
        public int Id { get; set; }
        public string Image { get; set; }       // Ảnh sản phẩm
        public string Name { get; set; }
        public decimal? Price { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int Stock { get; set; }
        public string Status { get; set; }      // Active / Inactive
        public string Category { get; set; }    // Danh mục
    }

    public class StoreOrderViewModel
    {
        public int Id { get; set; }             // Mã đơn hàng
        public string CustomerName { get; set; }
        public string Status { get; set; }      // Trạng thái
        public DateTime CreateAt { get; set; }  // Ngày tạo
        public decimal TotalAmount { get; set; }


    }


}
