using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LYBT.WebAPI.Middleware;
using LYBT.Infrastructure.Options;
using FluentAssertions;

namespace LYBT.WebAPI.Tests.Middleware
{
    /// <summary>
    /// SecurityHeadersMiddleware 单元测试
    /// </summary>
    public class SecurityHeadersMiddlewareTests
    {
        private readonly Mock<ILogger<SecurityHeadersMiddleware>> _loggerMock;
        private readonly SecurityOptions _securityOptions;
        private readonly SecurityHeadersMiddleware _middleware;
        private readonly DefaultHttpContext _httpContext;

        public SecurityHeadersMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<SecurityHeadersMiddleware>>();
            
            _securityOptions = new SecurityOptions
            {
                Environment = new EnvironmentOptions
                {
                    HideServerInfo = true
                },
                SecurityHeaders = new SecurityHeadersOptions
                {
                    ContentSecurityPolicy = "default-src 'self'",
                    XFrameOptions = "DENY",
                    XContentTypeOptions = "nosniff",
                    ReferrerPolicy = "no-referrer",
                    PermissionsPolicy = "camera=(), microphone=()"
                }
            };
            
            var optionsMock = new Mock<IOptions<SecurityOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_securityOptions);
            
            _httpContext = new DefaultHttpContext();
            _httpContext.Response.Body = new MemoryStream();
            
            _middleware = new SecurityHeadersMiddleware(
                next: async (innerHttpContext) => await Task.CompletedTask,
                securityOptions: optionsMock.Object,
                logger: _loggerMock.Object
            );
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddAllConfiguredSecurityHeaders()
        {
            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Headers.Should().ContainKey("Content-Security-Policy");
            _httpContext.Response.Headers["Content-Security-Policy"].Should().BeEquivalentTo("default-src 'self'");
            
            _httpContext.Response.Headers.Should().ContainKey("X-Frame-Options");
            _httpContext.Response.Headers["X-Frame-Options"].Should().BeEquivalentTo("DENY");
            
            _httpContext.Response.Headers.Should().ContainKey("X-Content-Type-Options");
            _httpContext.Response.Headers["X-Content-Type-Options"].Should().BeEquivalentTo("nosniff");
            
            _httpContext.Response.Headers.Should().ContainKey("Referrer-Policy");
            _httpContext.Response.Headers["Referrer-Policy"].Should().BeEquivalentTo("no-referrer");
            
            _httpContext.Response.Headers.Should().ContainKey("Permissions-Policy");
            _httpContext.Response.Headers["Permissions-Policy"].Should().BeEquivalentTo("camera=(), microphone=()");
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddXXSSProtectionHeader()
        {
            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Headers.Should().ContainKey("X-XSS-Protection");
            _httpContext.Response.Headers["X-XSS-Protection"].Should().BeEquivalentTo("0");
        }

        [Fact]
        public async Task InvokeAsync_WhenNoCacheControlExists_ShouldAddCacheHeaders()
        {
            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Headers.Should().ContainKey("Cache-Control");
            _httpContext.Response.Headers["Cache-Control"].Should().BeEquivalentTo("no-cache, no-store, must-revalidate, private");
            
            _httpContext.Response.Headers.Should().ContainKey("Pragma");
            _httpContext.Response.Headers["Pragma"].Should().BeEquivalentTo("no-cache");
            
            _httpContext.Response.Headers.Should().ContainKey("Expires");
            _httpContext.Response.Headers["Expires"].Should().BeEquivalentTo("0");
        }

        [Fact]
        public async Task InvokeAsync_WhenCacheControlExists_ShouldNotOverrideCacheHeaders()
        {
            // Arrange
            _httpContext.Response.Headers["Cache-Control"] = "public, max-age=3600";

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Headers["Cache-Control"].Should().BeEquivalentTo("public, max-age=3600");
            _httpContext.Response.Headers.Should().NotContainKey("Pragma");
            _httpContext.Response.Headers.Should().NotContainKey("Expires");
        }

        [Fact]
        public async Task InvokeAsync_WhenHideServerInfo_ShouldRemoveServerHeaders()
        {
            // Arrange
            _httpContext.Response.Headers["Server"] = "Microsoft-IIS/10.0";
            _httpContext.Response.Headers["X-Powered-By"] = "ASP.NET";
            _httpContext.Response.Headers["X-AspNet-Version"] = "4.0.30319";
            _httpContext.Response.Headers["X-AspNetMvc-Version"] = "5.2";

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Headers.Should().NotContainKey("Server");
            _httpContext.Response.Headers.Should().NotContainKey("X-Powered-By");
            _httpContext.Response.Headers.Should().NotContainKey("X-AspNet-Version");
            _httpContext.Response.Headers.Should().NotContainKey("X-AspNetMvc-Version");
        }

        [Fact]
        public async Task InvokeAsync_WhenHideServerInfoIsFalse_ShouldNotRemoveServerHeaders()
        {
            // Arrange
            _securityOptions.Environment.HideServerInfo = false;
            
            var optionsMock = new Mock<IOptions<SecurityOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_securityOptions);
            
            var middleware = new SecurityHeadersMiddleware(
                next: async (innerHttpContext) => await Task.CompletedTask,
                securityOptions: optionsMock.Object,
                logger: _loggerMock.Object
            );
            
            _httpContext.Response.Headers["Server"] = "Microsoft-IIS/10.0";

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Headers.Should().ContainKey("Server");
            _httpContext.Response.Headers["Server"].Should().BeEquivalentTo("Microsoft-IIS/10.0");
        }

