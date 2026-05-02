using BlogCMS.Business.DTOs;
using BlogCMS.Business.Helpers;
using BlogCMS.Business.Services.Interfaces;
using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;

namespace BlogCMS.Business.Services;

public class PostService : IPostService
{
    private readonly IPostRepository      _posts;
    private readonly ITagRepository       _tags;
    private readonly ICategoryRepository  _categories;
    private readonly ILogger<PostService> _logger;

    public PostService(IPostRepository posts, ITagRepository tags, ICategoryRepository categories,
        ILogger<PostService> logger)
    {
        _posts      = posts;
        _tags       = tags;
        _categories = categories;
        _logger     = logger;
    }

    public async Task<PostListResult> SearchAsync(PostQuery query)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 50);
        var (posts, total) = await _posts.SearchAsync(query);
        return new PostListResult(posts, total, query.Page, query.PageSize);
    }

    public Task<Post?> GetBySlugAsync(string slug) => _posts.GetBySlugAsync(slug);
    public Task<Post?> GetByIdAsync(int id) => _posts.GetByIdAsync(id);

    public async Task<int> CreateAsync(int authorId, PostInput input)
    {
        if (!await _categories.ExistsAsync(input.CategoryId))
            throw new InvalidOperationException("Selected category does not exist.");

        var slug = await SlugGenerator.GenerateUniqueAsync(input.Title, async s => await _posts.GetBySlugAsync(s) is not null);
        var post = new Post
        {
            Title = input.Title.Trim(),
            Slug = slug,
            Excerpt = string.IsNullOrWhiteSpace(input.Excerpt) ? null : input.Excerpt.Trim(),
            Content = input.Content,
            CoverImageUrl = string.IsNullOrWhiteSpace(input.CoverImageUrl) ? null : input.CoverImageUrl.Trim(),
            AuthorId = authorId,
            CategoryId = input.CategoryId,
            Status = input.Status == "Draft" ? "Draft" : "Pending"
        };
        post.Id = await _posts.CreateAsync(post);
        await SyncTagsAsync(post.Id, input.TagsCsv);
        _logger.LogInformation("Post {PostId} '{Title}' created by author {AuthorId}", post.Id, post.Title, authorId);
        return post.Id;
    }

    public async Task UpdateAsync(int currentUserId, bool isAdmin, PostInput input)
    {
        if (input.Id is null) throw new InvalidOperationException("Post id is required.");
        var existing = await _posts.GetByIdAsync(input.Id.Value)
            ?? throw new KeyNotFoundException("Post not found.");
        if (!isAdmin && existing.AuthorId != currentUserId)
            throw new UnauthorizedAccessException("You can only edit your own posts.");

        if (!await _categories.ExistsAsync(input.CategoryId))
            throw new InvalidOperationException("Selected category does not exist.");

        existing.Title = input.Title.Trim();
        existing.Excerpt = string.IsNullOrWhiteSpace(input.Excerpt) ? null : input.Excerpt.Trim();
        existing.Content = input.Content;
        existing.CoverImageUrl = string.IsNullOrWhiteSpace(input.CoverImageUrl) ? null : input.CoverImageUrl.Trim();
        existing.CategoryId = input.CategoryId;

        // Authors editing their own published posts revert to Pending; admins can keep status.
        if (!isAdmin && existing.Status == "Published")
        {
            existing.Status = "Pending";
            existing.PublishedAt = null;
        }

        await _posts.UpdateAsync(existing);
        await SyncTagsAsync(existing.Id, input.TagsCsv);
    }

    public async Task DeleteAsync(int currentUserId, bool isAdmin, int postId)
    {
        var existing = await _posts.GetByIdAsync(postId)
            ?? throw new KeyNotFoundException("Post not found.");
        if (!isAdmin && existing.AuthorId != currentUserId)
            throw new UnauthorizedAccessException("You can only delete your own posts.");
        await _posts.DeleteAsync(postId);
        _logger.LogInformation("Post {PostId} deleted by user {UserId}", postId, currentUserId);
    }

    public Task<IEnumerable<Post>> GetPendingAsync() => _posts.GetPendingAsync();

    public async Task ApproveAsync(int postId)
    {
        var post = await _posts.GetByIdAsync(postId) ?? throw new KeyNotFoundException("Post not found.");
        await _posts.UpdateStatusAsync(post.Id, "Published", DateTime.UtcNow);
        _logger.LogInformation("Post {PostId} approved and published", postId);
    }

    public async Task RejectAsync(int postId)
    {
        var post = await _posts.GetByIdAsync(postId) ?? throw new KeyNotFoundException("Post not found.");
        await _posts.UpdateStatusAsync(post.Id, "Rejected", null);
        _logger.LogInformation("Post {PostId} rejected", postId);
    }

    private async Task SyncTagsAsync(int postId, string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            await _tags.SetTagsForPostAsync(postId, []);
            return;
        }

        var names = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Where(n => n.Length > 0)
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .Take(10);

        var ids = new List<int>();
        foreach (var name in names)
        {
            var tag = await _tags.GetOrCreateAsync(name);
            ids.Add(tag.Id);
        }
        await _tags.SetTagsForPostAsync(postId, ids);
    }
}
