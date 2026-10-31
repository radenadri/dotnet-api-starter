using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using bookism.Data;
using bookism.Dto;
using bookism.Models;

namespace bookism.Services;

/// <summary>
/// Service contract governing authentication, registration, credential verification,
/// and active session user profile introspection with caching.
/// </summary>
public interface IAuthService
{
    Task<(bool Success, string Token, string ErrorMessage, UserProfileDto? Profile)> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<(bool Success, string ErrorMessage)> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<UserProfileDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
    Task InvalidateUserSessionAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Hybrid CQS implementation:
/// - READ: High-performance raw SQL via Dapper with Cache-Aside pattern (ICacheService).
/// - WRITE: Entity Framework Core for entity life-cycle management and constraint checks.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ICacheService _cache;
    private const string UserCachePrefix = "users:";

    public AuthService(AppDbContext db, IConfiguration config, ICacheService cache)
    {
        _db = db;
        _config = config;
        _cache = cache;
    }

    public async Task<(bool Success, string Token, string ErrorMessage, UserProfileDto? Profile)> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();

        const string sql = @"
            SELECT id, username, email, password_hash AS PasswordHash, role, is_active AS IsActive, created_at AS CreatedAt
            FROM users
            WHERE username = @Username
            LIMIT 1;";

        var user = await connection.QueryFirstOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Username = dto.Username }, cancellationToken: ct));

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return (false, string.Empty, "Kredensial tidak valid", null);
        }

        if (!user.IsActive)
        {
            return (false, string.Empty, "Akun pengguna tidak aktif", null);
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        var profile = new UserProfileDto(user.Id, user.Username, user.Email, user.Role, user.IsActive, user.CreatedAt);

        // Pre-warm cache for /me endpoint
        await _cache.SetAsync($"{UserCachePrefix}{user.Id}", profile, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), ct);

        return (true, tokenString, string.Empty, profile);
    }

    /// <summary>
    /// Cache-aside user profile introspection.
    /// Check cache first -> if miss, fetch Dapper -> store in cache.
    /// </summary>
    public async Task<UserProfileDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var cacheKey = $"{UserCachePrefix}{userId}";

        // 1. Check Cache
        var cached = await _cache.GetAsync<UserProfileDto>(cacheKey, ct);
        if (cached != null)
        {
            return cached;
        }

        UserProfileDto? profile = null;

        // 2. Fetch via Dapper (or EF Core fallback for non-relational providers)
        if (_db.Database.IsRelational())
        {
            var connection = _db.Database.GetDbConnection();
            const string sql = @"
                SELECT id AS Id, username AS Username, email AS Email, role AS Role, is_active AS IsActive, created_at AS CreatedAt
                FROM users
                WHERE id = @UserId AND is_active = true
                LIMIT 1;";

            profile = await connection.QueryFirstOrDefaultAsync<UserProfileDto>(
                new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        }
        else
        {
            var u = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
            if (u != null)
            {
                profile = new UserProfileDto(u.Id, u.Username, u.Email, u.Role, u.IsActive, u.CreatedAt);
            }
        }

        if (profile != null)
        {
            // 3. Save to Cache (5m absolute, 1m sliding)
            await _cache.SetAsync(cacheKey, profile, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), ct);
        }

        return profile;
    }

    public async Task InvalidateUserSessionAsync(Guid userId, CancellationToken ct = default)
    {
        await _cache.RemoveAsync($"{UserCachePrefix}{userId}", ct);
    }

    public async Task<(bool Success, string ErrorMessage)> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        int existingCount = 0;
        if (_db.Database.IsRelational())
        {
            var connection = _db.Database.GetDbConnection();
            const string checkSql = "SELECT COUNT(1) FROM users WHERE username = @Username OR (email = @Email AND @Email IS NOT NULL);";
            existingCount = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(checkSql, new { Username = dto.Username, Email = dto.Email }, cancellationToken: ct));
        }
        else
        {
            existingCount = await _db.Users.CountAsync(u => u.Username == dto.Username || (dto.Email != null && u.Email == dto.Email), ct);
        }

        if (existingCount > 0)
        {
            return (false, "Username atau Email sudah digunakan");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = "user",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync(ct);

        return (true, string.Empty);
    }
}
