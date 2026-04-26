using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace BlogCMS.Data.Repositories;

public class TagRepository : ITagRepository
{
    private readonly DbConnectionFactory _factory;

    public TagRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT Id, Name, Slug FROM Tags ORDER BY Name", conn);
        var list = new List<Tag>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new Tag { Id = r.GetInt32(0), Name = r.GetString(1), Slug = r.GetString(2) });
        return list;
    }

    public async Task<Tag?> GetBySlugAsync(string slug)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT Id, Name, Slug FROM Tags WHERE Slug = @Slug", conn);
        cmd.Parameters.AddWithValue("@Slug", slug);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new Tag { Id = r.GetInt32(0), Name = r.GetString(1), Slug = r.GetString(2) };
    }

    public async Task<Tag> GetOrCreateAsync(string name)
    {
        var slug = Slugify(name);
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            IF EXISTS (SELECT 1 FROM Tags WHERE Slug = @Slug)
                SELECT Id, Name, Slug FROM Tags WHERE Slug = @Slug
            ELSE
            BEGIN
                INSERT INTO Tags (Name, Slug) VALUES (@Name, @Slug);
                SELECT Id, Name, Slug FROM Tags WHERE Id = SCOPE_IDENTITY();
            END", conn);
        cmd.Parameters.AddWithValue("@Name", name);
        cmd.Parameters.AddWithValue("@Slug", slug);
        using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();
        return new Tag { Id = r.GetInt32(0), Name = r.GetString(1), Slug = r.GetString(2) };
    }

    public async Task SetTagsForPostAsync(int postId, IEnumerable<int> tagIds)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var del = new SqlCommand("DELETE FROM PostTags WHERE PostId = @P", conn, tx))
            {
                del.Parameters.AddWithValue("@P", postId);
                await del.ExecuteNonQueryAsync();
            }
            foreach (var tid in tagIds.Distinct())
            {
                using var ins = new SqlCommand(
                    "INSERT INTO PostTags (PostId, TagId) VALUES (@P, @T)", conn, tx);
                ins.Parameters.AddWithValue("@P", postId);
                ins.Parameters.AddWithValue("@T", tid);
                await ins.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<Tag>> GetForPostAsync(int postId)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            SELECT t.Id, t.Name, t.Slug
            FROM Tags t INNER JOIN PostTags pt ON pt.TagId = t.Id
            WHERE pt.PostId = @P ORDER BY t.Name", conn);
        cmd.Parameters.AddWithValue("@P", postId);
        var list = new List<Tag>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new Tag { Id = r.GetInt32(0), Name = r.GetString(1), Slug = r.GetString(2) });
        return list;
    }

    private static string Slugify(string s)
    {
        s = s.ToLowerInvariant().Trim();
        var chars = s.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
