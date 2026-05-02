using BlogCMS.Business.Services;
using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BlogCMS.Tests.Unit.Services;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _repo;
    private readonly CommentService           _service;

    public CommentServiceTests()
    {
        _repo    = new Mock<ICommentRepository>();
        _service = new CommentService(_repo.Object);
    }

    // ─── Validation ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NullContent_ThrowsInvalidOperation()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(1, 1, null, null!));
        ex.Message.Should().Contain("required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WhitespaceContent_ThrowsInvalidOperation(string content)
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(1, 1, null, content));
        ex.Message.Should().Contain("required");
    }

    [Fact]
    public async Task CreateAsync_ContentTooLong_ThrowsInvalidOperation()
    {
        var longContent = new string('x', 2001);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(1, 1, null, longContent));
        ex.Message.Should().Contain("too long");
    }

    [Fact]
    public async Task CreateAsync_Exactly2000Chars_Succeeds()
    {
        var content = new string('a', 2000);
        var saved   = new Comment { Id = 1, PostId = 1, Content = content, IsApproved = true };
        _repo.Setup(r => r.CreateAsync(It.IsAny<Comment>())).ReturnsAsync(1);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(saved);

        var result = await _service.CreateAsync(1, 1, null, content);

        result.Should().NotBeNull();
    }

    // ─── Content Normalization ────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ContentTrimmed()
    {
        var saved = new Comment { Id = 1, Content = "Hello world", IsApproved = true };
        _repo.Setup(r => r.CreateAsync(It.IsAny<Comment>())).ReturnsAsync(1);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(saved);

        await _service.CreateAsync(1, 1, null, "  Hello world  ");

        _repo.Verify(r => r.CreateAsync(It.Is<Comment>(c => c.Content == "Hello world")), Times.Once);
    }

    // ─── Auto-Approval ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AutoApproved()
    {
        var saved = new Comment { Id = 1, Content = "Test", IsApproved = true };
        _repo.Setup(r => r.CreateAsync(It.IsAny<Comment>())).ReturnsAsync(1);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(saved);

        await _service.CreateAsync(1, 1, null, "Test comment");

        _repo.Verify(r => r.CreateAsync(It.Is<Comment>(c => c.IsApproved == true)), Times.Once);
    }

    // ─── Field Mapping ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_FieldsMappedCorrectly()
    {
        Comment? captured = null;
        _repo.Setup(r => r.CreateAsync(It.IsAny<Comment>()))
             .Callback<Comment>(c => captured = c)
             .ReturnsAsync(5);
        _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Comment { Id = 5 });

        await _service.CreateAsync(postId: 10, userId: 20, parentId: 3, content: "Reply");

        captured.Should().NotBeNull();
        captured!.PostId.Should().Be(10);
        captured.UserId.Should().Be(20);
        captured.ParentId.Should().Be(3);
    }

    [Fact]
    public async Task CreateAsync_NoParent_ParentIdIsNull()
    {
        Comment? captured = null;
        _repo.Setup(r => r.CreateAsync(It.IsAny<Comment>()))
             .Callback<Comment>(c => captured = c)
             .ReturnsAsync(1);
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Comment { Id = 1 });

        await _service.CreateAsync(1, 1, null, "Top-level");

        captured!.ParentId.Should().BeNull();
    }

    // ─── Returns Saved Comment ────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsSavedCommentWithId()
    {
        var saved = new Comment { Id = 99, PostId = 1, Content = "Test", IsApproved = true };
        _repo.Setup(r => r.CreateAsync(It.IsAny<Comment>())).ReturnsAsync(99);
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(saved);

        var result = await _service.CreateAsync(1, 1, null, "Test");

        result.Id.Should().Be(99);
    }

    // ─── Delegation Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetForPostAsync_DelegatesToRepo()
    {
        var comments = new[] { new Comment { Id = 1 }, new Comment { Id = 2 } };
        _repo.Setup(r => r.GetForPostAsync(5, false)).ReturnsAsync(comments);

        var result = await _service.GetForPostAsync(5);

        result.Should().HaveCount(2);
        _repo.Verify(r => r.GetForPostAsync(5, false), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_DelegatesToRepo()
    {
        _repo.Setup(r => r.ApproveAsync(3)).Returns(Task.CompletedTask);

        await _service.ApproveAsync(3);

        _repo.Verify(r => r.ApproveAsync(3), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToRepo()
    {
        _repo.Setup(r => r.DeleteAsync(7)).Returns(Task.CompletedTask);

        await _service.DeleteAsync(7);

        _repo.Verify(r => r.DeleteAsync(7), Times.Once);
    }

    [Fact]
    public async Task GetPendingAsync_DelegatesToRepo()
    {
        _repo.Setup(r => r.GetPendingAsync()).ReturnsAsync([]);

        await _service.GetPendingAsync();

        _repo.Verify(r => r.GetPendingAsync(), Times.Once);
    }
}
