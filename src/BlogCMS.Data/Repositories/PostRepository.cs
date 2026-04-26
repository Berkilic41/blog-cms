using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BlogCMS.Data.Repositories;

public class PostRepository : IPostRepository
{
    private readonly DbConnectionFactory _factory;

    public PostRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<(IEnumerable<Post> Posts, int Total)> SearchAsync(PostQuery q)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("sp_GetPostsPaged", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@Search",     (object?)q.Search ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CategoryId", (object?)q.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TagSlug",    (object?)q.TagSlug ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status",     (object?)q.Status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AuthorId",   (object?)q.AuthorId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Page",       q.Page);
        cmd.Parameters.AddWithValue("@PageSize",   q.PageSize);

        var posts = new List<Post>();
        var total = 0;
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            posts.Add(new Post
            {
                Id = r.GetInt32(0),
                Title = r.GetString(1),
                Slug = r.GetString(2),
                Excerpt = r.IsDBNull(3) ? null : r.GetString(3),
                CoverImageUrl = r.IsDBNull(4) ? null : r.GetString(4),
                Status = r.GetString(5),
                CreatedAt = r.GetDateTime(6),
                PublishedAt = r.IsDBNull(7) ? null : r.GetDateTime(7),
                AuthorId = r.GetInt32(8),
                AuthorName = r.GetString(9),
                AuthorAvatar = r.IsDBNull(10) ? null : r.GetString(10),
                CategoryId = r.GetInt32(11),
                CategoryName = r.GetString(12),
                CategorySlug = r.GetString(13),
                CommentCount = r.GetInt32(14),
                LikeCount = r.GetInt32(15)
            });
            total = r.GetInt32(16);
        }
        return (posts, total);
    }

    public async Task<Post?> GetBySlugAsync(string slug)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("sp_GetPostBySlug", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@Slug", slug);

        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;

        var post = new Post
        {
            Id = r.GetInt32(0),
            Title = r.GetString(1),
            Slug = r.GetString(2),
            Excerpt = r.IsDBNull(3) ? null : r.GetString(3),
            Content = r.GetString(4),
            CoverImageUrl = r.IsDBNull(5) ? null : r.GetString(5),
            Status = r.GetString(6),
            CreatedAt = r.GetDateTime(7),
            UpdatedAt = r.GetDateTime(8),
            PublishedAt = r.IsDBNull(9) ? null : r.GetDateTime(9),
            AuthorId = r.GetInt32(10),
            AuthorName = r.GetString(11),
            AuthorDisplay = r.IsDBNull(12) ? null : r.GetString(12),
            AuthorBio = r.IsDBNull(13) ? null : r.GetString(13),
            AuthorAvatar = r.IsDBNull(14) ? null : r.GetString(14),
            CategoryId = r.GetInt32(15),
            CategoryName = r.GetString(16),
            CategorySlug = r.GetString(17),
            CommentCount = r.GetInt32(18),
            LikeCount = r.GetInt32(19)
        };

        if (await r.NextResultAsync())
        {
            while (await r.ReadAsync())
                post.Tags.Add(new Tag { Id = r.GetInt32(0), Name = r.GetString(1), Slug = r.GetString(2) });
        }
        return post;
    }

    public async Task<Post?> GetByIdAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            SELECT Id, Title, Slug, Excerpt, Content, CoverImageUrl, AuthorId, CategoryId, Status,
                   CreatedAt, UpdatedAt, PublishedAt
            FROM Posts WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new Post
        {
            Id = r.GetInt32(0), Title = r.GetString(1), Slug = r.GetString(2),
            Excerpt = r.IsDBNull(3) ? null : r.GetString(3),
            Content = r.GetString(4),
            CoverImageUrl = r.IsDBNull(5) ? null : r.GetString(5),
            AuthorId = r.GetInt32(6), CategoryId = r.GetInt32(7), Status = r.GetString(8),
            CreatedAt = r.GetDateTime(9), UpdatedAt = r.GetDateTime(10),
            PublishedAt = r.IsDBNull(11) ? null : r.GetDateTime(11)
        };
    }

    public async Task<int> CreateAsync(Post post)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            INSERT INTO Posts (Title, Slug, Excerpt, Content, CoverImageUrl, AuthorId, CategoryId, Status, PublishedAt)
            OUTPUT INSERTED.Id
            VALUES (@Title, @Slug, @Excerpt, @Content, @Image, @Author, @Category, @Status, @Published)", conn);
        cmd.Parameters.AddWithValue("@Title", post.Title);
        cmd.Parameters.AddWithValue("@Slug", post.Slug);
        cmd.Parameters.AddWithValue("@Excerpt", (object?)post.Excerpt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Content", post.Content);
        cmd.Parameters.AddWithValue("@Image", (object?)post.CoverImageUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Author", post.AuthorId);
        cmd.Parameters.AddWithValue("@Category", post.CategoryId);
        cmd.Parameters.AddWithValue("@Status", post.Status);
        cmd.Parameters.AddWithValue("@Published", (object?)post.PublishedAt ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Post post)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            UPDATE Posts SET
                Title = @Title, Slug = @Slug, Excerpt = @Excerpt, Content = @Content,
                CoverImageUrl = @Image, CategoryId = @Category, Status = @Status,
                UpdatedAt = GETUTCDATE(), PublishedAt = @Published
            WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", post.Id);
        cmd.Parameters.AddWithValue("@Title", post.Title);
        cmd.Parameters.AddWithValue("@Slug", post.Slug);
        cmd.Parameters.AddWithValue("@Excerpt", (object?)post.Excerpt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Content", post.Content);
        cmd.Parameters.AddWithValue("@Image", (object?)post.CoverImageUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Category", post.CategoryId);
        cmd.Parameters.AddWithValue("@Status", post.Status);
        cmd.Parameters.AddWithValue("@Published", (object?)post.PublishedAt ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("DELETE FROM Posts WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateStatusAsync(int id, string status, DateTime? publishedAt)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "UPDATE Posts SET Status = @S, PublishedAt = @P, UpdatedAt = GETUTCDATE() WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@S", status);
        cmd.Parameters.AddWithValue("@P", (object?)publishedAt ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Post>> GetPendingAsync()
    {
        var (posts, _) = await SearchAsync(new PostQuery { Status = "Pending", PageSize = 200 });
        return posts;
    }
}
