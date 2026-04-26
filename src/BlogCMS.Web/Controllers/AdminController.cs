using BlogCMS.Business.Services.Interfaces;
using BlogCMS.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogCMS.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserService _users;
    private readonly IPostService _posts;
    private readonly ICommentService _comments;

    public AdminController(IUserService users, IPostService posts, ICommentService comments)
    {
        _users = users;
        _posts = posts;
        _comments = comments;
    }

    public async Task<IActionResult> Index()
    {
        var pending = await _posts.GetPendingAsync();
        var pendingComments = await _comments.GetPendingAsync();
        var users = await _users.GetAllAsync();
        return View(new AdminDashboardViewModel
        {
            UserCount = users.Count(),
            PendingPostCount = pending.Count(),
            PendingCommentCount = pendingComments.Count(),
            RecentPending = pending.Take(5)
        });
    }

    public async Task<IActionResult> Users()
        => View(await _users.GetAllAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(int id, string role)
    {
        try
        {
            await _users.UpdateRoleAsync(id, role);
            TempData["Success"] = "Role updated.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool active)
    {
        await _users.SetActiveAsync(id, active);
        TempData["Success"] = active ? "User activated." : "User disabled.";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> PendingPosts()
        => View(await _posts.GetPendingAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovePost(int id)
    {
        await _posts.ApproveAsync(id);
        TempData["Success"] = "Post approved and published.";
        return RedirectToAction(nameof(PendingPosts));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPost(int id)
    {
        await _posts.RejectAsync(id);
        TempData["Success"] = "Post rejected.";
        return RedirectToAction(nameof(PendingPosts));
    }

    public async Task<IActionResult> Comments()
        => View(await _comments.GetPendingAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveComment(int id)
    {
        await _comments.ApproveAsync(id);
        TempData["Success"] = "Comment approved.";
        return RedirectToAction(nameof(Comments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id)
    {
        await _comments.DeleteAsync(id);
        TempData["Success"] = "Comment deleted.";
        return RedirectToAction(nameof(Comments));
    }
}
