using FluentAssertions;
using LYBT.Shared.Models.Exceptions;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace LYBT.WebAPI.UnitTests.Middleware;

/// <summary>
/// GlobalExceptionHandler 中间件测试
/// </summary>
public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly GlobalExceptionHandler _handler;
    private readonly DefaultHttpContext _httpContext;

    public GlobalExceptionHandlerTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionHandler>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        _handler = new GlobalExceptionHandler(_mockLogger.Object);

        // 设置HttpContext
        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Path = "/api/test";
        _httpContext.Request.Method = "GET";
        _httpContext.TraceIdentifier = "test-trace-id";
        _httpContext.Response.Body = new MemoryStream();

        // 设置服务提供者
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_mockEnvironment.Object);
        _httpContext.RequestServices = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task TryHandleAsync_ApiException_ShouldReturnCorrectProblemDetails()
    {
        // Arrange
        var apiException = new ApiException("API调用失败")
        {
            StatusCode = HttpStatusCode.BadRequest,
            ErrorCode = "API_ERROR",
            ResponseContent = "Error content",
            UserMessage = "用户友好的错误消息"
        };

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, apiException, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(400);

        // 验证响应内容
        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("API调用异常");
        problemDetails.Detail.Should().Be("用户友好的错误消息");
        problemDetails.Instance.Should().Be("/api/test");

        // 验证扩展信息
        problemDetails.Extensions.Should().ContainKey("traceId");
        problemDetails.Extensions.Should().ContainKey("timestamp");
        problemDetails.Extensions.Should().ContainKey("requestMethod");
        problemDetails.Extensions.Should().ContainKey("errorCode");
        problemDetails.Extensions.Should().ContainKey("responseContent");

        problemDetails.Extensions["errorCode"].Should().Be("API_ERROR");
        problemDetails.Extensions["responseContent"].Should().Be("Error content");
    }

    [Fact]
    public async Task TryHandleAsync_BusinessException_ShouldReturnCorrectProblemDetails()
    {
        // Arrange
        var businessException = new BusinessException("业务规则违反")
        {
            ErrorCode = "BIZ_001",
            BusinessRule = "患者必须填写完整基本信息",
            UserMessage = "请完善患者基本信息"
        };

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, businessException, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(400);

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("业务错误");
        problemDetails.Detail.Should().Be("请完善患者基本信息");

        problemDetails.Extensions.Should().ContainKey("errorCode");
        problemDetails.Extensions.Should().ContainKey("businessRule");
        problemDetails.Extensions["errorCode"].Should().Be("BIZ_001");
        problemDetails.Extensions["businessRule"].Should().Be("患者必须填写完整基本信息");
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_ShouldReturnCorrectProblemDetails()
    {
        // Arrange
        var validationException = new ValidationException("验证失败")
        {
            ErrorCode = "VAL_001",
            FieldName = "PatientName",
            UserMessage = "患者姓名不能为空"
        };
        validationException.AddError("PatientName", "患者姓名是必填项");

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, validationException, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(400);

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("验证失败");
        problemDetails.Detail.Should().Be("患者姓名不能为空");

        problemDetails.Extensions.Should().ContainKey("errorCode");
        problemDetails.Extensions.Should().ContainKey("fieldName");
        problemDetails.Extensions.Should().ContainKey("errors");
        problemDetails.Extensions["errorCode"].Should().Be("VAL_001");
        problemDetails.Extensions["fieldName"].Should().Be("PatientName");
    }

    [Fact]
    public async Task TryHandleAsync_NotFoundException_ShouldReturnCorrectProblemDetails()
    {
        // Arrange
        var notFoundException = new NotFoundException("资源未找到")
        {
            ErrorCode = "NOT_FOUND_001",
            ResourceType = "Patient",
            ResourceId = "12345",
            UserMessage = "找不到指定的患者"
        };

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, notFoundException, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(404);

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Status.Should().Be(404);
        problemDetails.Title.Should().Be("资源未找到");
        problemDetails.Detail.Should().Be("找不到指定的患者");

        problemDetails.Extensions.Should().ContainKey("errorCode");
        problemDetails.Extensions.Should().ContainKey("resourceType");
        problemDetails.Extensions.Should().ContainKey("resourceId");
        problemDetails.Extensions["errorCode"].Should().Be("NOT_FOUND_001");
        problemDetails.Extensions["resourceType"].Should().Be("Patient");
        problemDetails.Extensions["resourceId"].Should().Be("12345");
    }

    [Fact]
    public async Task TryHandleAsync_AppException_ShouldReturnCorrectProblemDetails()
    {
        // Arrange
        var appException = new AppException("应用程序异常")
        {
            ErrorCode = "APP_001",
            ShowDetailToUser = true,
            UserMessage = "系统暂时无法处理请求"
        };

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, appException, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(500);

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Status.Should().Be(500);
        problemDetails.Title.Should().Be("应用程序异常");
        problemDetails.Detail.Should().Be("系统暂时无法处理请求");

        problemDetails.Extensions.Should().ContainKey("errorCode");
        problemDetails.Extensions["errorCode"].Should().Be("APP_001");
    }

    [Fact]
    public async Task TryHandleAsync_UnauthorizedAccessException_ShouldReturnCorrectProblemDetails()
    {
        // Arrange
        var unauthorizedException = new UnauthorizedAccessException("访问被拒绝");

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, unauthorizedException, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(401);

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Status.Should().Be(401);
        problemDetails.Title.Should().Be("未授权");
        problemDetails.Detail.Should().Be("您没有权限访问此资源");
    }

    [Fact]
    public async Task TryHandleAsync_GenericException_InDevelopment_ShouldShowDetails()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_mockEnvironment.Object);
        _httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        var genericException = new InvalidOperationException("内部错误详情");

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, genericException, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(500);

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Status.Should().Be(500);
        problemDetails.Title.Should().Be("服务器内部错误");
        problemDetails.Detail.Should().Be("内部错误详情"); // 开发环境显示详细信息
    }

    [Fact]
    public async Task TryHandleAsync_GenericException_InProduction_ShouldHideDetails()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(false);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_mockEnvironment.Object);
        _httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        var genericException = new InvalidOperationException("内部错误详情");

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, genericException, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(500);

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Status.Should().Be(500);
        problemDetails.Title.Should().Be("服务器内部错误");
        problemDetails.Detail.Should().Be("处理请求时发生错误，请稍后重试"); // 生产环境隐藏详细信息
    }

    [Fact]
    public async Task TryHandleAsync_WithAuthenticatedUser_ShouldIncludeUserInfo()
    {
        // Arrange
        var claimsIdentity = new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "testuser") },
            "Bearer");
        _httpContext.User = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);

        var exception = new InvalidOperationException("测试异常");

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Extensions.Should().ContainKey("userId");
        problemDetails.Extensions["userId"].Should().Be("testuser");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldLogStructuredException()
    {
        // Arrange
        var exception = new BusinessException("测试业务异常")
        {
            ErrorCode = "TEST_001"
        };

        // Act
        await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("异常发生")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldAddContextInformation()
    {
        // Arrange
        _httpContext.Request.Headers["User-Agent"] = "Test User Agent";
        var exception = new InvalidOperationException("测试异常");

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Extensions.Should().ContainKey("traceId");
        problemDetails.Extensions.Should().ContainKey("timestamp");
        problemDetails.Extensions.Should().ContainKey("requestMethod");
        problemDetails.Extensions.Should().ContainKey("userAgent");

        problemDetails.Extensions["traceId"].Should().Be("test-trace-id");
        problemDetails.Extensions["requestMethod"].Should().Be("GET");
        problemDetails.Extensions["userAgent"].Should().Be("Test User Agent");
    }

    [Fact]
    public async Task TryHandleAsync_AppExceptionWithShowDetailToUserFalse_ShouldHideDetails()
    {
        // Arrange
        var appException = new AppException("内部详细错误")
        {
            ErrorCode = "APP_002",
            ShowDetailToUser = false,
            UserMessage = "用户友好消息"
        };

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, appException, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Detail.Should().Be("应用程序处理异常"); // 隐藏详细信息
    }

    [Fact]
    public async Task TryHandleAsync_ValidationExceptionWithoutUserMessage_ShouldUseOriginalMessage()
    {
        // Arrange
        var validationException = new ValidationException("原始验证错误消息")
        {
            ErrorCode = "VAL_002"
        };

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, validationException, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _httpContext.Response.Body.Position = 0;
        var responseContent = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent);

        problemDetails.Should().NotBeNull();
        problemDetails.Detail.Should().Be("原始验证错误消息");
    }
}