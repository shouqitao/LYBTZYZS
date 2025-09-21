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
    public class BaseControllerCoreTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly TestController _controller;

        public BaseControllerCoreTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockCache = new Mock<IMemoryCache>();
            _controller = new TestController(_mockLogger.Object, _mockCache.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_InitializeLogger_When_LoggerProvided()
        {
            // Arrange & Act
            var controller = new TestController(_mockLogger.Object);

            // Assert
            controller.TestLogger.Should().Be(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_Should_InitializeCache_When_CacheProvided()
        {
            // Arrange & Act
            var controller = new TestController(_mockLogger.Object, _mockCache.Object);

            // Assert
            controller.TestCache.Should().Be(_mockCache.Object);
        }

        [Fact]
        public void Constructor_Should_AcceptNullCache_When_CacheNotProvided()
        {
            // Arrange & Act
            var controller = new TestController(_mockLogger.Object, null);

            // Assert
            controller.TestCache.Should().BeNull();
        }

        #endregion

        #region GetOperator Tests

        [Fact]
        public void GetOperator_Should_ReturnOperatorInfo_When_ValidUserClaimsProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userName = "TestUser";
            var role = "Admin";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, userName),
                new("Admin", role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var (operatorId, operatorName, operatorRole) = _controller.TestGetOperator();

            // Assert
            operatorId.Should().Be(userId);
            operatorName.Should().Be(userName);
            operatorRole.Should().Be(role);
        }

        [Fact]
        public void GetOperator_Should_ReturnDefaultRole_When_AdminClaimMissing()
        {
            // Arrange
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

            // Act
            var (operatorId, operatorName, operatorRole) = _controller.TestGetOperator();

            // Assert
            operatorId.Should().Be(userId);
            operatorName.Should().Be(userName);
            operatorRole.Should().Be("User");
        }

        [Fact]
        public void GetOperator_Should_ThrowUnauthorizedAccessException_When_UserIdInvalid()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "invalid-guid"),
                new(ClaimTypes.Name, "TestUser")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act & Assert
            var action = () => _controller.TestGetOperator();
            action.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("未登录或用户信息无效");
        }

        [Fact]
        public void GetOperator_Should_ThrowUnauthorizedAccessException_When_UserNameMissing()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            identity.Name = null; // Explicitly set name to null

            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act & Assert
            var action = () => _controller.TestGetOperator();
            action.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("未登录或用户信息无效");
        }

        #endregion

        #region LogOperation Tests

        [Fact]
        public void LogOperation_Should_LogInformation_When_ValidOperatorAndData()
        {
            // Arrange
            SetupValidUser();
            var operation = "测试操作";
            var data = new { Property = "Value" };
            var targetId = Guid.NewGuid();

            // Act
            _controller.TestLogOperation(operation, data, targetId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(operation)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogOperation_Should_HandleLogException_When_LoggingFails()
        {
            // Arrange
            SetupValidUser();
            _mockLogger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Throws(new Exception("日志记录失败"));

            // Act & Assert
            var action = () => _controller.TestLogOperation("测试操作");
            action.Should().NotThrow("日志记录失败不应影响主业务流程");
        }

        #endregion

        #region HandleExceptionCore Tests

        [Fact]
        public void HandleExceptionCore_Should_LogError_When_ExceptionProvided()
        {
            // Arrange
            var exception = new Exception("测试异常");
            var operation = "测试操作";
            var context = new { Property = "Value" };

            // Act
            _controller.TestHandleExceptionCore(exception, operation, context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(operation)),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void HandleExceptionCore_Should_LogErrorWithoutContext_When_ContextIsNull()
        {
            // Arrange
            var exception = new Exception("测试异常");
            var operation = "测试操作";

            // Act
            _controller.TestHandleExceptionCore(exception, operation, null);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(operation)),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Model Validation Tests

        [Fact]
        public void GetModelErrors_Should_ReturnErrorList_When_ModelStateHasErrors()
        {
            // Arrange
            _controller.ModelState.AddModelError("Property1", "错误1");
            _controller.ModelState.AddModelError("Property2", "错误2");

            // Act
            var errors = _controller.TestGetModelErrors();

            // Assert
            errors.Should().HaveCount(2);
            errors.Should().Contain("错误1");
            errors.Should().Contain("错误2");
        }

        [Fact]
        public void GetModelErrors_Should_ReturnEmptyList_When_ModelStateValid()
        {
            // Arrange & Act
            var errors = _controller.TestGetModelErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void IsModelValid_Should_ReturnTrue_When_ModelStateValid()
        {
            // Arrange & Act
            var isValid = _controller.TestIsModelValid;

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void IsModelValid_Should_ReturnFalse_When_ModelStateInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Property", "错误");

            // Act
            var isValid = _controller.TestIsModelValid;

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void GetValidationErrorMessage_Should_ReturnJoinedErrors_When_ModelStateHasErrors()
        {
            // Arrange
            _controller.ModelState.AddModelError("Property1", "错误1");
            _controller.ModelState.AddModelError("Property2", "错误2");

            // Act
            var message = _controller.TestGetValidationErrorMessage();

            // Assert
            message.Should().Be("错误1; 错误2");
        }

        #endregion

        #region Utility Tests

        [Fact]
        public void IsValidGuid_Should_ReturnTrue_When_GuidNotEmpty()
        {
            // Arrange
            var guid = Guid.NewGuid();

            // Act
            var isValid = _controller.TestIsValidGuid(guid);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void IsValidGuid_Should_ReturnFalse_When_GuidEmpty()
        {
            // Arrange
            var guid = Guid.Empty;

            // Act
            var isValid = _controller.TestIsValidGuid(guid);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void GetRequestId_Should_ReturnTraceIdentifier_When_HttpContextExists()
        {
            // Arrange
            var traceId = "test-trace-id";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.HttpContext.TraceIdentifier = traceId;

            // Act
            var requestId = _controller.TestGetRequestId();

            // Assert
            requestId.Should().Be(traceId);
        }

        [Fact]
        public void GetRequestId_Should_ReturnGuid_When_HttpContextNull()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext();

            // Act
            var requestId = _controller.TestGetRequestId();

            // Assert
            requestId.Should().NotBeNullOrEmpty();
            Guid.TryParse(requestId, out _).Should().BeTrue();
        }

        [Fact]
        public void ClearCacheByPattern_Should_NotThrow_When_Called()
        {
            // Arrange & Act & Assert
            var action = () => _controller.TestClearCacheByPattern("test-pattern");
            action.Should().NotThrow();
        }

        #endregion

        #region Helper Methods

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
        }

        #endregion

        #region Test Controller

        private class TestController : BaseControllerCore
        {
            public TestController(ILogger logger, IMemoryCache? cache = null) : base(logger, cache) { }

            public ILogger TestLogger => _logger;
            public IMemoryCache? TestCache => _cache;

            public (Guid OperatorId, string OperatorName, string OperatorRole) TestGetOperator() => GetOperator();
            public void TestLogOperation(string operation, object? data = null, Guid? targetId = null) => LogOperation(operation, data, targetId);
            public void TestHandleExceptionCore(Exception ex, string operation, object? context = null) => HandleExceptionCore(ex, operation, context);
            public List<string> TestGetModelErrors() => GetModelErrors();
            public bool TestIsModelValid => IsModelValid;
            public string TestGetValidationErrorMessage() => GetValidationErrorMessage();
            public bool TestIsValidGuid(Guid id) => IsValidGuid(id);
            public string TestGetRequestId() => GetRequestId();
            public void TestClearCacheByPattern(string pattern) => ClearCacheByPattern(pattern);
        }

        #endregion
    }
}