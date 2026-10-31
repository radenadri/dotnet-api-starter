using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace bookism.Services;

/// <summary>
/// Production-grade resilient caching service:
/// - L2 Distributed Cache via Redis (primary tier).
/// - L1 In-Memory Cache via IMemoryCache (graceful fallback tier on Redis timeout/failure).
/// Prevents cascading application failures during Redis outages.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly IConnectionMultiplexer? _redisConnection;
    private readonly ILogger<CacheService> _logger;

    // Track keys locally to allow prefix eviction on in-memory tier
    private static readonly ConcurrentDictionary<string, byte> _memoryKeys = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        ILogger<CacheService> logger,
        IConnectionMultiplexer? redisConnection = null)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _logger = logger;
        _redisConnection = redisConnection;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        // 1. Try reading from primary Redis tier
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
            _logger.LogWarning(ex, "Redis cache read failed for key {Key}. Falling back to memory cache.", key);
        }

        // 2. Fallback to L1 memory cache
        if (_memoryCache.TryGetValue(key, out T? memoryValue))
        {
            return memoryValue;
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiry, TimeSpan? slidingExpiry = null, CancellationToken ct = default)
    {
        if (value == null) return;

        // 1. Write to L1 in-memory tier
        var memOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiry
        };
        if (slidingExpiry.HasValue)
        {
            memOptions.SlidingExpiration = slidingExpiry.Value;
        }

        _memoryCache.Set(key, value, memOptions);
        _memoryKeys.TryAdd(key, 0);

        // 2. Write to L2 Redis tier
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
            _logger.LogWarning(ex, "Redis cache write failed for key {Key}. Value preserved in memory cache.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        // Evict from memory
        _memoryCache.Remove(key);
        _memoryKeys.TryRemove(key, out _);

        // Evict from Redis
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
        // Evict matching prefix from memory cache
        var matchingMemKeys = _memoryKeys.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var k in matchingMemKeys)
        {
            _memoryCache.Remove(k);
            _memoryKeys.TryRemove(k, out _);
        }

        // Evict matching prefix from Redis using SCAN
        if (_redisConnection != null && _redisConnection.IsConnected)
        {
            try
            {
                var endpoints = _redisConnection.GetEndPoints();
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
}

