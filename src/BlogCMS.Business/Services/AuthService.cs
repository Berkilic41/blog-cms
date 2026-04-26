using BlogCMS.Business.DTOs;
using BlogCMS.Business.Helpers;
using BlogCMS.Business.Services.Interfaces;
using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;

namespace BlogCMS.Business.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;

    public AuthService(IUserRepository users) => _users = users;

    public async Task<AuthResult> RegisterAsync(string username, string email, string password)
    {
        if (await _users.ExistsByEmailAsync(email))
            return AuthResult.Fail("Email already in use.");
        if (await _users.ExistsByUsernameAsync(username))
            return AuthResult.Fail("Username already taken.");

        var (hash, salt) = PasswordHasher.Hash(password);
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = "Reader"
        };
        user.Id = await _users.CreateAsync(user);
        return AuthResult.Ok(user);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await _users.GetByEmailAsync(email);
        if (user is null) return AuthResult.Fail("Invalid email or password.");
        if (!user.IsActive) return AuthResult.Fail("This account is disabled.");
        if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
            return AuthResult.Fail("Invalid email or password.");
        return AuthResult.Ok(user);
    }

    public Task<User?> GetByIdAsync(int id) => _users.GetByIdAsync(id);
}
