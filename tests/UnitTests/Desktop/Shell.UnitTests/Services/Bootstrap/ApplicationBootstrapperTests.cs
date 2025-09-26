using FluentAssertions;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services.Performance;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Modularity;
using Xunit;

namespace Shell.UnitTests.Services.Bootstrap
{
    /// <summary>
    /// ApplicationBootstrapper单元测试
    /// 验证Service Locator重构后的依赖注入功能
    /// </summary>
    public class ApplicationBootstrapperTests
    {
        private readonly Mock<IApplicationInitializationService> _mockInitService;
        private readonly Mock<IStartupOptimizationService> _mockStartupService;
        private readonly Mock<IErrorHandlingService> _mockErrorService;
        private readonly Mock<IModuleManager> _mockModuleManager;
        private readonly Mock<IModuleCatalog> _mockModuleCatalog;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILogger<ApplicationBootstrapper>> _mockLogger;
        private readonly ApplicationBootstrapper _bootstrapper;

        public ApplicationBootstrapperTests()
        {
            _mockInitService = new Mock<IApplicationInitializationService>();
            _mockStartupService = new Mock<IStartupOptimizationService>();
            _mockErrorService = new Mock<IErrorHandlingService>();
            _mockModuleManager = new Mock<IModuleManager>();
            _mockModuleCatalog = new Mock<IModuleCatalog>();
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLogger = new Mock<ILogger<ApplicationBootstrapper>>();

            _bootstrapper = new ApplicationBootstrapper(
                _mockInitService.Object,
                _mockStartupService.Object,
                _mockErrorService.Object,
                _mockModuleManager.Object,
                _mockModuleCatalog.Object,
                _mockEventAggregator.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenParameterIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationBootstrapper(
                null!, _mockStartupService.Object, _mockErrorService.Object,
                _mockModuleManager.Object, _mockModuleCatalog.Object,
                _mockEventAggregator.Object, _mockLogger.Object));

            Assert.Throws<ArgumentNullException>(() => new ApplicationBootstrapper(
                _mockInitService.Object, null!, _mockErrorService.Object,
                _mockModuleManager.Object, _mockModuleCatalog.Object,
                _mockEventAggregator.Object, _mockLogger.Object));

            Assert.Throws<ArgumentNullException>(() => new ApplicationBootstrapper(
                _mockInitService.Object, _mockStartupService.Object, null!,
                _mockModuleManager.Object, _mockModuleCatalog.Object,
                _mockEventAggregator.Object, _mockLogger.Object));
        }

        [Fact]
        public async Task InitializeCoreServicesAsync_ShouldCallInitializationService()
        {
            // Arrange
            _mockInitService.Setup(x => x.InitializeCoreServicesAsync())
                           .Returns(Task.CompletedTask);

            // Act
            await _bootstrapper.InitializeCoreServicesAsync();

            // Assert
            _mockInitService.Verify(x => x.InitializeCoreServicesAsync(), Times.Once);
        }

        [Fact]
        public async Task InitializeCoreServicesAsync_ShouldHandleException_WhenServiceFails()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            _mockInitService.Setup(x => x.InitializeCoreServicesAsync())
                           .ThrowsAsync(exception);

            // Act & Assert
            await _bootstrapper.InitializeCoreServicesAsync(); // 不应抛出异常
        }

        [Fact]
        public async Task InitializeApplicationWarmupAsync_ShouldCallStartupService()
        {
            // Arrange
            _mockStartupService.Setup(x => x.WarmupApplicationAsync())
                              .Returns(Task.CompletedTask);

            // Act
            await _bootstrapper.InitializeApplicationWarmupAsync();

            // Assert
            _mockStartupService.Verify(x => x.WarmupApplicationAsync(), Times.Once);
        }

        [Fact]
        public void InitializeErrorHandlingService_ShouldCallErrorHandlingService()
        {
            // Arrange
            _mockErrorService.Setup(x => x.RegisterGlobalExceptionHandlers());

            // Act
            _bootstrapper.InitializeErrorHandlingService();

            // Assert
            _mockErrorService.Verify(x => x.RegisterGlobalExceptionHandlers(), Times.Once);
        }

        [Fact]
        public async Task LoadModulesForRoleAsync_ShouldLoadCorrectModulesForAdmin()
        {
            // Arrange
            var adminRole = UserRole.Admin;
            var expectedModules = new[]
            {
                "AuthModule", "UsersModule", "PatientsModule", "HerbsModule",
                "FormulaModule", "MedicalCaseModule", "ConsultationModule",
                "PrescriptionsModule", "MedicalWorkbenchModule"
            };

            // Act
            await _bootstrapper.LoadModulesForRoleAsync(adminRole);

            // Assert
            foreach (var module in expectedModules)
            {
                _mockModuleManager.Verify(x => x.LoadModule(module), Times.Once);
            }
        }

        [Fact]
        public async Task LoadModulesForRoleAsync_ShouldLoadCorrectModulesForNurse()
        {
            // Arrange
            var nurseRole = UserRole.Nurse;
            var expectedModules = new[]
            {
                "AuthModule", "UsersModule", "PatientsModule", "MedicalCaseModule"
            };

            // Act
            await _bootstrapper.LoadModulesForRoleAsync(nurseRole);

            // Assert
            foreach (var module in expectedModules)
            {
                _mockModuleManager.Verify(x => x.LoadModule(module), Times.Once);
            }

            // 验证不应加载的模块
            _mockModuleManager.Verify(x => x.LoadModule("HerbsModule"), Times.Never);
            _mockModuleManager.Verify(x => x.LoadModule("PrescriptionsModule"), Times.Never);
        }
    }
}