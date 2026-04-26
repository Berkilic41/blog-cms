using BlogCMS.Data.Entities;

namespace BlogCMS.Data.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByUsernameAsync(string username);
    Task<int> CreateAsync(User user);
    Task UpdateProfileAsync(int id, string? displayName, string? bio, string? avatarUrl);
    Task UpdateRoleAsync(int id, string role);
    Task SetActiveAsync(int id, bool active);
    Task<IEnumerable<User>> GetAllWithStatsAsync();
}
