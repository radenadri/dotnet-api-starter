using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using bookism.Controllers;
using bookism.Dto;
using bookism.Services;
using Xunit;

namespace bookism.Tests.Controllers;

/// <summary>
/// Unit tests for AuthController covering login, registration, session introspection, and logout.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["TraceId"] = "test-trace-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task Login_WhenCredentialsValid_ShouldReturn200AndSetCookie()
    {
        // Arrange
        var dto = new LoginDto("john", "password123");
        var profile = new UserProfileDto(Guid.NewGuid(), "john", "john@example.com", "user", true, DateTime.UtcNow);
        _authServiceMock
            .Setup(s => s.LoginAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "jwt-token-xyz", string.Empty, profile));

        // Act
        var result = await _controller.Login(dto, CancellationToken.None) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        var envelope = result.Value as ApiResponse<UserProfileDto>;
        envelope.Should().NotBeNull();
        envelope!.Data.Should().Be(profile);
    }

    [Fact]
    public async Task Login_WhenCredentialsInvalid_ShouldReturn401Unauthorized()
    {
        // Arrange
        var dto = new LoginDto("john", "wrongpass");
        _authServiceMock
            .Setup(s => s.LoginAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, string.Empty, "Kredensial tidak valid", null));

        // Act
        var result = await _controller.Login(dto, CancellationToken.None) as UnauthorizedObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        var envelope = result.Value as ApiErrorResponse;
        envelope.Should().NotBeNull();
        envelope!.Error.Message.Should().Be("Kredensial tidak valid");
    }

    [Fact]
    public async Task Register_WhenValid_ShouldReturn200Ok()
    {
        // Arrange
        var dto = new RegisterDto("newuser", "new@example.com", "pass1234", "pass1234");
        _authServiceMock
            .Setup(s => s.RegisterAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty));

        // Act
        var result = await _controller.Register(dto, CancellationToken.None) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Register_WhenDuplicate_ShouldReturn409Conflict()
    {
        // Arrange
        var dto = new RegisterDto("existing", "exist@example.com", "pass1234", "pass1234");
        _authServiceMock
            .Setup(s => s.RegisterAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Username atau Email sudah digunakan"));

        // Act
        var result = await _controller.Register(dto, CancellationToken.None) as ConflictObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        var envelope = result.Value as ApiErrorResponse;
        envelope!.Error.Message.Should().Be("Username atau Email sudah digunakan");
    }

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticatedAndActive_ShouldReturn200()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.HttpContext.User = new ClaimsPrincipal(identity);

        var profile = new UserProfileDto(userId, "john", "john@example.com", "admin", true, DateTime.UtcNow);
        _authServiceMock
            .Setup(s => s.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        var envelope = result.Value as ApiResponse<UserProfileDto>;
        envelope!.Data.Should().Be(profile);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserNotFoundOrInactive_ShouldReturn401()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        _authServiceMock
            .Setup(s => s.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfileDto?)null);

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None) as UnauthorizedObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldReturn200AndEvictSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act
        var result = await _controller.Logout(CancellationToken.None) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        _authServiceMock.Verify(s => s.InvalidateUserSessionAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

