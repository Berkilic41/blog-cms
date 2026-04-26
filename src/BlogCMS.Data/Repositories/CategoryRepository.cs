using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace BlogCMS.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly DbConnectionFactory _factory;

    public CategoryRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            SELECT c.Id, c.Name, c.Slug, c.Description,
                   (SELECT COUNT(*) FROM Posts p WHERE p.CategoryId = c.Id AND p.Status = 'Published') AS PostCount
            FROM Categories c
            ORDER BY c.Name", conn);
        var list = new List<Category>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(Map(r));
        return list;
    }

    public async Task<Category?> GetByIdAsync(int id) => await SingleAsync("WHERE Id = @Id", ("@Id", id));
    public async Task<Category?> GetBySlugAsync(string slug) => await SingleAsync("WHERE Slug = @Slug", ("@Slug", slug));

    public async Task<int> CreateAsync(Category category)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "INSERT INTO Categories (Name, Slug, Description) OUTPUT INSERTED.Id VALUES (@N, @S, @D)", conn);
        cmd.Parameters.AddWithValue("@N", category.Name);
        cmd.Parameters.AddWithValue("@S", category.Slug);
        cmd.Parameters.AddWithValue("@D", (object?)category.Description ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Category category)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "UPDATE Categories SET Name = @N, Slug = @S, Description = @D WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", category.Id);
        cmd.Parameters.AddWithValue("@N", category.Name);
        cmd.Parameters.AddWithValue("@S", category.Slug);
        cmd.Parameters.AddWithValue("@D", (object?)category.Description ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("DELETE FROM Categories WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM Categories WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    private async Task<Category?> SingleAsync(string where, params (string, object)[] parameters)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand($"SELECT Id, Name, Slug, Description, 0 AS PostCount FROM Categories {where}", conn);
        foreach (var (n, v) in parameters) cmd.Parameters.AddWithValue(n, v);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    private static Category Map(SqlDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        Slug = r.GetString(2),
        Description = r.IsDBNull(3) ? null : r.GetString(3),
        PostCount = r.GetInt32(4)
    };
}
