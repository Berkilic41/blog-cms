namespace BlogCMS.Data.Entities;

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public int AuthorId { get; set; }
    public int CategoryId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    public string? AuthorName { get; set; }
    public string? AuthorDisplay { get; set; }
    public string? AuthorBio { get; set; }
    public string? AuthorAvatar { get; set; }
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
    public int CommentCount { get; set; }
    public int LikeCount { get; set; }
    public List<Tag> Tags { get; set; } = [];
}
