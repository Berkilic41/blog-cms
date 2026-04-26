using BlogCMS.Business.Services.Interfaces;
using BlogCMS.Data.Repositories.Interfaces;
using BlogCMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BlogCMS.Web.Controllers;

public class HomeController : Controller
{
    private readonly IPostService _posts;
    private readonly ICategoryService _categories;

    public HomeController(IPostService posts, ICategoryService categories)
    {
        _posts = posts;
        _categories = categories;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId, string? tag, int page = 1)
    {
        var result = await _posts.SearchAsync(new PostQuery
        {
            Search = search,
            CategoryId = categoryId,
            TagSlug = tag,
            Status = "Published",
            Page = page,
            PageSize = 6
        });

        var categories = await _categories.GetAllAsync();
        return View(new PostListViewModel
        {
            Posts = result.Posts,
            Total = result.Total,
            Page = result.Page,
            TotalPages = result.TotalPages,
            Search = search,
            CategoryId = categoryId,
            TagSlug = tag,
            Categories = categories,
            PageTitle = tag is not null ? $"Tag: #{tag}"
                       : categoryId.HasValue ? "Filtered posts"
                       : !string.IsNullOrWhiteSpace(search) ? $"Search: {search}"
                       : "Latest posts"
        });
    }

    public IActionResult Privacy() => View();
}
