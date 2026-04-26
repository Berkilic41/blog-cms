using BlogCMS.Data.Entities;

namespace BlogCMS.Business.Services.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetBySlugAsync(string slug);
    Task<Category?> GetByIdAsync(int id);
}
