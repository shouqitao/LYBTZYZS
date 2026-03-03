using System.Security.Claims;
using FluentAssertions;
using LYBT.Shared.Logging.Management;
using LYBT.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Serilog.Events;
using Xunit;

namespace LYBT.Tests.Unit.WebAPI.Controllers;

/// <summary>
/// DiagnosticsController单元测试
/// refactor-logging-system: Task 4.9
/// </summary>
public class DiagnosticsControllerTests : IDisposable
{
    private readonly ILogger<DiagnosticsController> _logger;
    private readonly LoggingLevelManager _loggingLevelManager;
    private readonly DiagnosticsController _controller;

    public DiagnosticsControllerTests()
    {
        _logger = Substitute.For<ILogger<DiagnosticsController>>();
        _loggingLevelManager = new LoggingLevelManager(LogEventLevel.Information);
        _controller = new DiagnosticsController(_loggingLevelManager, _logger);

        // 设置HttpContext以支持GetOperator()
        SetupControllerContext();
    }

    private void SetupControllerContext()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "TestAdmin"),
            new Claim(ClaimTypes.Role, "SuperAdmin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    public void Dispose()
    {
        _loggingLevelManager.Dispose();
    }

    #region GetLoggingStatus Tests

    [Fact]
    public void GetLoggingStatus_ShouldReturnOkWithCurrentStatus()
    {
        // Act
        var result = _controller.GetLoggingStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var status = okResult.Value;

        status.Should().NotBeNull();

        // 使用反射检查匿名对象属性
        var statusType = status!.GetType();
        statusType.GetProperty("currentLevel")!.GetValue(status).Should().Be("Information");
        statusType.GetProperty("defaultLevel")!.GetValue(status).Should().Be("Information");
        statusType.GetProperty("isDebugModeActive")!.GetValue(status).Should().Be(false);
    }

    [Fact]
    public void GetLoggingStatus_WhenDebugModeActive_ShouldShowActiveStatus()
    {
        // Arrange
        _loggingLevelManager.EnableDebugMode(LogEventLevel.Debug, 30);

        // Act
        var result = _controller.GetLoggingStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var status = okResult.Value;

        var statusType = status!.GetType();
        statusType.GetProperty("currentLevel")!.GetValue(status).Should().Be("Debug");
        statusType.GetProperty("isDebugModeActive")!.GetValue(status).Should().Be(true);
        statusType.GetProperty("debugModeExpiresAt")!.GetValue(status).Should().NotBeNull();
    }

    #endregion

    #region EnableDebugMode Tests

    [Fact]
    public void EnableDebugMode_WithDefaultRequest_ShouldEnableDebugLevel()
    {
        // Arrange
        var request = new EnableDebugModeRequest();

        // Act
        var result = _controller.EnableDebugMode(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var responseType = response!.GetType();
        responseType.GetProperty("message")!.GetValue(response).Should().Be("调试模式已启用");
        responseType.GetProperty("currentLevel")!.GetValue(response).Should().Be("Debug");
        responseType.GetProperty("previousLevel")!.GetValue(response).Should().Be("Information");
    }

    [Fact]
    public void EnableDebugMode_WithVerboseLevel_ShouldEnableVerboseLevel()
    {
        // Arrange
        var request = new EnableDebugModeRequest { Level = "verbose" };

        // Act
        var result = _controller.EnableDebugMode(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var responseType = response!.GetType();
        responseType.GetProperty("currentLevel")!.GetValue(response).Should().Be("Verbose");
    }

    [Fact]
    public void EnableDebugMode_WithDurationExceedingMax_ShouldCapAt120Minutes()
    {
        // Arrange
        var request = new EnableDebugModeRequest { DurationMinutes = 200 };

        // Act
        var result = _controller.EnableDebugMode(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var responseType = response!.GetType();
        var durationMinutes = (int?)responseType.GetProperty("durationMinutes")!.GetValue(response);
        durationMinutes.Should().Be(120);
    }

    [Fact]
    public void EnableDebugMode_WithNullRequest_ShouldUseDefaults()
    {
        // Act
        var result = _controller.EnableDebugMode(null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var responseType = response!.GetType();
        responseType.GetProperty("currentLevel")!.GetValue(response).Should().Be("Debug");
        var durationMinutes = (int?)responseType.GetProperty("durationMinutes")!.GetValue(response);
        durationMinutes.Should().Be(30);
    }

    #endregion

    #region DisableDebugMode Tests

    [Fact]
    public void DisableDebugMode_WhenDebugModeActive_ShouldRestoreDefaultLevel()
    {
        // Arrange
        _loggingLevelManager.EnableDebugMode(LogEventLevel.Debug, 30);

        // Act
        var result = _controller.DisableDebugMode();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var responseType = response!.GetType();
        responseType.GetProperty("message")!.GetValue(response).Should().Be("调试模式已禁用，已恢复默认日志级别");
        responseType.GetProperty("currentLevel")!.GetValue(response).Should().Be("Information");
        responseType.GetProperty("previousLevel")!.GetValue(response).Should().Be("Debug");
    }

    [Fact]
    public void DisableDebugMode_WhenNotInDebugMode_ShouldStillReturnOk()
    {
        // Act
        var result = _controller.DisableDebugMode();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var responseType = response!.GetType();
        responseType.GetProperty("currentLevel")!.GetValue(response).Should().Be("Information");
    }

    #endregion

    #region SetLoggingLevel Tests

    [Fact]
    public void SetLoggingLevel_WithValidLevel_ShouldUpdateLevel()
    {
        // Arrange
        var request = new SetLoggingLevelRequest { Level = "Warning" };

        // Act
        var result = _controller.SetLoggingLevel(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var responseType = response!.GetType();
        responseType.GetProperty("message")!.GetValue(response).Should().Be("日志级别已更新");
        responseType.GetProperty("currentLevel")!.GetValue(response).Should().Be("Warning");
        responseType.GetProperty("previousLevel")!.GetValue(response).Should().Be("Information");
    }

    [Fact]
    public void SetLoggingLevel_WithEmptyLevel_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SetLoggingLevelRequest { Level = "" };

        // Act
        var result = _controller.SetLoggingLevel(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value;

        var errorType = error!.GetType();
        errorType.GetProperty("error")!.GetValue(error).Should().Be("日志级别不能为空");
    }

    [Fact]
    public void SetLoggingLevel_WithWhitespaceLevel_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SetLoggingLevelRequest { Level = "   " };

        // Act
        var result = _controller.SetLoggingLevel(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void SetLoggingLevel_WithInvalidLevel_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SetLoggingLevelRequest { Level = "InvalidLevel" };

        // Act
        var result = _controller.SetLoggingLevel(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value;

        var errorType = error!.GetType();
        errorType.GetProperty("error")!.GetValue(error).Should().Be("无效的日志级别");
        errorType.GetProperty("validLevels")!.GetValue(error).Should().NotBeNull();
    }

    [Theory]
    [InlineData("verbose", "Verbose")]
    [InlineData("debug", "Debug")]
    [InlineData("information", "Information")]
    [InlineData("warning", "Warning")]
    [InlineData("error", "Error")]
    [InlineData("fatal", "Fatal")]
    public void SetLoggingLevel_WithCaseInsensitiveLevel_ShouldUpdateCorrectly(string input, string expected)
    {
        // Arrange
        var request = new SetLoggingLevelRequest { Level = input };

        // Act
        var result = _controller.SetLoggingLevel(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var responseType = response!.GetType();
        responseType.GetProperty("currentLevel")!.GetValue(response).Should().Be(expected);
    }

    #endregion

    #region Logging Verification Tests

    [Fact]
    public void EnableDebugMode_ShouldLogWarning()
    {
        // Arrange
        var request = new EnableDebugModeRequest { Level = "debug", DurationMinutes = 30 };

        // Act
        _controller.EnableDebugMode(request);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("调试模式已启用")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void DisableDebugMode_ShouldLogWarning()
    {
        // Arrange
        _loggingLevelManager.EnableDebugMode(LogEventLevel.Debug, 30);

        // Act
        _controller.DisableDebugMode();

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("调试模式已禁用")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void SetLoggingLevel_ShouldLogWarning()
    {
        // Arrange
        var request = new SetLoggingLevelRequest { Level = "Warning" };

        // Act
        _controller.SetLoggingLevel(request);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("日志级别已手动更改")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}
