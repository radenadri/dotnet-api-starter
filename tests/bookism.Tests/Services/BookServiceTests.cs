using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using bookism.Data;
using bookism.Dto;
using bookism.Models;
using bookism.Services;
using Xunit;

namespace bookism.Tests.Services;

/// <summary>
/// Unit tests for BookService write commands and cache eviction using EF Core InMemory database.
/// </summary>
public class BookServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly BookService _service;

    public BookServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _cacheMock = new Mock<ICacheService>();
        _service = new BookService(_db, _cacheMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_WhenCached_ShouldReturnCachedList()
    {
        // Arrange
        var cachedEntry = new BookListCacheEntry(new List<BookDetailDto>
        {
            new(1, "Cached Book", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, new(), new(), null, null)
        }, 1);

        _cacheMock
            .Setup(c => c.GetAsync<BookListCacheEntry>("books:list:p1:l10:sall", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedEntry);

        // Act
        var (items, total) = await _service.GetAllAsync(1, 10, null);

        // Assert
        items.Should().HaveCount(1);
        total.Should().Be(1);
        items.First().Name.Should().Be("Cached Book");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCached_ShouldReturnCachedBook()
    {
        // Arrange
        var cached = new BookDetailDto(99, "Cached Single", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, new(), new(), null, null);
        _cacheMock
            .Setup(c => c.GetAsync<BookDetailDto>("books:99", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        // Act
        var result = await _service.GetByIdAsync(99);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Cached Single");
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistBookAndInvalidateListCache()
    {
        // Arrange
        var author = new Author { Name = "Robert Martin" };
        var category = new Category { Name = "Software" };
        _db.Authors.Add(author);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var dto = new BookCreateDto(
            Name: "Clean Architecture",
            Summary: "Architecture guide",
            Image: null,
            Language: "EN",
            Isbn10: null,
            Isbn13: null,
            TotalPages: 350,
            ReleaseDate: 2017,
            Rating: 4.8m,
            Status: "Read",
            Progress: 100,
            CurrentPage: null,
            PublisherId: null,
            FormatId: null,
            AuthorIds: new List<int> { author.Id },
            CategoryIds: new List<int> { category.Id }
        );

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Clean Architecture");
        _cacheMock.Verify(c => c.RemoveByPrefixAsync("books:list:", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_ShouldMutateAndEvictCache()
    {
        // Arrange
        var book = new Book { Name = "Initial Title", Rating = 3.0m };
        _db.Books.Add(book);
        await _db.SaveChangesAsync();

        var updateDto = new BookUpdateDto(
            Name: "Updated Title",
            Summary: null,
            Image: null,
            Language: null,
            Isbn10: null,
            Isbn13: null,
            TotalPages: null,
            ReleaseDate: null,
            Rating: 4.5m,
            Status: null,
            Progress: null,
            CurrentPage: null,
            PublisherId: null,
            FormatId: null,
            AuthorIds: null,
            CategoryIds: null
        );

        // Act
        var result = await _service.UpdateAsync(book.Id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Title");
        _cacheMock.Verify(c => c.RemoveAsync($"books:{book.Id}", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefixAsync("books:list:", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ShouldReturnNull()
    {
        // Act
        var result = await _service.UpdateAsync(999, new BookUpdateDto("Non-existent", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_ShouldSoftDeleteAndEvictCache()
    {
        // Arrange
        var book = new Book { Name = "To Delete" };
        _db.Books.Add(book);
        await _db.SaveChangesAsync();

        // Act
        var ok = await _service.DeleteAsync(book.Id);

        // Assert
        ok.Should().BeTrue();
        book.DeletedAt.Should().NotBeNull();
        _cacheMock.Verify(c => c.RemoveAsync($"books:{book.Id}", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefixAsync("books:list:", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ShouldReturnFalse()
    {
        // Act
        var ok = await _service.DeleteAsync(999);

        // Assert
        ok.Should().BeFalse();
    }
}
