using BlogCMS.Business.Services.Interfaces;
using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;

namespace BlogCMS.Business.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _repo;

    public CommentService(ICommentRepository repo) => _repo = repo;

    public Task<IEnumerable<Comment>> GetForPostAsync(int postId) => _repo.GetForPostAsync(postId);

    public async Task<Comment> CreateAsync(int postId, int userId, int? parentId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("Comment content is required.");
        if (content.Length > 2000)
            throw new InvalidOperationException("Comment is too long (max 2000 chars).");

        var comment = new Comment
        {
            PostId = postId,
            UserId = userId,
            ParentId = parentId,
            Content = content.Trim(),
            IsApproved = true // auto-approve for demo; could be false to require moderation
        };
        comment.Id = await _repo.CreateAsync(comment);
        var saved = await _repo.GetByIdAsync(comment.Id);
        return saved!;
    }

    public Task<IEnumerable<Comment>> GetPendingAsync() => _repo.GetPendingAsync();
    public Task ApproveAsync(int id) => _repo.ApproveAsync(id);
    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
}
