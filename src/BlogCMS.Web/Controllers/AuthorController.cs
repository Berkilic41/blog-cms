using System.Security.Claims;
using BlogCMS.Business.Services.Interfaces;
using BlogCMS.Data.Repositories.Interfaces;
using BlogCMS.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogCMS.Web.Controllers;

[Authorize(Roles = "Admin,Author")]
public class AuthorController : Controller
{
    private readonly IPostService _posts;
    private readonly ICategoryService _categories;
    private readonly ITagService _tags;

    public AuthorController(IPostService posts, ICategoryService categories, ITagService tags)
    {
        _posts = posts;
        _categories = categories;
        _tags = tags;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var result = await _posts.SearchAsync(new PostQuery
        {
            AuthorId = CurrentUserId,
            Status = null,
            Page = page,
            PageSize = 20
        });
        return View(new PostListViewModel
        {
            Posts = result.Posts,
            Total = result.Total,
            Page = result.Page,
            TotalPages = result.TotalPages,
            PageTitle = "My posts"
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View("Form", new PostFormViewModel
        {
            Categories = await _categories.GetAllAsync(),
            Status = "Pending",
            CurrentStatus = "New"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PostFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _categories.GetAllAsync();
            return View("Form", vm);
        }

        try
        {
            await _posts.CreateAsync(CurrentUserId, vm.ToInput());
            TempData["Success"] = "Post created. It is pending review.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            vm.Categories = await _categories.GetAllAsync();
            return View("Form", vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _posts.GetByIdAsync(id);
        if (post is null) return NotFound();
        if (!User.IsInRole("Admin") && post.AuthorId != CurrentUserId) return Forbid();

        var tags = await new BlogCMS.Data.Repositories.TagRepository(
            HttpContext.RequestServices.GetRequiredService<BlogCMS.Data.DbConnectionFactory>()
        ).GetForPostAsync(id);

        return View("Form", new PostFormViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Excerpt = post.Excerpt,
            Content = post.Content,
            CoverImageUrl = post.CoverImageUrl,
            CategoryId = post.CategoryId,
            Status = post.Status,
            CurrentStatus = post.Status,
            TagsCsv = string.Join(", ", tags.Select(t => t.Name)),
            Categories = await _categories.GetAllAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PostFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _categories.GetAllAsync();
            return View("Form", vm);
        }

        try
        {
            await _posts.UpdateAsync(CurrentUserId, User.IsInRole("Admin"), vm.ToInput());
            TempData["Success"] = "Post updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            vm.Categories = await _categories.GetAllAsync();
            return View("Form", vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _posts.DeleteAsync(CurrentUserId, User.IsInRole("Admin"), id);
            TempData["Success"] = "Post deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
