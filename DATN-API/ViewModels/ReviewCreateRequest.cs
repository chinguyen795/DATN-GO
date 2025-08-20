namespace DATN_API.ViewModels
{
    public class ReviewCreateRequest
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int OrderId { get; set; }
        public int Rating { get; set; }
        public string CommentText { get; set; }
        public List<string>? MediaList { get; set; }
    }

    public class ReviewViewModel
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }
        public int Rating { get; set; }
        public string? CommentText { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<string>? MediaUrls { get; set; }
        public int OrderId { get; set; }

    }
}