        [Fact]
        public async Task InvokeAsync_WhenHeaderNotConfigured_ShouldNotAddHeader()
        {
            // Arrange
            _securityOptions.SecurityHeaders.ContentSecurityPolicy = null;
            _securityOptions.SecurityHeaders.XFrameOptions = string.Empty;
            
            var optionsMock = new Mock<IOptions<SecurityOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_securityOptions);
            
            var middleware = new SecurityHeadersMiddleware(
                next: async (innerHttpContext) => await Task.CompletedTask,
                securityOptions: optionsMock.Object,
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Headers.Should().NotContainKey("Content-Security-Policy");
            _httpContext.Response.Headers.Should().NotContainKey("X-Frame-Options");
        }

        [Fact]
        public async Task InvokeAsync_WhenExceptionOccurs_ShouldStillCallNext()
        {
            // Arrange
            var nextCalled = false;
            
            // 创建一个会在添加头时抛出异常的响应
            var mockHeaders = new Mock<IHeaderDictionary>();
            mockHeaders.Setup(x => x.TryAdd(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("Headers already sent"));
            
            var mockResponse = new Mock<HttpResponse>();
            mockResponse.Setup(x => x.Headers).Returns(mockHeaders.Object);
            
            var mockContext = new Mock<HttpContext>();
            mockContext.Setup(x => x.Response).Returns(mockResponse.Object);
            
            var middleware = new SecurityHeadersMiddleware(
                next: async (innerHttpContext) =>
                {
                    nextCalled = true;
                    await Task.CompletedTask;
                },
                securityOptions: Options.Create(_securityOptions),
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(mockContext.Object);

            // Assert
            nextCalled.Should().BeTrue();
            
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("安全头中间件处理错误")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ShouldCallNextMiddleware()
        {
            // Arrange
            var nextCalled = false;
            var middleware = new SecurityHeadersMiddleware(
                next: async (innerHttpContext) =>
                {
                    nextCalled = true;
                    await Task.CompletedTask;
                },
                securityOptions: Options.Create(_securityOptions),
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_WithAllHeadersEmpty_ShouldOnlyAddDefaultHeaders()
        {
            // Arrange
            var emptyOptions = new SecurityOptions
            {
                Environment = new EnvironmentOptions { HideServerInfo = false },
                SecurityHeaders = new SecurityHeadersOptions()
            };
            
            var optionsMock = new Mock<IOptions<SecurityOptions>>();
            optionsMock.Setup(x => x.Value).Returns(emptyOptions);
            
            var middleware = new SecurityHeadersMiddleware(
                next: async (innerHttpContext) => await Task.CompletedTask,
                securityOptions: optionsMock.Object,
                logger: _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            // 只应该有默认的缓存控制和XSS保护头
            _httpContext.Response.Headers.Should().ContainKey("X-XSS-Protection");
            _httpContext.Response.Headers.Should().ContainKey("Cache-Control");
            _httpContext.Response.Headers.Should().ContainKey("Pragma");
            _httpContext.Response.Headers.Should().ContainKey("Expires");
            
            // 不应该有配置的安全头
            _httpContext.Response.Headers.Should().NotContainKey("Content-Security-Policy");
            _httpContext.Response.Headers.Should().NotContainKey("X-Frame-Options");
            _httpContext.Response.Headers.Should().NotContainKey("X-Content-Type-Options");
            _httpContext.Response.Headers.Should().NotContainKey("Referrer-Policy");
            _httpContext.Response.Headers.Should().NotContainKey("Permissions-Policy");
        }
    }
}