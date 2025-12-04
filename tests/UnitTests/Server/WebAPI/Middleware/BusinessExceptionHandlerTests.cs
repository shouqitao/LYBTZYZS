using LYBT.Shared.Models.Exceptions;
using LYBT.WebAPI.ExceptionHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.WebAPI.Tests.Middleware;

/// <summary>
/// BusinessExceptionHandler单元测试
/// refactor-logging-system: Task 4.2
/// </summary>
public class BusinessExceptionHandlerTests
{
    private readonly Mock<ILogger<BusinessExceptionHandler>> _loggerMock;
    private readonly BusinessExceptionHandler _handler;

    public BusinessExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<BusinessExceptionHandler>>();
        _handler = new BusinessExceptionHandler(_loggerMock.Object);
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
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
