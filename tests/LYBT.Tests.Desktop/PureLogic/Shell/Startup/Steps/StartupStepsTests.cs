using System.Net.Http;
using FluentAssertions;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.Performance;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Startup.Steps;
using LYBT.Shared.ExceptionHandling.Handlers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Prism.Modularity;

namespace LYBT.Tests.Desktop.PureLogic.Shell.Startup.Steps;

/// <summary>
/// 启动步骤单元测试
/// optimize-desktop-core: 更新为使用IDesktopExceptionHandler
/// </summary>
public class StartupStepsTests
{
    #region ErrorHandlingStartupStep Tests

    public class ErrorHandlingStartupStepTests
    {
        private readonly IDesktopExceptionHandler _exceptionHandler;
        private readonly ILogger<ErrorHandlingStartupStep> _logger;
        private readonly ErrorHandlingStartupStep _sut;

        public ErrorHandlingStartupStepTests()
        {
            _exceptionHandler = Substitute.For<IDesktopExceptionHandler>();
            _logger = Substitute.For<ILogger<ErrorHandlingStartupStep>>();
            _sut = new ErrorHandlingStartupStep(
                _exceptionHandler,
                _logger);
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
            _exceptionHandler.Received(1).RegisterGlobalExceptionHandlers();
        }

        [Fact]
        public async Task ExecuteAsync_WhenServiceThrows_ShouldReturnFailed()
        {
            // Arrange
            _exceptionHandler
                .When(s => s.RegisterGlobalExceptionHandlers())
                .Do(_ => throw new InvalidOperationException("Test error"));

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
            var progress = Substitute.For<IProgress<string>>();

            // Act
            await _sut.ExecuteAsync(progress);

            // Assert
            progress.Received(1).Report(Arg.Is<string>(s => s.Contains("异常处理器")));
        }
    }

    #endregion

    #region ModuleCoordinatorStartupStep Tests

    public class ModuleCoordinatorStartupStepTests
    {
        private readonly IModuleManager _moduleManager;
        private readonly ILogger<ModuleCoordinatorStartupStep> _logger;
        private readonly ModuleCoordinatorStartupStep _sut;

        public ModuleCoordinatorStartupStepTests()
        {
            _moduleManager = Substitute.For<IModuleManager>();
            _logger = Substitute.For<ILogger<ModuleCoordinatorStartupStep>>();
            _sut = new ModuleCoordinatorStartupStep(
                _moduleManager,
                _logger);
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
            var progress = Substitute.For<IProgress<string>>();

            // Act
            await _sut.ExecuteAsync(progress);

            // Assert
            progress.Received(1).Report(Arg.Is<string>(s => s.Contains("模块协调器")));
        }
    }

    #endregion

    #region CoreServicesStartupStep Tests

    public class CoreServicesStartupStepTests
    {
        private readonly IApplicationInitializationService _initializationService;
        private readonly ILogger<CoreServicesStartupStep> _logger;
        private readonly CoreServicesStartupStep _sut;

        public CoreServicesStartupStepTests()
        {
            _initializationService = Substitute.For<IApplicationInitializationService>();
            _logger = Substitute.For<ILogger<CoreServicesStartupStep>>();
            _sut = new CoreServicesStartupStep(
                _initializationService,
                _logger);
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
            await _initializationService.Received(1).InitializeCoreServicesAsync();
        }

        [Fact]
        public async Task ExecuteAsync_WhenServiceThrows_ShouldReturnFailed()
        {
            // Arrange
            _initializationService
                .InitializeCoreServicesAsync()
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
            var progress = Substitute.For<IProgress<string>>();

            // Act
            await _sut.ExecuteAsync(progress);

            // Assert
            progress.Received(1).Report(Arg.Is<string>(s => s.Contains("核心服务")));
        }
    }

    #endregion

    #region ApiHealthCheckStartupStep Tests

    public class ApiHealthCheckStartupStepTests
    {
        private readonly IApplicationStateService _applicationStateService;
        private readonly ILogger<ApiHealthCheckStartupStep> _logger;
        private readonly ApiHealthCheckStartupStep _sut;

