using DATN_GO.Models;

namespace DATN_GO.ViewModels
{
    public class PostArticleViewModel
    {
        public Users CurrentUser { get; set; }
        public Posts Post { get; set; } = new Posts();
        public List<Posts> PostList { get; set; } = new List<Posts>();
    }
}
