namespace BlogCMS.Business.DTOs;

public class PostInput
{
    public int? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string? TagsCsv { get; set; }
    public string Status { get; set; } = "Pending";
}
