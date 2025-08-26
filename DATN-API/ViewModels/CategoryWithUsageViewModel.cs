namespace DATN_API.ViewModels
{
    public class CategoryWithUsageViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Hashtag { get; set; }
        public string? Image { get; set; }
        public int Status { get; set; }
        public string? Description { get; set; }

        // thêm mới
        public int UsageCount { get; set; }
    }
}
