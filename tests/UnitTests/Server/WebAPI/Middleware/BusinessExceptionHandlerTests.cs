using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.ExceptionHandling.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.WebAPI.Tests.Middleware;

/// <summary>
/// BusinessExceptionHandler单元测试
/// refactor-logging-system: Task 4.2
/// </summary>
public class BusinessExceptionHandlerTests
{
    private readonly ILogger<BusinessExceptionHandler> _logger;
    private readonly BusinessExceptionHandler _handler;

    public BusinessExceptionHandlerTests()
    {
        _logger = Substitute.For<ILogger<BusinessExceptionHandler>>();
        _handler = new BusinessExceptionHandler(_logger);
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsTrue_ForValidationException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new ValidationException("字段验证失败", "TestField");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsTrue_ForNotFoundException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new NotFoundException("Patient", "123");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsTrue_ForBusinessException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new BusinessException("业务规则违反", "BR001");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsFalse_ForNonAppException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var exception = new InvalidOperationException("普通异常");

        // Act
        var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryHandleAsync_LogsWarning_ForBusinessException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new BusinessException("测试业务异常");

        // Act
        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert - 验证日志被调用
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
