using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using bookism.Services;
using Xunit;

namespace bookism.Tests.Services;

/// <summary>
/// Unit tests for CacheService covering Redis operations and graceful degradation to null.
/// </summary>
public class CacheServiceTests
{
    private readonly Mock<IDistributedCache> _distributedCacheMock;
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        _distributedCacheMock = new Mock<IDistributedCache>();
        _cacheService = new CacheService(
            _distributedCacheMock.Object,
            NullLogger<CacheService>.Instance,
            null
        );
    }

    [Fact]
    public async Task GetAsync_WhenDistributedCacheReturnsData_ShouldDeserialize()
    {
        // Arrange
        var key = "test-key";
        var sample = new TestPayload("value123");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(sample);
        _distributedCacheMock
            .Setup(d => d.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        // Act
        var result = await _cacheService.GetAsync<TestPayload>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().Be("value123");
    }

    [Fact]
    public async Task GetAsync_WhenDistributedCacheThrows_ShouldReturnNullGracefully()
    {
        // Arrange
        var key = "failing-key";
        _distributedCacheMock
            .Setup(d => d.GetAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis timeout"));

        // Act
        var result = await _cacheService.GetAsync<TestPayload>(key);

        // Assert: Returns null so caller bypasses cache and queries database directly
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldWriteToDistributedCache()
    {
        // Arrange
        var key = "set-key";
        var sample = new TestPayload("store-value");

        // Act
        await _cacheService.SetAsync(key, sample, TimeSpan.FromMinutes(5));

        // Assert
        _distributedCacheMock.Verify(d => d.SetAsync(
            key, 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_WhenDistributedCacheThrows_ShouldNotThrowException()
    {
        // Arrange
        var key = "set-failing-key";
        var sample = new TestPayload("store-value");
        _distributedCacheMock
            .Setup(d => d.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis connection dropped"));

        // Act & Assert
        var act = async () => await _cacheService.SetAsync(key, sample, TimeSpan.FromMinutes(5));
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveFromDistributedCache()
    {
        // Arrange
        var key = "remove-key";

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _distributedCacheMock.Verify(d => d.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WhenDistributedCacheThrows_ShouldNotThrowException()
    {
        // Arrange
        var key = "remove-failing-key";
        _distributedCacheMock
            .Setup(d => d.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis error"));

        // Act & Assert
        var act = async () => await _cacheService.RemoveAsync(key);
        await act.Should().NotThrowAsync();
    }

    private record TestPayload(string Data);
}
