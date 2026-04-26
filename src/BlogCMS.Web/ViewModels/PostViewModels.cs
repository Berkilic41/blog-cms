using System.ComponentModel.DataAnnotations;
using BlogCMS.Data.Entities;
using BlogCMS.Business.DTOs;

namespace BlogCMS.Web.ViewModels;

public class PostListViewModel
{
    public IEnumerable<Post> Posts { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int Total { get; set; }
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string? TagSlug { get; set; }
    public string? PageTitle { get; set; }
    public IEnumerable<Category> Categories { get; set; } = [];
}

public class PostDetailsViewModel
{
    public Post Post { get; set; } = null!;
    public IEnumerable<Comment> Comments { get; set; } = [];
    public bool IsLiked { get; set; }
    public bool CanComment { get; set; }
}

public class PostFormViewModel
{
    public int? Id { get; set; }
    [Required, MaxLength(250)] public string Title { get; set; } = string.Empty;
    [MaxLength(500)] public string? Excerpt { get; set; }
    [Required] public string Content { get; set; } = string.Empty;
    [MaxLength(500)] public string? CoverImageUrl { get; set; }
    [Required] public int CategoryId { get; set; }
    public string? TagsCsv { get; set; }
    public string Status { get; set; } = "Pending";

    public IEnumerable<Category> Categories { get; set; } = [];
    public string CurrentStatus { get; set; } = "Draft";

    public PostInput ToInput() => new()
    {
        Id = Id,
        Title = Title,
        Excerpt = Excerpt,
        Content = Content,
        CoverImageUrl = CoverImageUrl,
        CategoryId = CategoryId,
        TagsCsv = TagsCsv,
        Status = Status
    };
}
