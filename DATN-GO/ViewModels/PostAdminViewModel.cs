namespace DATN_GO.ViewModels
{
    public class PostAdminViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string Title { get; set; } = "";

        public string? Image { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string Status { get; set; }
        public DateTime CreateAt { get; set; }
    }
}