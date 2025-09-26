using System;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services.Performance;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Modularity;
using Xunit;

namespace LYBT.Desktop.Shell.Tests.Services
{
    /// <summary>
    /// ApplicationBootstrapper单元测试
    /// 测试应用程序启动引导服务的所有关键功能
    /// </summary>
    public class ApplicationBootstrapperTests : TestBase
    {
        private readonly ApplicationBootstrapper _bootstrapper;
        private readonly Mock<IApplicationInitializationService> _initializationServiceMock;
        private readonly Mock<IStartupOptimizationService> _startupOptimizationServiceMock;
        private readonly Mock<IErrorHandlingService> _errorHandlingServiceMock;
        private readonly Mock<IModuleManager> _moduleManagerMock;
        private readonly Mock<IModuleCatalog> _moduleCatalogMock;
        private readonly Mock<IEventAggregator> _eventAggregatorMock;
        private readonly Mock<ILogger<ApplicationBootstrapper>> _loggerMock;

        public ApplicationBootstrapperTests()
        {
            // 创建所有依赖的Mock对象
            _initializationServiceMock = CreateMock<IApplicationInitializationService>();
            _startupOptimizationServiceMock = CreateMock<IStartupOptimizationService>();
            _errorHandlingServiceMock = CreateMock<IErrorHandlingService>();
            _moduleManagerMock = CreateMock<IModuleManager>();
            _moduleCatalogMock = CreateMock<IModuleCatalog>();
            _eventAggregatorMock = CreateMock<IEventAggregator>();
            _loggerMock = CreateLoggerMock<ApplicationBootstrapper>();

            // 创建被测试的ApplicationBootstrapper实例
            _bootstrapper = new ApplicationBootstrapper(
                _initializationServiceMock.Object,
                _startupOptimizationServiceMock.Object,
                _errorHandlingServiceMock.Object,
                _moduleManagerMock.Object,
                _moduleCatalogMock.Object,
                _eventAggregatorMock.Object,
                _loggerMock.Object);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            // 注册测试相关的服务
            services.AddSingleton(_bootstrapper);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_WithNullInitializationService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationBootstrapper(
                    null,
                    _startupOptimizationServiceMock.Object,
                    _errorHandlingServiceMock.Object,
                    _moduleManagerMock.Object,
                    _moduleCatalogMock.Object,
                    _eventAggregatorMock.Object,
                    _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullStartupOptimizationService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationBootstrapper(
                    _initializationServiceMock.Object,
                    null,
                    _errorHandlingServiceMock.Object,
                    _moduleManagerMock.Object,
                    _moduleCatalogMock.Object,
                    _eventAggregatorMock.Object,
                    _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullErrorHandlingService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationBootstrapper(
                    _initializationServiceMock.Object,
                    _startupOptimizationServiceMock.Object,
                    null,
                    _moduleManagerMock.Object,
                    _moduleCatalogMock.Object,
                    _eventAggregatorMock.Object,
                    _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullModuleManager_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationBootstrapper(
                    _initializationServiceMock.Object,
                    _startupOptimizationServiceMock.Object,
                    _errorHandlingServiceMock.Object,
                    null,
                    _moduleCatalogMock.Object,
                    _eventAggregatorMock.Object,
                    _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationBootstrapper(
                    _initializationServiceMock.Object,
                    _startupOptimizationServiceMock.Object,
                    _errorHandlingServiceMock.Object,
                    _moduleManagerMock.Object,
                    _moduleCatalogMock.Object,
                    _eventAggregatorMock.Object,
                    null));
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var bootstrapper = new ApplicationBootstrapper(
                _initializationServiceMock.Object,
                _startupOptimizationServiceMock.Object,
                _errorHandlingServiceMock.Object,
                _moduleManagerMock.Object,
                _moduleCatalogMock.Object,
                _eventAggregatorMock.Object,
                _loggerMock.Object);

            // Assert
            bootstrapper.Should().NotBeNull();
        }

        #endregion

        #region InitializeCoreServicesAsync 测试

        [Fact]
        public async Task InitializeCoreServicesAsync_WhenSuccessful_ShouldCallInitializationService()
        {
            // Arrange
            _initializationServiceMock
                .Setup(x => x.InitializeCoreServicesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _bootstrapper.InitializeCoreServicesAsync();

            // Assert
            _initializationServiceMock.Verify(
                x => x.InitializeCoreServicesAsync(),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("开始初始化核心服务")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("核心服务初始化完成")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeCoreServicesAsync_WhenThrowsException_ShouldLogErrorAndContinue()
        {
            // Arrange
            var expectedException = new Exception("初始化失败");
            _initializationServiceMock
                .Setup(x => x.InitializeCoreServicesAsync())
                .ThrowsAsync(expectedException);

            // Act
            await _bootstrapper.InitializeCoreServicesAsync();

            // Assert - 应该记录错误但不抛出异常
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("核心服务初始化失败")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region InitializeApplicationWarmupAsync 测试

        [Fact]
        public async Task InitializeApplicationWarmupAsync_WhenSuccessful_ShouldCallWarmupService()
        {
            // Arrange
            _startupOptimizationServiceMock
                .Setup(x => x.WarmupApplicationAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _bootstrapper.InitializeApplicationWarmupAsync();

            // Assert
            _startupOptimizationServiceMock.Verify(
                x => x.WarmupApplicationAsync(),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("开始应用程序预热")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("应用程序预热完成")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeApplicationWarmupAsync_WhenThrowsException_ShouldLogWarningAndContinue()
        {
            // Arrange
            var expectedException = new Exception("预热失败");
            _startupOptimizationServiceMock
                .Setup(x => x.WarmupApplicationAsync())
                .ThrowsAsync(expectedException);

            // Act
            await _bootstrapper.InitializeApplicationWarmupAsync();

            // Assert - 预热失败只记录警告，不影响主流程
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("应用预热失败")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region InitializeErrorHandlingService 测试

        [Fact]
        public void InitializeErrorHandlingService_WhenSuccessful_ShouldRegisterGlobalExceptionHandlers()
        {
            // Arrange
            _errorHandlingServiceMock
                .Setup(x => x.RegisterGlobalExceptionHandlers())
                .Verifiable();

            // Act
            _bootstrapper.InitializeErrorHandlingService();

            // Assert
            _errorHandlingServiceMock.Verify(
                x => x.RegisterGlobalExceptionHandlers(),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("注册全局异常处理器")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void InitializeErrorHandlingService_WhenThrowsException_ShouldLogError()
        {
            // Arrange
            var expectedException = new Exception("注册失败");
            _errorHandlingServiceMock
                .Setup(x => x.RegisterGlobalExceptionHandlers())
                .Throws(expectedException);

            // Act
            _bootstrapper.InitializeErrorHandlingService();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("初始化错误处理服务失败")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region LoadModulesForRoleAsync 测试

        [Theory]
        [InlineData(UserRole.Admin, new[] { "AuthModule", "UsersModule", "PatientsModule", "HerbsModule", "FormulaModule", "MedicalCaseModule", "ConsultationModule", "PrescriptionsModule", "MedicalWorkbenchModule" })]
        [InlineData(UserRole.Doctor, new[] { "AuthModule", "UsersModule", "PatientsModule", "HerbsModule", "FormulaModule", "MedicalCaseModule", "ConsultationModule", "PrescriptionsModule", "MedicalWorkbenchModule" })]
        [InlineData(UserRole.Nurse, new[] { "AuthModule", "UsersModule", "PatientsModule", "MedicalCaseModule" })]
        [InlineData(UserRole.Pharmacist, new[] { "AuthModule", "UsersModule", "HerbsModule", "FormulaModule", "PrescriptionsModule" })]
        public async Task LoadModulesForRoleAsync_WithDifferentRoles_ShouldLoadCorrectModules(UserRole role, string[] expectedModules)
        {
            // Arrange
            var loadedModules = new System.Collections.Generic.List<string>();
            _moduleManagerMock
                .Setup(x => x.LoadModule(It.IsAny<string>()))
                .Callback<string>(moduleName => loadedModules.Add(moduleName));

            // Act
            await _bootstrapper.LoadModulesForRoleAsync(role);

            // Assert
            loadedModules.Should().BeEquivalentTo(expectedModules);

            foreach (var moduleName in expectedModules)
            {
                _moduleManagerMock.Verify(
                    x => x.LoadModule(moduleName),
                    Times.Once);
            }
        }

        [Fact]
        public async Task LoadModulesForRoleAsync_WhenModuleLoadFails_ShouldContinueWithOtherModules()
        {
            // Arrange
            var role = UserRole.Nurse;
            var loadedModules = new System.Collections.Generic.List<string>();
            
            _moduleManagerMock
                .Setup(x => x.LoadModule("AuthModule"))
                .Throws(new Exception("模块加载失败"));
            
            _moduleManagerMock
                .Setup(x => x.LoadModule(It.Is<string>(m => m != "AuthModule")))
                .Callback<string>(moduleName => loadedModules.Add(moduleName));

            // Act
            await _bootstrapper.LoadModulesForRoleAsync(role);

            // Assert - 其他模块应该继续加载
            loadedModules.Should().Contain("UsersModule");
            loadedModules.Should().Contain("PatientsModule");
            loadedModules.Should().Contain("MedicalCaseModule");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("AuthModule") && v.ToString().Contains("加载失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region InitializeSimplifiedModuleCoordinator 测试

        [Fact]
        public void InitializeSimplifiedModuleCoordinator_ShouldSubscribeToModuleEvents()
        {
            // Act
            _bootstrapper.InitializeSimplifiedModuleCoordinator();

            // Assert - 验证事件订阅
            _moduleManagerMock.VerifyAdd(
                x => x.ModuleDownloadProgressChanged += It.IsAny<EventHandler<ModuleDownloadProgressChangedEventArgs>>(),
                Times.Once);

            _moduleManagerMock.VerifyAdd(
                x => x.LoadModuleCompleted += It.IsAny<EventHandler<LoadModuleCompletedEventArgs>>(),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("UltraThink简化模块协调器初始化完成")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void InitializeSimplifiedModuleCoordinator_WhenExceptionOccurs_ShouldLogErrorAndContinue()
        {
            // Arrange
            _moduleManagerMock
                .SetupAdd(x => x.ModuleDownloadProgressChanged += It.IsAny<EventHandler<ModuleDownloadProgressChangedEventArgs>>())
                .Throws(new Exception("事件订阅失败"));

            // Act
            _bootstrapper.InitializeSimplifiedModuleCoordinator();

            // Assert - 应该记录错误但不抛出异常
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("简化模块协调器初始化异常")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region 模块事件处理测试

        [Fact]
        public void ModuleEvents_WhenModuleLoadsSuccessfully_ShouldLogLoadTime()
        {
            // Arrange
            _bootstrapper.InitializeSimplifiedModuleCoordinator();
            
            var moduleInfo = new ModuleInfo
            {
                ModuleName = "TestModule",
                ModuleType = "TestModuleType"
            };

            // 捕获事件处理器
            EventHandler<ModuleDownloadProgressChangedEventArgs> downloadProgressHandler = null;
            EventHandler<LoadModuleCompletedEventArgs> loadCompletedHandler = null;

            _moduleManagerMock
                .SetupAdd(x => x.ModuleDownloadProgressChanged += It.IsAny<EventHandler<ModuleDownloadProgressChangedEventArgs>>())
                .Callback<EventHandler<ModuleDownloadProgressChangedEventArgs>>(handler => downloadProgressHandler = handler);

            _moduleManagerMock
                .SetupAdd(x => x.LoadModuleCompleted += It.IsAny<EventHandler<LoadModuleCompletedEventArgs>>())
                .Callback<EventHandler<LoadModuleCompletedEventArgs>>(handler => loadCompletedHandler = handler);

            // 重新初始化以捕获事件处理器
            _bootstrapper.InitializeSimplifiedModuleCoordinator();

            // Act - 触发模块开始加载事件
            downloadProgressHandler?.Invoke(
                _moduleManagerMock.Object,
                new ModuleDownloadProgressChangedEventArgs(moduleInfo, 0, 0));

            // 稍微延迟以确保有时间差
            System.Threading.Thread.Sleep(10);

            // 触发模块加载完成事件
            loadCompletedHandler?.Invoke(
                _moduleManagerMock.Object,
                new LoadModuleCompletedEventArgs(moduleInfo, null, true));

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => 
                        v.ToString().Contains("TestModule") && 
                        v.ToString().Contains("加载完成")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void ModuleEvents_WhenModuleLoadFails_ShouldLogError()
        {
            // Arrange
            _bootstrapper.InitializeSimplifiedModuleCoordinator();
            
            var moduleInfo = new ModuleInfo
            {
                ModuleName = "FailedModule",
                ModuleType = "FailedModuleType"
            };
            
            var loadError = new Exception("模块加载失败");

            EventHandler<LoadModuleCompletedEventArgs> loadCompletedHandler = null;

            _moduleManagerMock
                .SetupAdd(x => x.LoadModuleCompleted += It.IsAny<EventHandler<LoadModuleCompletedEventArgs>>())
                .Callback<EventHandler<LoadModuleCompletedEventArgs>>(handler => loadCompletedHandler = handler);

            // 重新初始化以捕获事件处理器
            _bootstrapper.InitializeSimplifiedModuleCoordinator();

            // Act - 触发模块加载失败事件
            loadCompletedHandler?.Invoke(
                _moduleManagerMock.Object,
                new LoadModuleCompletedEventArgs(moduleInfo, loadError, false));

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => 
                        v.ToString().Contains("FailedModule") && 
                        v.ToString().Contains("加载失败")),
                    loadError,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion
    }
}