using BlogCMS.Data.Entities;

namespace BlogCMS.Business.Services.Interfaces;

public interface ITagService
{
    Task<IEnumerable<Tag>> GetAllAsync();
}
