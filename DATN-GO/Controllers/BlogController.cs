using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;

namespace DATN_GO.Controllers
{
    public class BlogController : Controller
    {
        private readonly BlogService _blogService;

        public BlogController(BlogService blogService)
        {
            _blogService = blogService;
        }

        public async Task<IActionResult> Blog(string? search, string? category, string? sort, string? time)
        {
            var posts = await _blogService.GetAllPostsAsync();
            posts = _blogService.FilterPosts(posts, search);
            posts = _blogService.FilterByCategory(posts, category);
            posts = _blogService.FilterByTime(posts, time);
            posts = _blogService.SortPosts(posts, sort);
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Sort = sort;
            ViewBag.Time = time;
            return View(posts);
        }
    }
}