using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using LYBT.WebAPI.Middleware;
using FluentAssertions;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.WebAPI.Tests.Middleware
{
    /// <summary>
    /// GlobalExceptionMiddleware 单元测试
    /// </summary>
    public class GlobalExceptionMiddlewareTests
    {
        private readonly Mock<ILogger<GlobalExceptionMiddleware>> _loggerMock;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly GlobalExceptionMiddleware _middleware;
        private readonly DefaultHttpContext _httpContext;
        private readonly IServiceCollection _services;

        public GlobalExceptionMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
            _environmentMock = new Mock<IWebHostEnvironment>();
            _services = new ServiceCollection();
            _services.AddSingleton(_environmentMock.Object);
            
            var serviceProvider = _services.BuildServiceProvider();
            _httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };
            _httpContext.Response.Body = new MemoryStream();
            _httpContext.Request.Path = "/api/test";
            
            _middleware = new GlobalExceptionMiddleware(
                next: async (innerHttpContext) => await Task.CompletedTask,
                logger: _loggerMock.Object
            );
        }

        [Fact]
        public async Task InvokeAsync_WhenNoException_ShouldCallNext()
        {
            // Arrange
            var nextCalled = false;
            var middleware = new GlobalExceptionMiddleware(
                next: async (innerHttpContext) =>
                {
                    nextCalled = true;
                    await Task.CompletedTask;
                },
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            nextCalled.Should().BeTrue();
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_WhenUnauthorizedAccessException_ShouldReturn401()
        {
            // Arrange
            var exception = new UnauthorizedAccessException("未授权访问");
            var middleware = new GlobalExceptionMiddleware(
                next: async (innerHttpContext) =>
                {
                    throw exception;
                },
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            
            var responseBody = await GetResponseBody(_httpContext);
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be(401);
            problemDetails.Title.Should().Be("未授权");
            problemDetails.Detail.Should().Be("未授权访问");
            problemDetails.Instance.Should().Be("/api/test");
            
            VerifyLogError(exception);
        }

        [Fact]
        public async Task InvokeAsync_WhenArgumentException_ShouldReturn400()
        {
            // Arrange
            var exception = new ArgumentException("参数不正确");
            var middleware = new GlobalExceptionMiddleware(
                next: async (innerHttpContext) =>
                {
                    throw exception;
                },
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            
            var responseBody = await GetResponseBody(_httpContext);
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be(400);
            problemDetails.Title.Should().Be("参数错误");
            problemDetails.Detail.Should().Be("参数不正确");
            
            VerifyLogError(exception);
        }

        [Fact]
        public async Task InvokeAsync_WhenKeyNotFoundException_ShouldReturn404()
        {
            // Arrange
            var exception = new KeyNotFoundException("资源不存在");
            var middleware = new GlobalExceptionMiddleware(
                next: async (innerHttpContext) =>
                {
                    throw exception;
                },
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            
            var responseBody = await GetResponseBody(_httpContext);
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be(404);
            problemDetails.Title.Should().Be("资源未找到");
            problemDetails.Detail.Should().Be("请求的资源不存在");
            
            VerifyLogError(exception);
        }

        [Fact]
        public async Task InvokeAsync_WhenInvalidOperationException_ShouldReturn400()
        {
            // Arrange
            var exception = new InvalidOperationException("操作无效");
            var middleware = new GlobalExceptionMiddleware(
                next: async (innerHttpContext) =>
                {
                    throw exception;
                },
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            
            var responseBody = await GetResponseBody(_httpContext);
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be(400);
            problemDetails.Title.Should().Be("操作无效");
            problemDetails.Detail.Should().Be("操作无效");
            
            VerifyLogError(exception);
        }

        [Fact]
        public async Task InvokeAsync_WhenGeneralException_InDevelopment_ShouldReturn500WithDetails()
        {
            // Arrange
            _environmentMock.Setup(x => x.IsDevelopment()).Returns(true);
            
            var exception = new Exception("内部错误详情");
            var middleware = new GlobalExceptionMiddleware(
                next: async (innerHttpContext) =>
                {
                    throw exception;
                },
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            
            var responseBody = await GetResponseBody(_httpContext);
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be(500);
            problemDetails.Title.Should().Be("服务器内部错误");
            problemDetails.Detail.Should().Be("内部错误详情"); // 开发环境显示详细错误
            
            VerifyLogError(exception);
        }

        [Fact]
        public async Task InvokeAsync_WhenGeneralException_InProduction_ShouldReturn500WithGenericMessage()
        {
            // Arrange
            _environmentMock.Setup(x => x.IsDevelopment()).Returns(false);
            
            var exception = new Exception("内部错误详情");
            var middleware = new GlobalExceptionMiddleware(
                next: async (innerHttpContext) =>
                {
                    throw exception;
                },
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            
            var responseBody = await GetResponseBody(_httpContext);
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be(500);
            problemDetails.Title.Should().Be("服务器内部错误");
            problemDetails.Detail.Should().Be("处理请求时发生错误，请稍后重试"); // 生产环境隐藏详细错误
            
            VerifyLogError(exception);
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddTraceIdToResponse()
        {
            // Arrange
            _httpContext.TraceIdentifier = "test-trace-id";
            
            var exception = new Exception("测试异常");
            var middleware = new GlobalExceptionMiddleware(
                next: async (innerHttpContext) =>
                {
                    throw exception;
                },
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            var responseBody = await GetResponseBody(_httpContext);
            var jsonDoc = JsonDocument.Parse(responseBody);
            
            jsonDoc.RootElement.TryGetProperty("extensions", out var extensions).Should().BeTrue();
            extensions.TryGetProperty("traceId", out var traceId).Should().BeTrue();
            traceId.GetString().Should().Be("test-trace-id");
        }

        [Fact]
        public async Task InvokeAsync_ShouldSetContentTypeToApplicationProblemJson()
        {
            // Arrange
            var exception = new Exception("测试异常");
            var middleware = new GlobalExceptionMiddleware(
                next: async (innerHttpContext) =>
                {
                    throw exception;
                },
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.ContentType.Should().Be("application/problem+json");
        }

        #region Helper Methods

        private async Task<string> GetResponseBody(HttpContext httpContext)
        {
            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(httpContext.Response.Body);
            return await reader.ReadToEndAsync();
        }

        private void VerifyLogError(Exception exception)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("发生未处理的异常")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}