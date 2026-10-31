using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using bookism.Data;
using bookism.Dto;
using bookism.Services;
using Xunit;

namespace bookism.Tests.Services;

/// <summary>
/// Unit tests for TodoService write operations using EF Core InMemory database.
/// </summary>
public class TodoServiceTests
{
    private readonly AppDbContext _db;
    private readonly TodoService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public TodoServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _service = new TodoService(_db);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistTodo()
    {
        // Arrange
        var dto = new TodoCreateDto("Learn Testing");

        // Act
        var result = await _service.CreateAsync(dto, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Learn Testing");
        result.IsCompleted.Should().BeFalse();

        var inDb = await _db.Todos.FirstOrDefaultAsync(t => t.Id == result.Id);
        inDb.Should().NotBeNull();
        inDb!.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_ShouldUpdateFields()
    {
        // Arrange
        var created = await _service.CreateAsync(new TodoCreateDto("Original"), _userId);

        // Act
        var updated = await _service.UpdateAsync(created.Id, new TodoUpdateDto("Modified", true), _userId);

        // Assert
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Modified");
        updated.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ShouldReturnNull()
    {
        // Act
        var result = await _service.UpdateAsync(999, new TodoUpdateDto("Non-existent", true), _userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_ShouldRemoveRecord()
    {
        // Arrange
        var created = await _service.CreateAsync(new TodoCreateDto("To Delete"), _userId);

        // Act
        var deleted = await _service.DeleteAsync(created.Id, _userId);

        // Assert
        deleted.Should().BeTrue();
        (await _db.Todos.AnyAsync(t => t.Id == created.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ShouldReturnFalse()
    {
        // Act
        var deleted = await _service.DeleteAsync(999, _userId);

        // Assert
        deleted.Should().BeFalse();
    }
}
