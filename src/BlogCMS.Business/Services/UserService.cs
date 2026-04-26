using BlogCMS.Business.Services.Interfaces;
using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;

namespace BlogCMS.Business.Services;

public class UserService : IUserService
{
    private static readonly HashSet<string> ValidRoles = ["Admin", "Author", "Reader"];

    private readonly IUserRepository _repo;

    public UserService(IUserRepository repo) => _repo = repo;

    public Task<IEnumerable<User>> GetAllAsync() => _repo.GetAllWithStatsAsync();

    public Task UpdateRoleAsync(int id, string role)
    {
        if (!ValidRoles.Contains(role))
            throw new InvalidOperationException($"Invalid role. Must be one of: {string.Join(", ", ValidRoles)}");
        return _repo.UpdateRoleAsync(id, role);
    }

    public Task SetActiveAsync(int id, bool active) => _repo.SetActiveAsync(id, active);
}
