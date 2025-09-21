using System.Security.Claims;
using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Web
{
    public class BaseSystemControllerTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly TestSystemController _controller;

        public BaseSystemControllerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockCache = new Mock<IMemoryCache>();
            _controller = new TestSystemController(_mockLogger.Object, _mockCache.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_InitializeBase_When_ParametersProvided()
        {
            // Arrange & Act
            var controller = new TestSystemController(_mockLogger.Object, _mockCache.Object);

            // Assert
            controller.Should().NotBeNull();
        }

        #endregion

        #region SystemOk Response Tests

        [Fact]
        public void SystemOk_Should_ReturnOkWithData_When_DataProvided()
        {
            // Arrange
            SetupHttpContext();
            var data = new { Property = "Value" };
            var message = "操作成功";

            // Act
            var result = _controller.TestSystemOk(data, message);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value;
            response.Should().NotBeNull();

            var responseType = response!.GetType();
            var successProperty = responseType.GetProperty("success");
            var messageProperty = responseType.GetProperty("message");
            var dataProperty = responseType.GetProperty("data");
            var timestampProperty = responseType.GetProperty("timestamp");
            var requestIdProperty = responseType.GetProperty("requestId");

            successProperty!.GetValue(response).Should().Be(true);
            messageProperty!.GetValue(response).Should().Be(message);
            dataProperty!.GetValue(response).Should().Be(data);
            timestampProperty!.GetValue(response).Should().BeOfType<long>();
            requestIdProperty!.GetValue(response).Should().NotBeNull();
        }

        [Fact]
        public void SystemOk_Should_ReturnOkWithoutData_When_OnlyMessageProvided()
        {
            // Arrange
            SetupHttpContext();
            var message = "系统正常";

            // Act
            var result = _controller.TestSystemOk(message);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value;
            response.Should().NotBeNull();

            var responseType = response!.GetType();
            var successProperty = responseType.GetProperty("success");
            var messageProperty = responseType.GetProperty("message");
            var timestampProperty = responseType.GetProperty("timestamp");
            var requestIdProperty = responseType.GetProperty("requestId");

            successProperty!.GetValue(response).Should().Be(true);
            messageProperty!.GetValue(response).Should().Be(message);
            timestampProperty!.GetValue(response).Should().BeOfType<long>();
            requestIdProperty!.GetValue(response).Should().NotBeNull();
        }

        [Fact]
        public void SystemOk_Should_UseDefaultMessage_When_NoMessageProvided()
        {
            // Arrange
            SetupHttpContext();

            // Act
            var result = _controller.TestSystemOk();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value;
            var messageProperty = response!.GetType().GetProperty("message");
            messageProperty!.GetValue(response).Should().Be("系统正常");
        }

        #endregion

        #region SystemError Response Tests

        [Fact]
        public void SystemError_Should_ReturnErrorWithStatusCode_When_Called()
        {
            // Arrange
            SetupHttpContext();
            var message = "系统错误";
            var statusCode = 400;

            // Act
            var result = _controller.TestSystemError(message, statusCode);

            // Assert
            var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
            errorResult.StatusCode.Should().Be(statusCode);

            var response = errorResult.Value;
            var responseType = response!.GetType();
            var successProperty = responseType.GetProperty("success");
            var messageProperty = responseType.GetProperty("message");

            successProperty!.GetValue(response).Should().Be(false);
            messageProperty!.GetValue(response).Should().Be(message);
        }

        [Fact]
        public void SystemError_Should_UseDefaultStatusCode_When_NotProvided()
        {
            // Arrange
            SetupHttpContext();
            var message = "系统错误";

            // Act
            var result = _controller.TestSystemError(message);

            // Assert
            var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
            errorResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region SystemWarning Response Tests

        [Fact]
        public void SystemWarning_Should_ReturnWarningResponse_When_Called()
        {
            // Arrange
            SetupHttpContext();
            var data = new { Property = "Value" };
            var message = "系统警告";

            // Act
            var result = _controller.TestSystemWarning(data, message);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value;
            var responseType = response!.GetType();
            var successProperty = responseType.GetProperty("success");
            var warningProperty = responseType.GetProperty("warning");
            var messageProperty = responseType.GetProperty("message");
            var dataProperty = responseType.GetProperty("data");

            successProperty!.GetValue(response).Should().Be(true);
            warningProperty!.GetValue(response).Should().Be(true);
            messageProperty!.GetValue(response).Should().Be(message);
            dataProperty!.GetValue(response).Should().Be(data);
        }

        #endregion

        #region IsSystemAdmin Tests

        [Fact]
        public void IsSystemAdmin_Should_ReturnTrue_When_UserIsAdmin()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var isAdmin = _controller.TestIsSystemAdmin();

            // Assert
            isAdmin.Should().BeTrue();
        }

        [Fact]
        public void IsSystemAdmin_Should_ReturnFalse_When_UserIsNotAdmin()
        {
            // Arrange
            SetupRegularUser();

            // Act
            var isAdmin = _controller.TestIsSystemAdmin();

            // Assert
            isAdmin.Should().BeFalse();
        }

        [Fact]
        public void IsSystemAdmin_Should_ReturnFalse_When_GetOperatorThrows()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = null! }
            };

            // Act
            var isAdmin = _controller.TestIsSystemAdmin();

            // Assert
            isAdmin.Should().BeFalse();
        }

        #endregion

        #region ValidateSystemParameters Tests

        [Fact]
        public void ValidateSystemParameters_Should_ReturnNull_When_AllValidationsPass()
        {
            // Arrange
            SetupHttpContext();
            var validations = new[]
            {
                (Condition: true, Message: "验证1"),
                (Condition: true, Message: "验证2")
            };

            // Act
            var result = _controller.TestValidateSystemParameters(validations);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ValidateSystemParameters_Should_ReturnError_When_ValidationFails()
        {
            // Arrange
            SetupHttpContext();
            var validations = new[]
            {
                (Condition: true, Message: "验证1"),
                (Condition: false, Message: "验证失败")
            };

            // Act
            var result = _controller.TestValidateSystemParameters(validations);

            // Assert
            var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
            errorResult.StatusCode.Should().Be(400);

            var response = errorResult.Value;
            var messageProperty = response!.GetType().GetProperty("message");
            messageProperty!.GetValue(response).Should().Be("验证失败");
        }

        #endregion

        #region HandleSystemException Tests

        [Fact]
        public void HandleSystemException_Should_ReturnUnauthorized_When_UnauthorizedAccessException()
        {
            // Arrange
            SetupHttpContext();
            var exception = new UnauthorizedAccessException("访问被拒绝");

            // Act
            var result = _controller.TestHandleSystemException(exception, "测试操作");

            // Assert
            var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
            errorResult.StatusCode.Should().Be(401);

            var response = errorResult.Value;
            var messageProperty = response!.GetType().GetProperty("message");
            messageProperty!.GetValue(response).Should().Be("访问被拒绝");
        }

        [Fact]
        public void HandleSystemException_Should_ReturnBadRequest_When_ArgumentException()
        {
            // Arrange
            SetupHttpContext();
            var exception = new ArgumentException("参数错误");

            // Act
            var result = _controller.TestHandleSystemException(exception, "测试操作");

            // Assert
            var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
            errorResult.StatusCode.Should().Be(400);

            var response = errorResult.Value;
            var messageProperty = response!.GetType().GetProperty("message");
            messageProperty!.GetValue(response).Should().Be("参数错误");
        }

        [Fact]
        public void HandleSystemException_Should_ReturnConflict_When_InvalidOperationException()
        {
            // Arrange
            SetupHttpContext();
            var exception = new InvalidOperationException("操作无效");

            // Act
            var result = _controller.TestHandleSystemException(exception, "测试操作");

            // Assert
            var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
            errorResult.StatusCode.Should().Be(409);

            var response = errorResult.Value;
            var messageProperty = response!.GetType().GetProperty("message");
            messageProperty!.GetValue(response).Should().Be("操作无效");
        }

        [Fact]
        public void HandleSystemException_Should_ReturnInternalServerError_When_GenericException()
        {
            // Arrange
            SetupHttpContext();
            var exception = new Exception("通用异常");
            var operation = "测试操作";

            // Act
            var result = _controller.TestHandleSystemException(exception, operation);

            // Assert
            var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
            errorResult.StatusCode.Should().Be(500);

            var response = errorResult.Value;
            var messageProperty = response!.GetType().GetProperty("message");
            messageProperty!.GetValue(response).Should().Be($"{operation}执行失败");
        }

        #endregion

        #region Cache Tests

        [Fact]
        public void ClearCacheByPattern_Should_LogOperation_When_Called()
        {
            // Arrange
            SetupValidUser();
            var pattern = "test-pattern";

            // Act
            _controller.TestClearCacheByPattern(pattern);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("清除缓存")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void GetCacheStats_Should_ReturnCacheInfo_When_Called()
        {
            // Arrange & Act
            var stats = _controller.TestGetCacheStats();

            // Assert
            stats.Should().NotBeNull();
            var statsType = stats.GetType();
            var cacheProviderProperty = statsType.GetProperty("cacheProvider");
            var timestampProperty = statsType.GetProperty("timestamp");

            cacheProviderProperty!.GetValue(stats).Should().NotBeNull();
            timestampProperty!.GetValue(stats).Should().BeOfType<long>();
        }

        #endregion

        #region System Monitoring Tests

        [Fact]
        public void GetSystemInfo_Should_ReturnSystemInformation_When_Called()
        {
            // Arrange & Act
            var systemInfo = _controller.TestGetSystemInfo();

            // Assert
            systemInfo.Should().NotBeNull();
            var infoType = systemInfo.GetType();

            var environmentProperty = infoType.GetProperty("environment");
            var versionProperty = infoType.GetProperty("version");
            var frameworkProperty = infoType.GetProperty("framework");
            var platformProperty = infoType.GetProperty("platform");
            var serverTimeProperty = infoType.GetProperty("serverTime");
            var processIdProperty = infoType.GetProperty("processId");
            var workingSetProperty = infoType.GetProperty("workingSet");

            environmentProperty!.GetValue(systemInfo).Should().NotBeNull();
            versionProperty!.GetValue(systemInfo).Should().NotBeNull();
            frameworkProperty!.GetValue(systemInfo).Should().NotBeNull();
            platformProperty!.GetValue(systemInfo).Should().NotBeNull();
            serverTimeProperty!.GetValue(systemInfo).Should().BeOfType<DateTimeOffset>();
            processIdProperty!.GetValue(systemInfo).Should().BeOfType<int>();
            workingSetProperty!.GetValue(systemInfo).Should().BeOfType<long>();
        }

        [Fact]
        public void GetHealthStatus_Should_ReturnHealthInformation_When_Called()
        {
            // Arrange & Act
            var healthStatus = _controller.TestGetHealthStatus();

            // Assert
            healthStatus.Should().NotBeNull();
            var statusType = healthStatus.GetType();

            var statusProperty = statusType.GetProperty("status");
            var checksProperty = statusType.GetProperty("checks");

            statusProperty!.GetValue(healthStatus).Should().NotBeNull();
            checksProperty!.GetValue(healthStatus).Should().NotBeNull();

            var status = (string)statusProperty.GetValue(healthStatus)!;
            status.Should().BeOneOf("Healthy", "Warning", "Unhealthy");
        }

        #endregion

        #region Helper Methods

        private void SetupHttpContext()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.HttpContext.TraceIdentifier = "test-trace-id";
        }

        private void SetupValidUser()
        {
            var userId = Guid.NewGuid();
            var userName = "TestUser";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, userName)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            _controller.HttpContext.TraceIdentifier = "test-trace-id";
        }

        private void SetupAdminUser()
        {
            var userId = Guid.NewGuid();
            var userName = "AdminUser";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, userName),
                new("Admin", "SysAdmin")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private void SetupRegularUser()
        {
            var userId = Guid.NewGuid();
            var userName = "RegularUser";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, userName)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #endregion

        #region Test Controller

        private class TestSystemController : BaseSystemController
        {
            public TestSystemController(ILogger logger, IMemoryCache? cache = null) : base(logger, cache) { }

            public IActionResult TestSystemOk(object data, string message = "系统正常") => SystemOk(data, message);
            public IActionResult TestSystemOk(string message = "系统正常") => SystemOk(message);
            public IActionResult TestSystemError(string message, int statusCode = 500) => SystemError(message, statusCode);
            public IActionResult TestSystemWarning(object data, string message) => SystemWarning(data, message);
            public bool TestIsSystemAdmin() => IsSystemAdmin();
            public IActionResult? TestValidateSystemParameters(params (bool Condition, string Message)[] validations) => ValidateSystemParameters(validations);
            public IActionResult TestHandleSystemException(Exception ex, string operation, object? context = null) => HandleSystemException(ex, operation, context);
            public void TestClearCacheByPattern(string pattern) => ClearCacheByPattern(pattern);
            public object TestGetCacheStats() => GetCacheStats();
            public object TestGetSystemInfo() => GetSystemInfo();
            public object TestGetHealthStatus() => GetHealthStatus();
        }

        #endregion
    }
}