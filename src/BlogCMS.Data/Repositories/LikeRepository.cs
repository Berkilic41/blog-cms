using BlogCMS.Data.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace BlogCMS.Data.Repositories;

public class LikeRepository : ILikeRepository
{
    private readonly DbConnectionFactory _factory;

    public LikeRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<bool> ToggleAsync(int postId, int userId)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            IF EXISTS (SELECT 1 FROM Likes WHERE PostId = @P AND UserId = @U)
            BEGIN
                DELETE FROM Likes WHERE PostId = @P AND UserId = @U;
                SELECT 0 AS Liked;
            END
            ELSE
            BEGIN
                INSERT INTO Likes (PostId, UserId) VALUES (@P, @U);
                SELECT 1 AS Liked;
            END", conn);
        cmd.Parameters.AddWithValue("@P", postId);
        cmd.Parameters.AddWithValue("@U", userId);
        return (int)(await cmd.ExecuteScalarAsync())! == 1;
    }

    public async Task<bool> IsLikedAsync(int postId, int userId)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM Likes WHERE PostId = @P AND UserId = @U", conn);
        cmd.Parameters.AddWithValue("@P", postId);
        cmd.Parameters.AddWithValue("@U", userId);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    public async Task<int> CountForPostAsync(int postId)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM Likes WHERE PostId = @P", conn);
        cmd.Parameters.AddWithValue("@P", postId);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }
}
