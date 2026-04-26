using BlogCMS.Business.DTOs;
using BlogCMS.Data.Entities;

namespace BlogCMS.Business.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string username, string email, string password);
    Task<AuthResult> LoginAsync(string email, string password);
    Task<User?> GetByIdAsync(int id);
}
