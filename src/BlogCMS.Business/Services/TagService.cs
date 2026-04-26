using BlogCMS.Business.Services.Interfaces;
using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;

namespace BlogCMS.Business.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _repo;

    public TagService(ITagRepository repo) => _repo = repo;

    public Task<IEnumerable<Tag>> GetAllAsync() => _repo.GetAllAsync();
}
