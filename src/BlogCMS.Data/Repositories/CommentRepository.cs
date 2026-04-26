using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BlogCMS.Data.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly DbConnectionFactory _factory;

    public CommentRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<Comment>> GetForPostAsync(int postId, bool includePending = false)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("sp_GetCommentsForPost", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@PostId", postId);
        cmd.Parameters.AddWithValue("@IncludePending", includePending);

        var flat = new List<Comment>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) flat.Add(MapWithUser(r));

        var byParent = flat.Where(c => c.ParentId.HasValue).GroupBy(c => c.ParentId!.Value).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var c in flat) if (byParent.TryGetValue(c.Id, out var rep)) c.Replies = rep;
        return flat.Where(c => c.ParentId == null).ToList();
    }

    public async Task<int> CreateAsync(Comment comment)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            INSERT INTO Comments (PostId, UserId, ParentId, Content, IsApproved)
            OUTPUT INSERTED.Id
            VALUES (@P, @U, @Pa, @C, @A)", conn);
        cmd.Parameters.AddWithValue("@P", comment.PostId);
        cmd.Parameters.AddWithValue("@U", comment.UserId);
        cmd.Parameters.AddWithValue("@Pa", (object?)comment.ParentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@C", comment.Content);
        cmd.Parameters.AddWithValue("@A", comment.IsApproved);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<Comment?> GetByIdAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            SELECT c.Id, c.PostId, c.UserId, c.ParentId, c.Content, c.IsApproved, c.CreatedAt,
                   u.Username, u.AvatarUrl
            FROM Comments c
            INNER JOIN Users u ON u.Id = c.UserId
            WHERE c.Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapWithUser(r) : null;
    }

    public async Task ApproveAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("UPDATE Comments SET IsApproved = 1 WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("DELETE FROM Comments WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Comment>> GetPendingAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("sp_GetPendingComments", conn) { CommandType = CommandType.StoredProcedure };
        var list = new List<Comment>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var c = MapWithUser(r);
            c.PostTitle = r.GetString(9);
            c.PostSlug = r.GetString(10);
            list.Add(c);
        }
        return list;
    }

    private static Comment MapWithUser(SqlDataReader r) => new()
    {
        Id = r.GetInt32(0),
        PostId = r.GetInt32(1),
        UserId = r.GetInt32(2),
        ParentId = r.IsDBNull(3) ? null : r.GetInt32(3),
        Content = r.GetString(4),
        IsApproved = r.GetBoolean(5),
        CreatedAt = r.GetDateTime(6),
        Username = r.GetString(7),
        AvatarUrl = r.IsDBNull(8) ? null : r.GetString(8)
    };
}
