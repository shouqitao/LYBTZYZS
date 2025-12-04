using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using LYBT.WebAPI.Middleware;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.WebAPI.Middleware.Tests
{
    /// <summary>
    /// MedicalCase权限验证中间件单元测试
    /// Epic #1612: MedicalCase模块权限优化
    /// </summary>
    public class MedicalCasePermissionMiddlewareTests
    {
        private readonly Mock<ILogger<MedicalCasePermissionMiddleware>> _loggerMock;
        private readonly MedicalCasePermissionMiddleware _middleware;

        public MedicalCasePermissionMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<MedicalCasePermissionMiddleware>>();
            _middleware = new MedicalCasePermissionMiddleware(_nextDelegate.Object, _loggerMock.Object);
        }

        private readonly Mock<RequestDelegate> _nextDelegate = new();

        #region 路径识别测试

        [Fact]
        public async Task InvokeAsync_NonMedicalCaseRequest_ShouldNotProcessPermission()
        {
            // Arrange
            var httpContext = CreateHttpContext("GET", "/api/v1/patients");
            var user = CreateAuthenticatedUser();
            httpContext.User = user;

            // Act
            await _middleware.InvokeAsync(httpContext);

            // Assert
            _nextDelegate.Verify(next => next(It.IsAny<HttpContext>()), Times.Once);
            Assert.False(httpContext.Items.ContainsKey("MedicalCaseUserInfo"));
        }

        [Theory]
        [InlineData("PUT", "/api/v1/medicalcases")]
        [InlineData("PATCH", "/api/v1/medicalcases/123")]
        [InlineData("DELETE", "/api/v1/medicalcases/456")]
        [InlineData("PUT", "/api/v2/medicalcases")]
        public async Task InvokeAsync_MedicalCaseRestrictedRequest_ShouldProcessPermission(string method, string path)
        {
            // Arrange
            var httpContext = CreateHttpContext(method, path);
            var user = CreateAuthenticatedUser();
            httpContext.User = user;

            // Act
            await _middleware.InvokeAsync(httpContext);

            // Assert
            _nextDelegate.Verify(next => next(It.IsAny<HttpContext>()), Times.Once);
            Assert.True(httpContext.Items.ContainsKey("MedicalCaseUserInfo"));
        }

        [Theory]
        [InlineData("GET", "/api/v1/medicalcases")]
        [InlineData("POST", "/api/v1/medicalcases")]
        [InlineData("GET", "/api/v1/medicalcases/123")]
        public async Task InvokeAsync_MedicalCaseReadOnlyRequest_ShouldNotProcessPermission(string method, string path)
        {
            // Arrange
            var httpContext = CreateHttpContext(method, path);
            var user = CreateAuthenticatedUser();
            httpContext.User = user;

            // Act
            await _middleware.InvokeAsync(httpContext);

            // Assert
            _nextDelegate.Verify(next => next(It.IsAny<HttpContext>()), Times.Once);
            Assert.False(httpContext.Items.ContainsKey("MedicalCaseUserInfo"));
        }

        #endregion

        #region 用户信息提取测试

        [Fact]
        public async Task InvokeAsync_ValidUserClaims_ShouldExtractUserInfo()
        {
            // Arrange
            var httpContext = CreateHttpContext("PUT", "/api/v1/medicalcases/123");
            var userId = Guid.NewGuid();
            var user = CreateAuthenticatedUser(userId, "testuser", UserRole.Doctor);
            httpContext.User = user;

            // Act
            await _middleware.InvokeAsync(httpContext);

            // Assert
            Assert.True(httpContext.Items.ContainsKey("MedicalCaseUserInfo"));
            var userInfo = httpContext.Items["MedicalCaseUserInfo"] as MedicalCaseUserInfo;
            Assert.NotNull(userInfo);
            Assert.Equal(userId, userInfo.UserId);
            Assert.Equal("testuser", userInfo.UserName);
            Assert.Equal(UserRole.Doctor, userInfo.Role);
            Assert.False(userInfo.IsAdmin);
        }

        [Fact]
        public async Task InvokeAsync_AdminUser_ShouldSetAdminFlag()
        {
            // Arrange
            var httpContext = CreateHttpContext("PUT", "/api/v1/medicalcases/123");
            var userId = Guid.NewGuid();
            var user = CreateAuthenticatedUser(userId, "adminuser", UserRole.Admin);
            httpContext.User = user;

            // Act
            await _middleware.InvokeAsync(httpContext);

            // Assert
            var userInfo = httpContext.Items["MedicalCaseUserInfo"] as MedicalCaseUserInfo;
            Assert.NotNull(userInfo);
            Assert.True(userInfo.IsAdmin);
            Assert.True(userInfo.CanEditToday);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("invalid-guid")]
        public async Task InvokeAsync_InvalidUserId_ShouldReturnUnauthorized(string userIdValue)
        {
            // Arrange
            var httpContext = CreateHttpContext("PUT", "/api/v1/medicalcases/123");
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userIdValue ?? string.Empty),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Doctor")
            }, "Test"));

            var responseStream = new MemoryStream();
            httpContext.Response.Body = responseStream;
            httpContext.User = user;

            // Act
            await _middleware.InvokeAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
            _nextDelegate.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_UnauthenticatedUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var httpContext = CreateHttpContext("PUT", "/api/v1/medicalcases/123");
            httpContext.User = new ClaimsPrincipal(); // 未认证用户

            var responseStream = new MemoryStream();
            httpContext.Response.Body = responseStream;

            // Act
            await _middleware.InvokeAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
            _nextDelegate.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
        }

        #endregion

        #region 辅助方法

        private static HttpContext CreateHttpContext(string method, string path)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = method;
            httpContext.Request.Path = path;
            httpContext.Response.Body = new MemoryStream();

            var features = new FeatureCollection();
            features.Set<IHttpRequestFeature>(new HttpRequestFeature());
            features.Set<IHttpResponseFeature>(new HttpResponseFeature());

            return httpContext;
        }

        private static ClaimsPrincipal CreateAuthenticatedUser(
            Guid? userId = null,
            string? userName = null,
            UserRole role = UserRole.Doctor)
        {
            userId ??= Guid.NewGuid();
            userName ??= "testuser";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, userId.Value.ToString()),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role.ToString())
            };

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        #endregion
    }
}