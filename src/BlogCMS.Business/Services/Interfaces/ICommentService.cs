using BlogCMS.Data.Entities;

namespace BlogCMS.Business.Services.Interfaces;

public interface ICommentService
{
    Task<IEnumerable<Comment>> GetForPostAsync(int postId);
    Task<Comment> CreateAsync(int postId, int userId, int? parentId, string content);
    Task<IEnumerable<Comment>> GetPendingAsync();
    Task ApproveAsync(int id);
    Task DeleteAsync(int id);
}
