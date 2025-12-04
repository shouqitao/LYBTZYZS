using LYBT.WebAPI.ExceptionHandlers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.WebAPI.Tests.Middleware;

/// <summary>
/// SystemExceptionHandler单元测试
/// refactor-logging-system: Task 4.2
/// </summary>
public class SystemExceptionHandlerTests
{
    private readonly Mock<ILogger<SystemExceptionHandler>> _loggerMock;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly SystemExceptionHandler _handler;

    public SystemExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<SystemExceptionHandler>>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        _handler = new SystemExceptionHandler(_loggerMock.Object, _environmentMock.Object);
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsTrue_ForAnyException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new InvalidOperationException("测试异常");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TryHandleAsync_Returns500_ForSystemException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new InvalidOperationException("系统异常");

        // Act
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_LogsError_ForSystemException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new InvalidOperationException("系统异常");

        // Act
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task TryHandleAsync_HidesExceptionDetails_InProduction()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var sensitiveMessage = "数据库连接字符串: Server=secret";
        var exception = new InvalidOperationException(sensitiveMessage);

        // Act
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        Assert.DoesNotContain("secret", responseBody);
    }

    [Fact]
    public async Task TryHandleAsync_IncludesCorrelationId_InResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-correlation-id";
        var exception = new InvalidOperationException("测试异常");

        // Act
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        Assert.Contains("correlationId", responseBody);
    }
}
