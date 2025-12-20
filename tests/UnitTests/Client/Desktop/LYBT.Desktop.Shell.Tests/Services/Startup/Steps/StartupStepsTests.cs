using System.Net.Http;
using FluentAssertions;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.Performance;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Startup.Steps;
using LYBT.Shared.ExceptionHandling.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Modularity;

namespace LYBT.Desktop.Shell.Tests.Services.Startup.Steps;

/// <summary>
/// 启动步骤单元测试
/// optimize-desktop-core: 更新为使用IDesktopExceptionHandler
/// </summary>
public class StartupStepsTests
{
    #region ErrorHandlingStartupStep Tests

    public class ErrorHandlingStartupStepTests
    {
        private readonly Mock<IDesktopExceptionHandler> _exceptionHandlerMock;
        private readonly Mock<ILogger<ErrorHandlingStartupStep>> _loggerMock;
        private readonly ErrorHandlingStartupStep _sut;

        public ErrorHandlingStartupStepTests()
        {
            _exceptionHandlerMock = new Mock<IDesktopExceptionHandler>();
            _loggerMock = new Mock<ILogger<ErrorHandlingStartupStep>>();
            _sut = new ErrorHandlingStartupStep(
                _exceptionHandlerMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void Properties_ShouldHaveCorrectValues()
        {
            _sut.Name.Should().Be("错误处理初始化");
            _sut.Order.Should().Be(10);
            _sut.IsRequired.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCallRegisterGlobalExceptionHandlers()
        {
            // Act
            var result = await _sut.ExecuteAsync();

            // Assert
            result.Success.Should().BeTrue();
            _exceptionHandlerMock.Verify(s => s.RegisterGlobalExceptionHandlers(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenServiceThrows_ShouldReturnFailed()
        {
            // Arrange
            _exceptionHandlerMock
                .Setup(s => s.RegisterGlobalExceptionHandlers())
                .Throws(new InvalidOperationException("Test error"));

            // Act
            var result = await _sut.ExecuteAsync();

            // Assert
            result.Success.Should().BeFalse();
            // ERR-012: 异常消息安全化 - 错误消息不应包含原始异常信息，应使用安全的用户友好消息
            result.ErrorMessage.Should().Contain("注册全局异常处理器失败");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReportProgress()
        {
            // Arrange
            var progressMock = new Mock<IProgress<string>>();

            // Act
            await _sut.ExecuteAsync(progressMock.Object);

            // Assert
            progressMock.Verify(p => p.Report(It.Is<string>(s => s.Contains("异常处理器"))), Times.Once);
        }
    }

    #endregion

    #region ModuleCoordinatorStartupStep Tests

    public class ModuleCoordinatorStartupStepTests
    {
        private readonly Mock<IModuleManager> _moduleManagerMock;
        private readonly Mock<ILogger<ModuleCoordinatorStartupStep>> _loggerMock;
        private readonly ModuleCoordinatorStartupStep _sut;

        public ModuleCoordinatorStartupStepTests()
        {
            _moduleManagerMock = new Mock<IModuleManager>();
            _loggerMock = new Mock<ILogger<ModuleCoordinatorStartupStep>>();
            _sut = new ModuleCoordinatorStartupStep(
                _moduleManagerMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void Properties_ShouldHaveCorrectValues()
        {
            _sut.Name.Should().Be("模块协调器初始化");
            _sut.Order.Should().Be(20);
            _sut.IsRequired.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSucceed()
        {
            // Act
            var result = await _sut.ExecuteAsync();

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_IsNotRequired_ShouldNotBlockStartup()
        {
            // 模块协调器初始化失败不应阻塞启动（IsRequired = false）
            // 因此即使订阅事件失败，步骤本身仍然是成功的
            // 具体的错误处理在步骤内部通过日志记录

            // Assert - 验证该步骤不是必需的
            _sut.IsRequired.Should().BeFalse();

            // Act
            var result = await _sut.ExecuteAsync();

            // Assert - 即使出现问题也应该成功（因为是可选步骤）
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReportProgress()
        {
            // Arrange
            var progressMock = new Mock<IProgress<string>>();

            // Act
            await _sut.ExecuteAsync(progressMock.Object);

            // Assert
            progressMock.Verify(p => p.Report(It.Is<string>(s => s.Contains("模块协调器"))), Times.Once);
        }
    }

    #endregion

    #region CoreServicesStartupStep Tests

    public class CoreServicesStartupStepTests
    {
        private readonly Mock<IApplicationInitializationService> _initializationServiceMock;
        private readonly Mock<ILogger<CoreServicesStartupStep>> _loggerMock;
        private readonly CoreServicesStartupStep _sut;

        public CoreServicesStartupStepTests()
        {
            _initializationServiceMock = new Mock<IApplicationInitializationService>();
            _loggerMock = new Mock<ILogger<CoreServicesStartupStep>>();
            _sut = new CoreServicesStartupStep(
                _initializationServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void Properties_ShouldHaveCorrectValues()
        {
            _sut.Name.Should().Be("核心服务初始化");
            _sut.Order.Should().Be(30);
            _sut.IsRequired.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCallInitializeCoreServicesAsync()
        {
            // Act
            var result = await _sut.ExecuteAsync();

            // Assert
            result.Success.Should().BeTrue();
            _initializationServiceMock.Verify(s => s.InitializeCoreServicesAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenServiceThrows_ShouldReturnFailed()
        {
            // Arrange
            _initializationServiceMock
                .Setup(s => s.InitializeCoreServicesAsync())
                .ThrowsAsync(new InvalidOperationException("Test error"));

            // Act
            var result = await _sut.ExecuteAsync();

            // Assert
            result.Success.Should().BeFalse();
            // ERR-012: 异常消息安全化 - 错误消息不应包含原始异常信息，应使用安全的用户友好消息
            result.ErrorMessage.Should().Contain("核心服务初始化失败");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReportProgress()
        {
            // Arrange
            var progressMock = new Mock<IProgress<string>>();

            // Act
            await _sut.ExecuteAsync(progressMock.Object);

            // Assert
            progressMock.Verify(p => p.Report(It.Is<string>(s => s.Contains("核心服务"))), Times.Once);
        }
    }

    #endregion

    #region ApiHealthCheckStartupStep Tests

    public class ApiHealthCheckStartupStepTests
    {
        private readonly Mock<IApplicationStateService> _applicationStateServiceMock;
        private readonly Mock<ILogger<ApiHealthCheckStartupStep>> _loggerMock;
        private readonly ApiHealthCheckStartupStep _sut;

        public ApiHealthCheckStartupStepTests()
        {
            _applicationStateServiceMock = new Mock<IApplicationStateService>();
            _loggerMock = new Mock<ILogger<ApiHealthCheckStartupStep>>();
            _sut = new ApiHealthCheckStartupStep(
                _applicationStateServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void Properties_ShouldHaveCorrectValues()
        {
            _sut.Name.Should().Be("API健康检查");
            _sut.Order.Should().Be(40);
            _sut.IsRequired.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_WhenApiHealthy_ShouldReturnSuccess()
        {
            // Arrange
            _applicationStateServiceMock
                .Setup(s => s.CheckApiHealthAsync(It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.ExecuteAsync();

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_WhenApiUnhealthy_ShouldReturnFailed()
        {
            // Arrange
            _applicationStateServiceMock
                .Setup(s => s.CheckApiHealthAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.ExecuteAsync();

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("API服务不可用");
        }

        [Fact]
        public async Task ExecuteAsync_WhenServiceThrows_ShouldReturnFailed()
        {
            // Arrange
            _applicationStateServiceMock
                .Setup(s => s.CheckApiHealthAsync(It.IsAny<int>()))
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            // Act
            var result = await _sut.ExecuteAsync();

            // Assert
            result.Success.Should().BeFalse();
            // ERR-012: 异常消息安全化 - 错误消息不应包含原始异常信息，应使用安全的用户友好消息
            result.ErrorMessage.Should().Contain("API健康检查失败");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReportProgress()
        {
            // Arrange
            _applicationStateServiceMock
                .Setup(s => s.CheckApiHealthAsync(It.IsAny<int>()))
                .ReturnsAsync(true);
            var progressMock = new Mock<IProgress<string>>();

            // Act
            await _sut.ExecuteAsync(progressMock.Object);

            // Assert
            progressMock.Verify(p => p.Report(It.Is<string>(s => s.Contains("API"))), Times.Once);
        }
    }

    #endregion

    #region WarmupStartupStep Tests

    public class WarmupStartupStepTests
    {
        private readonly Mock<IStartupOptimizationService> _startupOptimizationServiceMock;
        private readonly Mock<ILogger<WarmupStartupStep>> _loggerMock;
        private readonly WarmupStartupStep _sut;

        public WarmupStartupStepTests()
        {
            _startupOptimizationServiceMock = new Mock<IStartupOptimizationService>();
            _loggerMock = new Mock<ILogger<WarmupStartupStep>>();
            _sut = new WarmupStartupStep(
                _startupOptimizationServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void Properties_ShouldHaveCorrectValues()
        {
            _sut.Name.Should().Be("应用预热");
            _sut.Order.Should().Be(50);
            _sut.IsRequired.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCallWarmupApplicationAsync()
        {
            // Act
            var result = await _sut.ExecuteAsync();

            // Assert
            result.Success.Should().BeTrue();
            _startupOptimizationServiceMock.Verify(s => s.WarmupApplicationAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenServiceThrows_ShouldReturnFailed()
        {
            // Arrange
            _startupOptimizationServiceMock
                .Setup(s => s.WarmupApplicationAsync())
                .ThrowsAsync(new InvalidOperationException("Warmup error"));

            // Act
            var result = await _sut.ExecuteAsync();

            // Assert
            result.Success.Should().BeFalse();
            // ERR-012: 异常消息安全化 - 错误消息不应包含原始异常信息，应使用安全的用户友好消息
            result.ErrorMessage.Should().Contain("应用预热失败");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReportProgress()
        {
            // Arrange
            var progressMock = new Mock<IProgress<string>>();

            // Act
            await _sut.ExecuteAsync(progressMock.Object);

            // Assert
            progressMock.Verify(p => p.Report(It.Is<string>(s => s.Contains("预热"))), Times.Once);
        }
    }

    #endregion
}
