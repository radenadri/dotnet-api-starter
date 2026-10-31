using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using bookism.Services;
using Xunit;

namespace bookism.Tests.Services;

/// <summary>
/// Unit tests for CacheService covering fallback mechanics and eviction.
/// </summary>
public class CacheServiceTests
{
    private readonly Mock<IDistributedCache> _distributedCacheMock;
    private readonly IMemoryCache _memoryCache;
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        _distributedCacheMock = new Mock<IDistributedCache>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cacheService = new CacheService(
            _distributedCacheMock.Object,
            _memoryCache,
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
    public async Task GetAsync_WhenDistributedCacheThrows_ShouldFallbackToMemoryCache()
    {
        // Arrange
        var key = "fallback-key";
        var sample = new TestPayload("memory-value");
        _memoryCache.Set(key, sample);

        _distributedCacheMock
            .Setup(d => d.GetAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis timeout"));

        // Act
        var result = await _cacheService.GetAsync<TestPayload>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().Be("memory-value");
    }

    [Fact]
    public async Task SetAsync_ShouldWriteToMemoryAndDistributedCache()
    {
        // Arrange
        var key = "set-key";
        var sample = new TestPayload("store-value");

        // Act
        await _cacheService.SetAsync(key, sample, TimeSpan.FromMinutes(5));

        // Assert
        _memoryCache.TryGetValue(key, out TestPayload? memVal).Should().BeTrue();
        memVal!.Data.Should().Be("store-value");
        _distributedCacheMock.Verify(d => d.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveFromMemoryAndDistributedCache()
    {
        // Arrange
        var key = "remove-key";
        _memoryCache.Set(key, new TestPayload("delete-me"));

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _memoryCache.TryGetValue(key, out _).Should().BeFalse();
        _distributedCacheMock.Verify(d => d.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByPrefixAsync_ShouldEvictMatchingMemoryKeys()
    {
        // Arrange
        await _cacheService.SetAsync("prefix:1", new TestPayload("1"), TimeSpan.FromMinutes(5));
        await _cacheService.SetAsync("prefix:2", new TestPayload("2"), TimeSpan.FromMinutes(5));
        await _cacheService.SetAsync("other:1", new TestPayload("3"), TimeSpan.FromMinutes(5));

        // Act
        await _cacheService.RemoveByPrefixAsync("prefix:");

        // Assert
        _memoryCache.TryGetValue("prefix:1", out _).Should().BeFalse();
        _memoryCache.TryGetValue("prefix:2", out _).Should().BeFalse();
        _memoryCache.TryGetValue("other:1", out _).Should().BeTrue();
    }

    private record TestPayload(string Data);
}

