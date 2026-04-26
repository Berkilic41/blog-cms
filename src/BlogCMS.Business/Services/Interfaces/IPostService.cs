using BlogCMS.Business.DTOs;
using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;

namespace BlogCMS.Business.Services.Interfaces;

public interface IPostService
{
    Task<PostListResult> SearchAsync(PostQuery query);
    Task<Post?> GetBySlugAsync(string slug);
    Task<Post?> GetByIdAsync(int id);
    Task<int> CreateAsync(int authorId, PostInput input);
    Task UpdateAsync(int currentUserId, bool isAdmin, PostInput input);
    Task DeleteAsync(int currentUserId, bool isAdmin, int postId);
    Task<IEnumerable<Post>> GetPendingAsync();
    Task ApproveAsync(int postId);
    Task RejectAsync(int postId);
}
