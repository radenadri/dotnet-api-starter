using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace bookism.Services;

/// <summary>
/// Service abstraction for distributed caching backed by Redis.
/// Provides standardized JSON serialization, sliding/absolute TTL, and key pattern invalidation.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a deserialized cached item by key from Redis.
    /// Returns default(T) on cache miss or when Redis fails/timeouts (graceful DB bypass).
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Persists an item to Redis cache with absolute and optional sliding expiration.
    /// Fails gracefully with warning logging if Redis is unavailable.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiry, TimeSpan? slidingExpiry = null, CancellationToken ct = default);

    /// <summary>
    /// Evicts a specific key from Redis cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Evicts all keys matching a specific prefix from Redis cache using non-blocking SCAN.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

/// <summary>
/// Production-grade distributed caching service backed directly by Redis:
/// - Cache-aside pattern: Returns null on Redis degradation/outage, allowing consumer services to query database directly.
/// - Avoids in-memory state desynchronization across multi-replica container instances.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer? _redisConnection;
    private readonly ILogger<CacheService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CacheService(
        IDistributedCache distributedCache,
        ILogger<CacheService> logger,
        IConnectionMultiplexer? redisConnection = null)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _redisConnection = redisConnection;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var bytes = await _distributedCache.GetAsync(key, ct);
            if (bytes != null && bytes.Length > 0)
            {
                return JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache read failed for key {Key}. Bypassing cache to query database directly.", key);
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiry, TimeSpan? slidingExpiry = null, CancellationToken ct = default)
    {
        if (value == null) return;

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
            var distOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiry
            };
            if (slidingExpiry.HasValue)
            {
                distOptions.SlidingExpiration = slidingExpiry.Value;
            }

            await _distributedCache.SetAsync(key, bytes, distOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache write failed for key {Key}. Value was not cached.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache eviction failed for key {Key}.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        if (_redisConnection == null || !_redisConnection.IsConnected)
        {
            _logger.LogWarning("Redis connection is unavailable. Skipping prefix eviction for pattern {Prefix}*.", prefix);
            return;
        }

        try
        {
            var endpoints = _redisConnection.GetEndPoints();
            if (endpoints.Length == 0) return;

            var server = _redisConnection.GetServer(endpoints[0]);
            var db = _redisConnection.GetDatabase();

            var keys = server.Keys(pattern: prefix + "*").ToArray();
            if (keys.Length > 0)
            {
                await db.KeyDeleteAsync(keys);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis prefix eviction failed for pattern {Prefix}*.", prefix);
        }
    }
}
