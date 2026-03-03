using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Unit.WebAPI.Middleware;

/// <summary>
/// CorrelationId中间件单元测试
/// refactor-logging-system: Task 4.1
/// </summary>
public class CorrelationIdMiddlewareTests
{
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<CorrelationIdMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_GeneratesNewCorrelationId_WhenNotProvided()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new CorrelationIdMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.True(context.Items.ContainsKey(CorrelationIdMiddleware.CorrelationIdItemKey));
        var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
        Assert.NotNull(correlationId);
        Assert.NotEmpty(correlationId);
    }

    [Fact]
    public async Task InvokeAsync_UsesExistingCorrelationId_WhenProvided()
    {
        // Arrange
        var expectedCorrelationId = "test-correlation-id-12345";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeader] = expectedCorrelationId;

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new CorrelationIdMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        var storedCorrelationId = context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
        Assert.Equal(expectedCorrelationId, storedCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_StoresCorrelationIdInHttpContext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        string? storedCorrelationId = null;

        RequestDelegate next = ctx =>
        {
            storedCorrelationId = ctx.GetCorrelationId();
            return Task.CompletedTask;
        };
        var middleware = new CorrelationIdMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.NotNull(storedCorrelationId);
        Assert.NotEmpty(storedCorrelationId);
        Assert.NotEqual("N/A", storedCorrelationId);
    }

    [Fact]
    public void GetCorrelationId_ReturnsNA_WhenNotSet()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var correlationId = context.GetCorrelationId();

        // Assert
        Assert.Equal("N/A", correlationId);
    }

    [Fact]
    public void CorrelationIdHeader_HasCorrectValue()
    {
        // Assert
        Assert.Equal("X-Correlation-ID", CorrelationIdMiddleware.CorrelationIdHeader);
    }

    [Fact]
    public async Task InvokeAsync_GeneratesShortCorrelationId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - 验证生成的CorrelationId是12位短格式
        var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
        Assert.NotNull(correlationId);
        Assert.Equal(12, correlationId.Length);
    }
}
