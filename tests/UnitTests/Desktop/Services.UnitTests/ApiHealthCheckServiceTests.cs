using System.Net;
using System.Net.Http;
using FluentAssertions;
using LYBT.Desktop.Services.HealthCheck;
using LYBT.Desktop.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace LYBT.Desktop.Services.Tests;

/// <summary>
/// ApiHealthCheckService 单元测试
/// Issue #856: 登录界面 WebAPI 连接状态显示 - 验收标准 5
/// </summary>
public class ApiHealthCheckServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ApiHealthCheckService _sut;

    public ApiHealthCheckServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockConfiguration = new Mock<IConfiguration>();

        // 默认配置 WebAPI BaseUrl
        _mockConfiguration.Setup(c => c["Lybt:WebApi:BaseUrl"])
            .Returns("http://localhost:5000");

        _sut = new ApiHealthCheckService(_httpClient, _mockConfiguration.Object);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenApiRespondsSuccessfully_ReturnsHealthy()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString() == "http://localhost:5000/health"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CheckHealthAsync();

        // Assert
        result.Should().Be(ApiHealthStatus.Healthy);
        _sut.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenApiReturns500_ReturnsUnhealthy()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CheckHealthAsync();

        // Assert
        result.Should().Be(ApiHealthStatus.Unhealthy);
        _sut.LastErrorMessage.Should().Contain("服务器响应异常");
        _sut.LastErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRequestTimesOut_ReturnsUnhealthy()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        // Act
        var result = await _sut.CheckHealthAsync(timeout: 1000);

        // Assert
        result.Should().Be(ApiHealthStatus.Unhealthy);
        _sut.LastErrorMessage.Should().Contain("连接超时");
        _sut.LastErrorMessage.Should().Contain("1000ms");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNetworkError_ReturnsUnhealthy()
    {
        // Arrange
        var networkException = new HttpRequestException("网络不可达");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(networkException);

        // Act
        var result = await _sut.CheckHealthAsync();

        // Assert
        result.Should().Be(ApiHealthStatus.Unhealthy);
        _sut.LastErrorMessage.Should().Contain("网络连接失败");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnknownException_ReturnsUnhealthy()
    {
        // Arrange
        var unknownException = new InvalidOperationException("未知错误");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(unknownException);

        // Act
        var result = await _sut.CheckHealthAsync();

        // Assert
        result.Should().Be(ApiHealthStatus.Unhealthy);
        _sut.LastErrorMessage.Should().Contain("未知错误");
    }

    [Fact]
    public async Task CheckHealthAsync_UsesConfiguredBaseUrl()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Lybt:WebApi:BaseUrl"])
            .Returns("https://api.example.com");

        var sut = new ApiHealthCheckService(_httpClient, _mockConfiguration.Object);

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        HttpRequestMessage? capturedRequest = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(expectedResponse);

        // Act
        await sut.CheckHealthAsync();

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("https://api.example.com/health");
    }

    [Fact]
    public async Task CheckHealthAsync_UsesDefaultBaseUrl_WhenConfigurationMissing()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Lybt:WebApi:BaseUrl"])
            .Returns((string?)null);

        var sut = new ApiHealthCheckService(_httpClient, _mockConfiguration.Object);

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        HttpRequestMessage? capturedRequest = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(expectedResponse);

        // Act
        await sut.CheckHealthAsync();

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("http://localhost:5000/health");
    }

    [Fact]
    public async Task CheckHealthAsync_ClearsLastErrorMessage_OnNewCheck()
    {
        // Arrange
        // 第一次调用失败
        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("失败"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        await _sut.CheckHealthAsync(); // 第一次失败
        _sut.LastErrorMessage.Should().NotBeNullOrEmpty();

        await _sut.CheckHealthAsync(); // 第二次成功

        // Assert
        _sut.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenHttpClientIsNull()
    {
        // Act & Assert
        var act = () => new ApiHealthCheckService(null!, _mockConfiguration.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConfigurationIsNull()
    {
        // Act & Assert
        var act = () => new ApiHealthCheckService(_httpClient, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }
}
