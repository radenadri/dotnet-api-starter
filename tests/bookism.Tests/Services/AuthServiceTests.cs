using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using bookism.Data;
using bookism.Dto;
using bookism.Models;
using bookism.Services;
using Xunit;

namespace bookism.Tests.Services;

/// <summary>
/// Unit tests for AuthService covering registration, caching, and session invalidation.
/// </summary>
public class AuthServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns("very_secret_key_1234567890_test_key_12345");
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("BookshelfAPI");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("BookshelfClient");

        _cacheMock = new Mock<ICacheService>();
        _service = new AuthService(_db, _configMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WhenNewUser_ShouldPersistUser()
    {
        // Arrange
        var dto = new RegisterDto("unique_user", "unique@example.com", "Secret123!", "Secret123!");

        // Act
        var (success, errorMessage) = await _service.RegisterAsync(dto);

        // Assert
        success.Should().BeTrue();
        errorMessage.Should().BeEmpty();

        var inDb = await _db.Users.FirstOrDefaultAsync(u => u.Username == "unique_user");
        inDb.Should().NotBeNull();
        inDb!.Email.Should().Be("unique@example.com");
        inDb.Role.Should().Be("user");
        BCrypt.Net.BCrypt.Verify("Secret123!", inDb.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WhenUsernameAlreadyExists_ShouldReturnError()
    {
        // Arrange
        var user = new User { Username = "existing_user", Email = "old@example.com" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var dto = new RegisterDto("existing_user", "new@example.com", "Pass123!", "Pass123!");

        // Act
        var (success, errorMessage) = await _service.RegisterAsync(dto);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().Be("Username atau Email sudah digunakan");
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenCached_ShouldReturnFromCacheWithoutDb()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cached = new UserProfileDto(userId, "cached_user", "c@example.com", "user", true, DateTime.UtcNow);
        _cacheMock
            .Setup(c => c.GetAsync<UserProfileDto>($"users:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        // Act
        var result = await _service.GetCurrentUserAsync(userId);

        // Assert
        result.Should().Be(cached);
    }

    [Fact]
    public async Task InvalidateUserSessionAsync_ShouldCallCacheRemove()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.InvalidateUserSessionAsync(userId);

        // Assert
        _cacheMock.Verify(c => c.RemoveAsync($"users:{userId}", It.IsAny<CancellationToken>()), Times.Once);
    }
}

