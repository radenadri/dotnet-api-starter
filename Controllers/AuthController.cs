using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using bookism.Dto;
using bookism.Services;

namespace bookism.Controllers;

/// <summary>
/// Controller governing user authentication, registration, session invalidation, and introspection.
/// Adheres to HTTP semantics (401 Unauthorized, 409 Conflict, 200 OK) with standard envelopes.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    private string TraceId => CorrelationMiddleware.GetTraceId(HttpContext);

    /// <summary>
    /// Authenticates credentials and issues an HttpOnly session cookie containing role claims.
    /// Emits 401 Unauthorized with standard error envelope upon failed verification.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(dto, ct);
        if (!result.Success)
        {
            return Unauthorized(new ApiErrorResponse(new ApiErrorDetail(
                Message: string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Kredensial tidak valid" : result.ErrorMessage,
                TraceId: TraceId
            )));
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddHours(2)
        };

        Response.Cookies.Append("jwt", result.Token, cookieOptions);
        return Ok(ApiResponse<UserProfileDto>.Create("Login berhasil", result.Profile));
    }

    /// <summary>
    /// Registers a new user. Emits 409 Conflict if username or email already exists.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(dto, ct);
        if (!result.Success)
        {
            return Conflict(new ApiErrorResponse(new ApiErrorDetail(
                Message: string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Registrasi gagal" : result.ErrorMessage,
                TraceId: TraceId
            )));
        }

        return Ok(ApiResponse<object>.Create("Registrasi berhasil", null));
    }

    /// <summary>
    /// Introspects the current authenticated user's session and returns profile context via cache-aside.
    /// Emits 401 Unauthorized if the token is invalid, expired, or the user account is inactive.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new ApiErrorResponse(new ApiErrorDetail(
                Message: "Token autentikasi tidak valid",
                TraceId: TraceId
            )));
        }

        var profile = await _auth.GetCurrentUserAsync(userId, ct);
        if (profile == null)
        {
            return Unauthorized(new ApiErrorResponse(new ApiErrorDetail(
                Message: "Akun pengguna tidak ditemukan atau telah dinonaktifkan",
                TraceId: TraceId
            )));
        }

        return Ok(ApiResponse<UserProfileDto>.Create("Profil pengguna berhasil diambil", profile));
    }

    /// <summary>
    /// Clears the authentication session cookie and evicts user cache.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userIdString) && Guid.TryParse(userIdString, out var userId))
        {
            await _auth.InvalidateUserSessionAsync(userId, ct);
        }

        Response.Cookies.Delete("jwt");
        return Ok(ApiResponse<object>.Create("Logout berhasil", null));
    }
}
