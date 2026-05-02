using BlogCMS.Business.DTOs;
using BlogCMS.Business.Services;
using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BlogCMS.Tests.Unit.Services;

public class PostServiceTests
{
    private readonly Mock<IPostRepository>     _mockPostRepo;
    private readonly Mock<ITagRepository>      _mockTagRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly PostService               _service;

    public PostServiceTests()
    {
        _mockPostRepo     = new Mock<IPostRepository>();
        _mockTagRepo      = new Mock<ITagRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _service = new PostService(_mockPostRepo.Object, _mockTagRepo.Object, _mockCategoryRepo.Object);
    }

    // ─── Post Lifecycle ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithDraftStatus_CreatesPostInDraft()
    {
        var input = MakeInput(status: "Draft");
        SetupCreate(postId: 1);

        var result = await _service.CreateAsync(authorId: 1, input);

        result.Should().Be(1);
        _mockPostRepo.Verify(p => p.CreateAsync(It.Is<Post>(x => x.Status == "Draft")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonDraftStatus_CreatesPostInPending()
    {
        var input = MakeInput(status: "Pending");
        SetupCreate(postId: 2);

        await _service.CreateAsync(authorId: 1, input);

        _mockPostRepo.Verify(p => p.CreateAsync(It.Is<Post>(x => x.Status == "Pending")), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_SetsStatusPublishedWithTimestamp()
    {
        _mockPostRepo.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(new Post { Id = 1, Status = "Pending" });
        _mockPostRepo.Setup(p => p.UpdateStatusAsync(1, "Published", It.IsAny<DateTime>())).Returns(Task.CompletedTask);

        await _service.ApproveAsync(1);

        _mockPostRepo.Verify(p => p.UpdateStatusAsync(1, "Published", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_PostNotFound_ThrowsKeyNotFoundException()
    {
        _mockPostRepo.Setup(p => p.GetByIdAsync(999)).ReturnsAsync((Post?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ApproveAsync(999));
    }

    [Fact]
    public async Task RejectAsync_SetsStatusRejectedAndClearsPublishedAt()
    {
        _mockPostRepo.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(new Post { Id = 1, Status = "Pending" });
        _mockPostRepo.Setup(p => p.UpdateStatusAsync(1, "Rejected", null)).Returns(Task.CompletedTask);

        await _service.RejectAsync(1);

        _mockPostRepo.Verify(p => p.UpdateStatusAsync(1, "Rejected", null), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_PostNotFound_ThrowsKeyNotFoundException()
    {
        _mockPostRepo.Setup(p => p.GetByIdAsync(999)).ReturnsAsync((Post?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.RejectAsync(999));
    }

    // ─── Authorization ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_AuthorEditingOwnPost_Succeeds()
    {
        var post = new Post { Id = 1, AuthorId = 1, Status = "Draft", CategoryId = 1 };
        SetupUpdate(post);

        await _service.UpdateAsync(currentUserId: 1, isAdmin: false, MakeInput(id: 1));

        _mockPostRepo.Verify(p => p.UpdateAsync(It.IsAny<Post>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AuthorEditingOthersPost_ThrowsUnauthorized()
    {
        var post = new Post { Id = 1, AuthorId = 2, Status = "Draft", CategoryId = 1 };
        _mockPostRepo.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(post);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateAsync(currentUserId: 1, isAdmin: false, MakeInput(id: 1)));
    }

    [Fact]
    public async Task UpdateAsync_AdminEditingAnyPost_Succeeds()
    {
        var post = new Post { Id = 1, AuthorId = 99, Status = "Draft", CategoryId = 1 };
        SetupUpdate(post);

        await _service.UpdateAsync(currentUserId: 1, isAdmin: true, MakeInput(id: 1));

        _mockPostRepo.Verify(p => p.UpdateAsync(It.IsAny<Post>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_AuthorDeletingOwnPost_Succeeds()
    {
        _mockPostRepo.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(new Post { Id = 1, AuthorId = 1 });
        _mockPostRepo.Setup(p => p.DeleteAsync(1)).Returns(Task.CompletedTask);

        await _service.DeleteAsync(currentUserId: 1, isAdmin: false, postId: 1);

        _mockPostRepo.Verify(p => p.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_AuthorDeletingOthersPost_ThrowsUnauthorized()
    {
        _mockPostRepo.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(new Post { Id = 1, AuthorId = 99 });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.DeleteAsync(currentUserId: 1, isAdmin: false, postId: 1));
    }

    [Fact]
    public async Task DeleteAsync_AdminDeletingAnyPost_Succeeds()
    {
        _mockPostRepo.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(new Post { Id = 1, AuthorId = 99 });
        _mockPostRepo.Setup(p => p.DeleteAsync(1)).Returns(Task.CompletedTask);

        await _service.DeleteAsync(currentUserId: 1, isAdmin: true, postId: 1);

        _mockPostRepo.Verify(p => p.DeleteAsync(1), Times.Once);
    }

    // ─── Published Post Re-Editing ────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_AuthorEditingPublishedPost_RevertsToPending()
    {
        var publishedAt = DateTime.UtcNow.AddDays(-1);
        var post = new Post { Id = 1, AuthorId = 1, Status = "Published", PublishedAt = publishedAt, CategoryId = 1 };
        SetupUpdate(post);

        await _service.UpdateAsync(currentUserId: 1, isAdmin: false, MakeInput(id: 1));

        _mockPostRepo.Verify(p => p.UpdateAsync(It.Is<Post>(x =>
            x.Status == "Pending" && x.PublishedAt == null
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AdminEditingPublishedPost_KeepsPublished()
    {
        var publishedAt = DateTime.UtcNow.AddDays(-1);
        var post = new Post { Id = 1, AuthorId = 99, Status = "Published", PublishedAt = publishedAt, CategoryId = 1 };
        SetupUpdate(post);

        await _service.UpdateAsync(currentUserId: 1, isAdmin: true, MakeInput(id: 1));

        _mockPostRepo.Verify(p => p.UpdateAsync(It.Is<Post>(x =>
            x.Status == "Published" && x.PublishedAt == publishedAt
        )), Times.Once);
    }

    // ─── Input Validation ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_InvalidCategory_ThrowsInvalidOperation()
    {
        _mockCategoryRepo.Setup(c => c.ExistsAsync(999)).ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(authorId: 1, MakeInput(categoryId: 999)));
    }

    [Fact]
    public async Task UpdateAsync_PostNotFound_ThrowsKeyNotFound()
    {
        _mockPostRepo.Setup(p => p.GetByIdAsync(999)).ReturnsAsync((Post?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(currentUserId: 1, isAdmin: false, MakeInput(id: 999)));
    }

    [Fact]
    public async Task UpdateAsync_NullPostId_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(currentUserId: 1, isAdmin: false, new PostInput { Id = null }));
    }

    [Fact]
    public async Task DeleteAsync_PostNotFound_ThrowsKeyNotFound()
    {
        _mockPostRepo.Setup(p => p.GetByIdAsync(999)).ReturnsAsync((Post?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteAsync(currentUserId: 1, isAdmin: false, postId: 999));
    }

    // ─── Search / Pagination ──────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_ClampsPageBelowOne()
    {
        _mockPostRepo.Setup(p => p.SearchAsync(It.IsAny<PostQuery>()))
            .ReturnsAsync((new List<Post>(), 0));

        await _service.SearchAsync(new PostQuery { Page = 0, PageSize = 10 });

        _mockPostRepo.Verify(p => p.SearchAsync(It.Is<PostQuery>(q => q.Page >= 1)), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ClampsPageSizeAbove50()
    {
        _mockPostRepo.Setup(p => p.SearchAsync(It.IsAny<PostQuery>()))
            .ReturnsAsync((new List<Post>(), 0));

        await _service.SearchAsync(new PostQuery { Page = 1, PageSize = 200 });

        _mockPostRepo.Verify(p => p.SearchAsync(It.Is<PostQuery>(q => q.PageSize <= 50)), Times.Once);
    }

    // ─── Tag Synchronization ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NullTags_SetsEmptyTagList()
    {
        SetupCreate(postId: 1);

        await _service.CreateAsync(authorId: 1, MakeInput(tags: null));

        _mockTagRepo.Verify(t => t.SetTagsForPostAsync(1, It.Is<IEnumerable<int>>(x => !x.Any())), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DeduplicatesTagsCaseInsensitive()
    {
        var tag = new Tag { Id = 1, Name = "csharp" };
        _mockTagRepo.Setup(t => t.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(tag);
        SetupCreate(postId: 1);

        await _service.CreateAsync(authorId: 1, MakeInput(tags: "csharp, CSharp, CSHARP"));

        _mockTagRepo.Verify(t => t.GetOrCreateAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_LimitsTagsToTen()
    {
        var tag = new Tag { Id = 1, Name = "tag" };
        _mockTagRepo.Setup(t => t.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(tag);
        SetupCreate(postId: 1);
        var fifteenTags = string.Join(", ", Enumerable.Range(1, 15).Select(i => $"tag{i}"));

        await _service.CreateAsync(authorId: 1, MakeInput(tags: fifteenTags));

        _mockTagRepo.Verify(t => t.GetOrCreateAsync(It.IsAny<string>()), Times.Exactly(10));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private PostInput MakeInput(int? id = null, int categoryId = 1, string status = "Draft", string? tags = null) => new()
    {
        Id         = id,
        Title      = "Test Post",
        Content    = "Test content",
        CategoryId = categoryId,
        Status     = status,
        TagsCsv    = tags
    };

    private void SetupCreate(int postId)
    {
        _mockCategoryRepo.Setup(c => c.ExistsAsync(It.IsAny<int>())).ReturnsAsync(true);
        _mockPostRepo.Setup(p => p.CreateAsync(It.IsAny<Post>())).ReturnsAsync(postId);
        _mockTagRepo.Setup(t => t.SetTagsForPostAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupUpdate(Post existing)
    {
        _mockPostRepo.Setup(p => p.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
        _mockCategoryRepo.Setup(c => c.ExistsAsync(It.IsAny<int>())).ReturnsAsync(true);
        _mockPostRepo.Setup(p => p.UpdateAsync(It.IsAny<Post>())).Returns(Task.CompletedTask);
        _mockTagRepo.Setup(t => t.SetTagsForPostAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
            .Returns(Task.CompletedTask);
    }
}
