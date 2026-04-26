namespace BlogCMS.Data.Entities;

public class Comment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
    public int? ParentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PostTitle { get; set; }
    public string? PostSlug { get; set; }
    public List<Comment> Replies { get; set; } = [];
}
