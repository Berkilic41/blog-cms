using BlogCMS.Data.Entities;

namespace BlogCMS.Data.Repositories.Interfaces;

public class PostQuery
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string? TagSlug { get; set; }
    public string? Status { get; set; } = "Published";
    public int? AuthorId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public interface IPostRepository
{
    Task<(IEnumerable<Post> Posts, int Total)> SearchAsync(PostQuery query);
    Task<Post?> GetBySlugAsync(string slug);
    Task<Post?> GetByIdAsync(int id);
    Task<int> CreateAsync(Post post);
    Task UpdateAsync(Post post);
    Task DeleteAsync(int id);
    Task UpdateStatusAsync(int id, string status, DateTime? publishedAt);
    Task<IEnumerable<Post>> GetPendingAsync();
}
