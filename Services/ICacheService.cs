namespace bookism.Services;

/// <summary>
/// Service abstraction for distributed multi-level caching with automatic failover.
/// Provides standardized serialization, sliding/absolute TTL, and key pattern invalidation.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a deserialized cached item by key.
    /// Returns default(T) on cache miss or server degradation.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Persists an item to cache with absolute and optional sliding expiration.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiry, TimeSpan? slidingExpiry = null, CancellationToken ct = default);

    /// <summary>
    /// Evicts a specific key from all cache tiers.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Evicts all keys matching a specific prefix from all cache tiers.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

