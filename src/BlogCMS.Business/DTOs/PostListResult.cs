using BlogCMS.Data.Entities;

namespace BlogCMS.Business.DTOs;

public record PostListResult(
    IEnumerable<Post> Posts,
    int Total,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
