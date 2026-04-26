using System.Security.Claims;
using BlogCMS.Business.Services.Interfaces;
using BlogCMS.Data.Repositories.Interfaces;
using BlogCMS.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogCMS.Web.Controllers;

public class PostsController : Controller
{
    private readonly IPostService _posts;
    private readonly ICommentService _comments;
    private readonly ILikeService _likes;
    private readonly ILikeRepository _likeRepo;

    public PostsController(IPostService posts, ICommentService comments, ILikeService likes, ILikeRepository likeRepo)
    {
        _posts = posts;
        _comments = comments;
        _likes = likes;
        _likeRepo = likeRepo;
    }

    [HttpGet("/post/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var post = await _posts.GetBySlugAsync(slug);
        if (post is null) return NotFound();
        if (post.Status != "Published" && !User.IsInRole("Admin") && !IsAuthor(post.AuthorId))
            return NotFound();

        var comments = await _comments.GetForPostAsync(post.Id);
        var liked = User.Identity?.IsAuthenticated == true
            ? await _likeRepo.IsLikedAsync(post.Id, CurrentUserId)
            : false;

        return View(new PostDetailsViewModel
        {
            Post = post,
            Comments = comments,
            IsLiked = liked,
            CanComment = User.Identity?.IsAuthenticated == true
        });
    }

    [HttpPost("/posts/{postId:int}/comments")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int postId, string content, int? parentId)
    {
        try
        {
            var comment = await _comments.CreateAsync(postId, CurrentUserId, parentId, content);
            return Json(new
            {
                success = true,
                id = comment.Id,
                username = User.Identity!.Name,
                avatarUrl = User.FindFirstValue("AvatarUrl"),
                content = comment.Content,
                createdAt = comment.CreatedAt.ToString("u"),
                parentId = comment.ParentId
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("/posts/{postId:int}/like")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(int postId)
    {
        var (liked, count) = await _likes.ToggleAsync(postId, CurrentUserId);
        return Json(new { liked, count });
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAuthor(int authorId) =>
        User.Identity?.IsAuthenticated == true && CurrentUserId == authorId;
}
