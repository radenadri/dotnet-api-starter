using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StackExchange.Redis;
using bookism.Controllers;
using bookism.Data;
using bookism.Dto;
using Xunit;

namespace bookism.Tests.Controllers;

/// <summary>
/// Unit tests for HealthController covering liveness, readiness, startup, and dependencies probes.
/// </summary>
public class HealthControllerTests
{
    private readonly AppDbContext _dbContext;

    public HealthControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);
    }

    [Fact]
    public void GetLive_ShouldReturnOkWithUpStatus()
    {
        // Arrange
        var controller = new HealthController(_dbContext);

        // Act
        var result = controller.GetLive() as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        var health = result.Value as HealthResponse;
        health.Should().NotBeNull();
        health!.Status.Should().Be("UP");
        health.Checks.Should().BeEmpty();
    }

    [Fact]
    public void GetStartup_ShouldReturnOkWithUpStatus()
    {
        // Arrange
        var controller = new HealthController(_dbContext);

        // Act
        var result = controller.GetStartup() as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        var health = result.Value as HealthResponse;
        health.Should().NotBeNull();
        health!.Status.Should().Be("UP");
    }

    [Fact]
    public async Task GetReady_WhenDbHealthyAndRedisHealthy_ShouldReturn200()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mockRedis.Setup(r => r.IsConnected).Returns(true);
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);
        mockDb.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(5));

        var controller = new HealthController(_dbContext, mockRedis.Object);

        // Act
        var result = await controller.GetReady() as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        var response = result.Value as HealthResponse;
        response!.Status.Should().Be("UP");
        response.Checks["database"].Should().Be("UP");
        response.Checks["redis"].Should().Be("UP");
    }

    [Fact]
    public async Task GetReady_WhenDbHealthyAndRedisFails_ShouldReturn206Degraded()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(r => r.IsConnected).Returns(false);

        var controller = new HealthController(_dbContext, mockRedis.Object);

        // Act
        var result = await controller.GetReady() as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status206PartialContent);
        var response = result.Value as HealthResponse;
        response!.Status.Should().Be("DEGRADED");
        response.Checks["database"].Should().Be("UP");
        response.Checks["redis"].Should().Be("DEGRADED");
    }

    [Fact]
    public async Task GetDependencies_WhenBothHealthy_ShouldReturn200()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mockRedis.Setup(r => r.IsConnected).Returns(true);
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);
        mockDb.Setup(d => d.PingAsync(It.IsAny<CommandFlags>())).ReturnsAsync(TimeSpan.FromMilliseconds(5));

        var controller = new HealthController(_dbContext, mockRedis.Object);

        // Act
        var result = await controller.GetDependencies() as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);
        var response = result.Value as HealthResponse;
        response!.Status.Should().Be("UP");
    }

    [Fact]
    public async Task GetDependencies_WhenRedisDegraded_ShouldReturn206()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(r => r.IsConnected).Returns(false);

        var controller = new HealthController(_dbContext, mockRedis.Object);

        // Act
        var result = await controller.GetDependencies() as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status206PartialContent);
        var response = result.Value as HealthResponse;
        response!.Status.Should().Be("DEGRADED");
    }
}
