using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using bookism.Dto;
using bookism.Services;

namespace bookism.Controllers;

/// <summary>
/// Catches unhandled exceptions escaping controller action boundaries to guarantee
/// that consumers receive sanitized, standardized 500 error envelopes without exposing internal state.
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var traceId = CorrelationMiddleware.GetTraceId(context.HttpContext);

        _logger.LogError(context.Exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);

        var errorResponse = new ApiErrorResponse(new ApiErrorDetail(
            Message: "Terjadi kesalahan internal pada server",
            TraceId: traceId
        ));

        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

        context.ExceptionHandled = true;
    }
}

