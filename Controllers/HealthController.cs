using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using bookism.Data;
using bookism.Dto;

namespace bookism.Controllers;

/// <summary>
/// Exposes segregated health probe endpoints (liveness, readiness, startup, and dependencies)
/// to govern Kubernetes orchestration and traffic routing decisions accurately.
/// Implements conditional dependency degradation for Redis caching.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConnectionMultiplexer? _redis;
    private const string ServiceName = "bookism";
    private const string ServiceVersion = "1.0.0";

    public HealthController(AppDbContext db, IConnectionMultiplexer? redis = null)
    {
        _db = db;
        _redis = redis;
    }

    /// <summary>
    /// Liveness probe: verifies process execution only.
    /// Strictly forbidden from checking external dependencies to prevent cascade pod restart loops.
    /// </summary>
    [HttpGet("live")]
    public IActionResult GetLive()
    {
        var response = new HealthResponse(
            Status: "UP",
            Service: ServiceName,
            Version: ServiceVersion,
            Timestamp: DateTime.UtcNow.ToString("o"),
            Checks: new Dictionary<string, string>()
        );
        return Ok(response);
    }

    /// <summary>
    /// Readiness probe: determines whether this instance can serve inbound user traffic.
    /// - Database: Critical dependency -> Failure leads to 503 Service Unavailable.
    /// - Redis: Conditional dependency (has in-memory fallback) -> Failure leads to 206 DEGRADED.
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReady()
    {
        var checks = new Dictionary<string, string>();
        bool dbHealthy = false;
        bool redisHealthy = false;

        // 1. Critical Dependency: Primary PostgreSQL Database
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            dbHealthy = await _db.Database.CanConnectAsync(cts.Token);
            checks["database"] = dbHealthy ? "UP" : "DOWN";
        }
        catch
        {
            checks["database"] = "DOWN";
            dbHealthy = false;
        }

        // If primary database is down, service cannot accept traffic -> 503
        if (!dbHealthy)
        {
            var downResponse = new HealthResponse(
                Status: "DOWN",
                Service: ServiceName,
                Version: ServiceVersion,
                Timestamp: DateTime.UtcNow.ToString("o"),
                Checks: checks
            );
            return StatusCode(StatusCodes.Status503ServiceUnavailable, downResponse);
        }

        // 2. Conditional Dependency: Redis Cache
        try
        {
            if (_redis != null && _redis.IsConnected)
            {
                var latency = await _redis.GetDatabase().PingAsync();
                redisHealthy = latency.TotalMilliseconds >= 0;
                checks["redis"] = redisHealthy ? "UP" : "DOWN";
            }
            else
            {
                checks["redis"] = "DEGRADED";
                redisHealthy = false;
            }
        }
        catch
        {
            checks["redis"] = "DEGRADED";
            redisHealthy = false;
        }

        // Determine overall status
        if (redisHealthy)
        {
            var upResponse = new HealthResponse(
                Status: "UP",
                Service: ServiceName,
                Version: ServiceVersion,
                Timestamp: DateTime.UtcNow.ToString("o"),
                Checks: checks
            );
            return Ok(upResponse);
        }
        else
        {
            // Graceful degradation: in-memory cache handles traffic
            var degradedResponse = new HealthResponse(
                Status: "DEGRADED",
                Service: ServiceName,
                Version: ServiceVersion,
                Timestamp: DateTime.UtcNow.ToString("o"),
                Checks: checks
            );
            return StatusCode(StatusCodes.Status206PartialContent, degradedResponse);
        }
    }

    /// <summary>
    /// Startup probe: verifies application warm-up and initialization completion.
    /// </summary>
    [HttpGet("startup")]
    public IActionResult GetStartup()
    {
        var response = new HealthResponse(
            Status: "UP",
            Service: ServiceName,
            Version: ServiceVersion,
            Timestamp: DateTime.UtcNow.ToString("o"),
            Checks: new Dictionary<string, string>()
        );
        return Ok(response);
    }

    /// <summary>
    /// Diagnostics probe: exposes detailed component dependency status for internal observability.
    /// </summary>
    [HttpGet("dependencies")]
    public async Task<IActionResult> GetDependencies()
    {
        var checks = new Dictionary<string, string>();
        bool dbHealthy = false;
        bool redisHealthy = false;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            dbHealthy = await _db.Database.CanConnectAsync(cts.Token);
            checks["database"] = dbHealthy ? "UP" : "DOWN";
        }
        catch
        {
            checks["database"] = "DOWN";
        }

        try
        {
            if (_redis != null && _redis.IsConnected)
            {
                var latency = await _redis.GetDatabase().PingAsync();
                redisHealthy = latency.TotalMilliseconds >= 0;
                checks["redis"] = redisHealthy ? "UP" : "DOWN";
            }
            else
            {
                checks["redis"] = "DEGRADED";
            }
        }
        catch
        {
            checks["redis"] = "DEGRADED";
        }

        string overallStatus = (dbHealthy, redisHealthy) switch
        {
            (true, true) => "UP",
            (true, false) => "DEGRADED",
            _ => "DOWN"
        };

        var response = new HealthResponse(
            Status: overallStatus,
            Service: ServiceName,
            Version: ServiceVersion,
            Timestamp: DateTime.UtcNow.ToString("o"),
            Checks: checks
        );

        return overallStatus switch
        {
            "UP" => Ok(response),
            "DEGRADED" => StatusCode(StatusCodes.Status206PartialContent, response),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, response)
        };
    }
}
