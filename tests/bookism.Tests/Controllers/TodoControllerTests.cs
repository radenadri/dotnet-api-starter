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
/// Unit tests for TodoController covering GetAll, GetById, Create, Update, and Delete.
/// </summary>
public class TodoControllerTests
{
    private readonly Mock<ITodoService> _todoServiceMock;
    private readonly TodoController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public TodoControllerTests()
    {
        _todoServiceMock = new Mock<ITodoService>();
        _controller = new TodoController(_todoServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["TraceId"] = "trace-123";
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetAll_ShouldReturn200WithPagedData()
    {
        // Arrange
        var items = new List<TodoResponseDto> { new(1, "Task 1", false), new(2, "Task 2", true) };
        _todoServiceMock
            .Setup(s => s.GetAllAsync(_userId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 2));

        // Act
        var result = await _controller.GetAll(1, 10) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        var envelope = result.Value as ApiResponse<PagedData<TodoResponseDto>>;
        envelope!.Data!.Items.Should().HaveCount(2);
        envelope.Data.Pagination.TotalData.Should().Be(2);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturn200()
    {
        // Arrange
        var item = new TodoResponseDto(10, "Specific Task", false);
        _todoServiceMock
            .Setup(s => s.GetByIdAsync(10, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        var result = await _controller.GetById(10) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        var envelope = result.Value as ApiResponse<TodoResponseDto>;
        envelope!.Data.Should().Be(item);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        _todoServiceMock
            .Setup(s => s.GetByIdAsync(999, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TodoResponseDto?)null);

        // Act
        var result = await _controller.GetById(999) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturn201CreatedAtAction()
    {
        // Arrange
        var createDto = new TodoCreateDto("New Task");
        var created = new TodoResponseDto(1, "New Task", false);
        _todoServiceMock
            .Setup(s => s.CreateAsync(createDto, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.Create(createDto) as CreatedAtActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.ActionName.Should().Be(nameof(_controller.GetById));
    }

    [Fact]
    public async Task Update_WhenFound_ShouldReturn200()
    {
        // Arrange
        var updateDto = new TodoUpdateDto("Updated Task", true);
        var updated = new TodoResponseDto(1, "Updated Task", true);
        _todoServiceMock
            .Setup(s => s.UpdateAsync(1, updateDto, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        // Act
        var result = await _controller.Update(1, updateDto) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Update_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var updateDto = new TodoUpdateDto("Updated Task", true);
        _todoServiceMock
            .Setup(s => s.UpdateAsync(1, updateDto, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TodoResponseDto?)null);

        // Act
        var result = await _controller.Update(1, updateDto) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Delete_WhenFound_ShouldReturn200()
    {
        // Arrange
        _todoServiceMock
            .Setup(s => s.DeleteAsync(1, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        _todoServiceMock
            .Setup(s => s.DeleteAsync(1, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(1) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}

