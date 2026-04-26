using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BlogCMS.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _factory;

    public UserRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<User?> GetByIdAsync(int id)
        => await SingleAsync("SELECT * FROM Users WHERE Id = @Id", ("@Id", id));

    public async Task<User?> GetByEmailAsync(string email)
        => await SingleAsync("SELECT * FROM Users WHERE Email = @Email", ("@Email", email));

    public async Task<User?> GetByUsernameAsync(string username)
        => await SingleAsync("SELECT * FROM Users WHERE Username = @Username", ("@Username", username));

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM Users WHERE Email = @Email", conn);
        cmd.Parameters.AddWithValue("@Email", email);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM Users WHERE Username = @Username", conn);
        cmd.Parameters.AddWithValue("@Username", username);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    public async Task<int> CreateAsync(User user)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, Role, DisplayName, AvatarUrl)
            OUTPUT INSERTED.Id
            VALUES (@Username, @Email, @Hash, @Salt, @Role, @DisplayName, @AvatarUrl)", conn);
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@Hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@Salt", user.PasswordSalt);
        cmd.Parameters.AddWithValue("@Role", user.Role);
        cmd.Parameters.AddWithValue("@DisplayName", (object?)user.DisplayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AvatarUrl", (object?)user.AvatarUrl ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateProfileAsync(int id, string? displayName, string? bio, string? avatarUrl)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "UPDATE Users SET DisplayName = @D, Bio = @B, AvatarUrl = @A WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@D", (object?)displayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@B", (object?)bio ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@A", (object?)avatarUrl ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateRoleAsync(int id, string role)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("UPDATE Users SET Role = @R WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@R", role);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SetActiveAsync(int id, bool active)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("UPDATE Users SET IsActive = @A WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@A", active);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<User>> GetAllWithStatsAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("sp_GetUserList", conn) { CommandType = CommandType.StoredProcedure };
        var users = new List<User>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            users.Add(new User
            {
                Id = r.GetInt32(0),
                Username = r.GetString(1),
                Email = r.GetString(2),
                Role = r.GetString(3),
                DisplayName = r.IsDBNull(4) ? null : r.GetString(4),
                IsActive = r.GetBoolean(5),
                CreatedAt = r.GetDateTime(6),
                PostCount = r.GetInt32(7),
                CommentCount = r.GetInt32(8)
            });
        }
        return users;
    }

    private async Task<User?> SingleAsync(string sql, params (string, object)[] parameters)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(sql, conn);
        foreach (var (name, val) in parameters) cmd.Parameters.AddWithValue(name, val);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new User
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            Username = r.GetString(r.GetOrdinal("Username")),
            Email = r.GetString(r.GetOrdinal("Email")),
            PasswordHash = r.GetString(r.GetOrdinal("PasswordHash")),
            PasswordSalt = r.GetString(r.GetOrdinal("PasswordSalt")),
            Role = r.GetString(r.GetOrdinal("Role")),
            DisplayName = r.IsDBNull(r.GetOrdinal("DisplayName")) ? null : r.GetString(r.GetOrdinal("DisplayName")),
            Bio = r.IsDBNull(r.GetOrdinal("Bio")) ? null : r.GetString(r.GetOrdinal("Bio")),
            AvatarUrl = r.IsDBNull(r.GetOrdinal("AvatarUrl")) ? null : r.GetString(r.GetOrdinal("AvatarUrl")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt"))
        };
    }
}
