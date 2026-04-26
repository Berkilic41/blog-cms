using BlogCMS.Data.Entities;

namespace BlogCMS.Data.Repositories.Interfaces;

public interface ITagRepository
{
    Task<IEnumerable<Tag>> GetAllAsync();
    Task<Tag?> GetBySlugAsync(string slug);
    Task<Tag> GetOrCreateAsync(string name);
    Task SetTagsForPostAsync(int postId, IEnumerable<int> tagIds);
    Task<IEnumerable<Tag>> GetForPostAsync(int postId);
}
