using BlogCMS.Business.Services.Interfaces;
using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;

namespace BlogCMS.Business.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;

    public CategoryService(ICategoryRepository repo) => _repo = repo;

    public Task<IEnumerable<Category>> GetAllAsync() => _repo.GetAllAsync();
    public Task<Category?> GetBySlugAsync(string slug) => _repo.GetBySlugAsync(slug);
    public Task<Category?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
}
