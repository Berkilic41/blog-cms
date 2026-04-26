using BlogCMS.Data.Entities;

namespace BlogCMS.Data.Repositories.Interfaces;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetForPostAsync(int postId, bool includePending = false);
    Task<int> CreateAsync(Comment comment);
    Task<Comment?> GetByIdAsync(int id);
    Task ApproveAsync(int id);
    Task DeleteAsync(int id);
    Task<IEnumerable<Comment>> GetPendingAsync();
}