        public ApiHealthCheckStartupStepTests()
        {
            _applicationStateService = Substitute.For<IApplicationStateService>();
            _logger = Substitute.For<ILogger<ApiHealthCheckStartupStep>>();
            _sut = new ApiHealthCheckStartupStep(
                _applicationStateService,
                _logger);
        }

        [Fact]
        public void Properties_ShouldHaveCorrectValues()
        {
            _sut.Name.Should().Be("API健康检查");
            _sut.Order.Should().Be(40);
            // OpenSpec: implement-local-mode - API健康检查为非必需，支持离线模式
            _sut.IsRequired.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_WhenApiHealthy_ShouldReturnSuccess()
        {
            // Arrange
            _applicationStateService
                .CheckApiHealthAsync(Arg.Any<int>())
                .Returns(true);

            // Act
            var result = await _sut.ExecuteAsync();

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_WhenApiUnhealthy_ShouldStillReturnSuccess_DoesNotBlockStartup()
        {
            // Arrange - API不健康，但启动步骤不应阻塞
            _applicationStateService
                .CheckApiHealthAsync(Arg.Any<int>())
                .Returns(false);

            // Act
            var result = await _sut.ExecuteAsync();

            // Assert - 后台异步执行，即使API不健康也立即返回成功
            result.Success.Should().BeTrue();
            result.Duration.Should().Be(TimeSpan.Zero); // 立即返回，无等待
        }

        [Fact]
        public async Task ExecuteAsync_WhenServiceThrows_ShouldStillReturnSuccess_ExceptionHandledInBackground()
        {
            // Arrange - API检查抛出异常
            _applicationStateService
                .CheckApiHealthAsync(Arg.Any<int>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            // Act
            var result = await _sut.ExecuteAsync();

            // Assert - 后台异步执行，异常在后台处理，启动不阻塞
            result.Success.Should().BeTrue();
            result.Duration.Should().Be(TimeSpan.Zero); // 立即返回，无等待
        }

        [Fact]
        public async Task ExecuteAsync_ShouldTriggerBackgroundHealthCheck()
        {
            // Arrange
            _applicationStateService
                .CheckApiHealthAsync(Arg.Any<int>())
                .Returns(true);

            // Act
            var result = await _sut.ExecuteAsync();

            // Assert - 立即返回成功，但后台应触发健康检查
            result.Success.Should().BeTrue();
            // 给后台任务一点时间执行
            await Task.Delay(100);
            await _applicationStateService.Received().CheckApiHealthAsync(Arg.Any<int>());
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReportProgress()
        {
            // Arrange
            _applicationStateService
                .CheckApiHealthAsync(Arg.Any<int>())
                .Returns(true);
            var progress = Substitute.For<IProgress<string>>();

            // Act
            await _sut.ExecuteAsync(progress);

            // Assert
            progress.Received(1).Report(Arg.Is<string>(s => s.Contains("API")));
        }
    }

    #endregion

    #region WarmupStartupStep Tests

    public class WarmupStartupStepTests
    {
        private readonly IStartupOptimizationService _startupOptimizationService;
        private readonly ILogger<WarmupStartupStep> _logger;
        private readonly WarmupStartupStep _sut;

        public WarmupStartupStepTests()
        {
            _startupOptimizationService = Substitute.For<IStartupOptimizationService>();
            _logger = Substitute.For<ILogger<WarmupStartupStep>>();
            _sut = new WarmupStartupStep(
                _startupOptimizationService,
                _logger);
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
            await _startupOptimizationService.Received(1).WarmupApplicationAsync();
        }

        [Fact]
        public async Task ExecuteAsync_WhenServiceThrows_ShouldReturnFailed()
        {
            // Arrange
            _startupOptimizationService
                .WarmupApplicationAsync()
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
            var progress = Substitute.For<IProgress<string>>();

            // Act
            await _sut.ExecuteAsync(progress);

            // Assert
            progress.Received(1).Report(Arg.Is<string>(s => s.Contains("预热")));
        }
    }

    #endregion
}
