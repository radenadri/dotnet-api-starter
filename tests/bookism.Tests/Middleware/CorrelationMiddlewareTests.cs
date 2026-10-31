using FluentAssertions;
using Microsoft.AspNetCore.Http;
using bookism.Services;
using Xunit;

namespace bookism.Tests.Middleware;

/// <summary>
/// Unit tests for CorrelationMiddleware verifying X-Request-ID propagation.
/// </summary>
public class CorrelationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenHeaderMissing_ShouldGenerateNewTraceId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new CorrelationMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items[CorrelationMiddleware.ItemKey].Should().NotBeNull();
        var traceId = CorrelationMiddleware.GetTraceId(context);
        Guid.TryParse(traceId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderPresent_ShouldPropagateTraceId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        const string customId = "custom-correlation-12345";
        context.Request.Headers[CorrelationMiddleware.HeaderKey] = customId;

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new CorrelationMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items[CorrelationMiddleware.ItemKey].Should().Be(customId);
        CorrelationMiddleware.GetTraceId(context).Should().Be(customId);
    }
}

