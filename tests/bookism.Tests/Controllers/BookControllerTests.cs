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
/// Unit tests for BookController covering GetAll, GetById, Create, Update, and Delete.
/// </summary>
public class BookControllerTests
{
    private readonly Mock<IBookService> _bookServiceMock;
    private readonly BookController _controller;

    public BookControllerTests()
    {
        _bookServiceMock = new Mock<IBookService>();
        _controller = new BookController(_bookServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["TraceId"] = "trace-book-123";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetAll_ShouldReturn200WithPagedData()
    {
        // Arrange
        var book = new BookDetailDto(1, "Clean Code", "Summary", null, "EN", null, null, 400, 2008, 4.8m, "Read", 100, null, null, null, null, null, new(), new(), DateTime.UtcNow, DateTime.UtcNow);
        _bookServiceMock
            .Setup(s => s.GetAllAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<BookDetailDto> { book }, 1));

        // Act
        var result = await _controller.GetAll(1, 10, null, CancellationToken.None) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        var envelope = result.Value as ApiResponse<PagedData<BookDetailDto>>;
        envelope!.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturn200()
    {
        // Arrange
        var book = new BookDetailDto(1, "Clean Code", "Summary", null, "EN", null, null, 400, 2008, 4.8m, "Read", 100, null, null, null, null, null, new(), new(), DateTime.UtcNow, DateTime.UtcNow);
        _bookServiceMock
            .Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        var envelope = result.Value as ApiResponse<BookDetailDto>;
        envelope!.Data!.Name.Should().Be("Clean Code");
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        _bookServiceMock
            .Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookDetailDto?)null);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturn201CreatedAtAction()
    {
        // Arrange
        var createDto = new BookCreateDto("DDD", "Summary", null, "EN", null, null, 500, 2003, 4.9m, "Read", 100, null, null, null, null, null);
        var created = new BookDetailDto(2, "DDD", "Summary", null, "EN", null, null, 500, 2003, 4.9m, "Read", 100, null, null, null, null, null, new(), new(), DateTime.UtcNow, DateTime.UtcNow);
        _bookServiceMock
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None) as CreatedAtActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.ActionName.Should().Be(nameof(_controller.GetById));
    }

    [Fact]
    public async Task Update_WhenFound_ShouldReturn200()
    {
        // Arrange
        var updateDto = new BookUpdateDto("Updated Title", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        var updated = new BookDetailDto(1, "Updated Title", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, new(), new(), DateTime.UtcNow, DateTime.UtcNow);
        _bookServiceMock
            .Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Update_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var updateDto = new BookUpdateDto("Updated Title", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        _bookServiceMock
            .Setup(s => s.UpdateAsync(999, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookDetailDto?)null);

        // Act
        var result = await _controller.Update(999, updateDto, CancellationToken.None) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Delete_WhenFound_ShouldReturn200()
    {
        // Arrange
        _bookServiceMock
            .Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Delete_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        _bookServiceMock
            .Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999, CancellationToken.None) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}

