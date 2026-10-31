namespace bookism.Dto;

/// <summary>
/// Metadata envelope containing ISO-8601 timestamp and API contract version
/// to provide consumers with runtime traceability.
/// </summary>
public record ApiMeta(string Timestamp, string Version);

/// <summary>
/// Pagination metadata embedded within the data payload for collection endpoints
/// to prevent unbounded dataset transmission and enable standard pagination mechanics.
/// </summary>
public record PaginationMeta(int Page, int Limit, int TotalPages, int TotalData);

/// <summary>
/// Standard data envelope for collection responses containing the list of items
/// alongside pagination details.
/// </summary>
public record PagedData<T>(IEnumerable<T> Items, PaginationMeta Pagination);

/// <summary>
/// Unified success response envelope conforming to organizational API standards.
/// </summary>
public record ApiResponse<T>(string Message, T? Data, ApiMeta Meta)
{
    /// <summary>
    /// Helper factory method to construct a standard successful response envelope with current timestamp and default version.
    /// </summary>
    public static ApiResponse<T> Create(string message, T? data, string version = "1.0.0") =>
        new(message, data, new ApiMeta(DateTime.UtcNow.ToString("o"), version));
}

/// <summary>
/// Detail of an individual field constraint or semantic business validation failure.
/// </summary>
public record ValidationErrorDetail(string Field, string Issue);

/// <summary>
/// Structured error payload containing consumer-facing explanation, distributed trace identifier,
/// and optional validation breakdown.
/// </summary>
public record ApiErrorDetail(string Message, string TraceId, IEnumerable<ValidationErrorDetail>? Details = null);

/// <summary>
/// Unified error envelope to guarantee consistent client error consumption without leaking internal stack traces.
/// </summary>
public record ApiErrorResponse(ApiErrorDetail Error);

/// <summary>
/// Health check response envelope adhering to organizational service observability standards.
/// </summary>
public record HealthResponse(
    string Status,
    string Service,
    string Version,
    string Timestamp,
    Dictionary<string, string> Checks
);
