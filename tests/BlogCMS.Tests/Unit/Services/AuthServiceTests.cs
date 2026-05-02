using BlogCMS.Business.Services;
using BlogCMS.Data.Entities;
using BlogCMS.Data.Repositories.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BlogCMS.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly AuthService           _service;

    public AuthServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _service      = new AuthService(_mockUserRepo.Object);
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsSuccessWithReaderRole()
    {
        _mockUserRepo.Setup(u => u.ExistsByEmailAsync("new@test.com")).ReturnsAsync(false);
        _mockUserRepo.Setup(u => u.ExistsByUsernameAsync("newuser")).ReturnsAsync(false);
        _mockUserRepo.Setup(u => u.CreateAsync(It.IsAny<User>())).ReturnsAsync(1);

        var result = await _service.RegisterAsync("newuser", "new@test.com", "Pass123!");

        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Role.Should().Be("Reader");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFail()
    {
        _mockUserRepo.Setup(u => u.ExistsByEmailAsync("taken@test.com")).ReturnsAsync(true);

        var result = await _service.RegisterAsync("user", "taken@test.com", "Pass123!");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Email already in use");
        _mockUserRepo.Verify(u => u.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ReturnsFail()
    {
        _mockUserRepo.Setup(u => u.ExistsByEmailAsync("new@test.com")).ReturnsAsync(false);
        _mockUserRepo.Setup(u => u.ExistsByUsernameAsync("taken")).ReturnsAsync(true);

        var result = await _service.RegisterAsync("taken", "new@test.com", "Pass123!");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Username already taken");
        _mockUserRepo.Verify(u => u.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ReturnsFail()
    {
        _mockUserRepo.Setup(u => u.GetByEmailAsync("ghost@test.com")).ReturnsAsync((User?)null);

        var result = await _service.LoginAsync("ghost@test.com", "Pass123!");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_InactiveAccount_ReturnsFail()
    {
        _mockUserRepo.Setup(u => u.GetByEmailAsync("user@test.com"))
            .ReturnsAsync(new User { Id = 1, Email = "user@test.com", IsActive = false });

        var result = await _service.LoginAsync("user@test.com", "Pass123!");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("account is disabled");
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsUser()
    {
        _mockUserRepo.Setup(u => u.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, Username = "author1" });

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Username.Should().Be("author1");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        _mockUserRepo.Setup(u => u.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }
}
