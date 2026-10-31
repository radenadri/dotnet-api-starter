using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using bookism.Controllers;
using bookism.Dto;
using Xunit;

namespace bookism.Tests.Middleware;

/// <summary>
/// Unit tests for ApiExceptionFilter verifying sanitized 500 error envelopes.
/// </summary>
public class ApiExceptionFilterTests
{
    [Fact]
    public void OnException_ShouldFormat500ApiErrorResponse()
    {
        // Arrange
        var filter = new ApiExceptionFilter(NullLogger<ApiExceptionFilter>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TraceId"] = "trace-err-999";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var exceptionContext = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new InvalidOperationException("Sensitive DB error query failed")
        };

        // Act
        filter.OnException(exceptionContext);

        // Assert
        exceptionContext.ExceptionHandled.Should().BeTrue();
        var result = exceptionContext.Result as ObjectResult;
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var error = result.Value as ApiErrorResponse;
        error.Should().NotBeNull();
        error!.Error.Message.Should().Be("Terjadi kesalahan internal pada server");
        error.Error.TraceId.Should().Be("trace-err-999");
    }
}

