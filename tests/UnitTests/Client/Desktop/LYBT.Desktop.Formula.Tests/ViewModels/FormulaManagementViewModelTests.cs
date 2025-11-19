using FluentAssertions;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using Xunit;

namespace LYBT.Desktop.Formula.Tests.ViewModels
{
    /// <summary>
    /// FormulaManagementViewModel单元测试 - Issue #2165
    /// </summary>
    public class FormulaManagementViewModelTests
    {
        private readonly Mock<IFormulaCommandHandler> _mockCommandHandler;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<ILogger<FormulaManagementViewModel>> _mockLogger;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IUserNotificationService> _mockUserNotificationService;
        private readonly FormulaManagementViewModel _viewModel;

        public FormulaManagementViewModelTests()
        {
            // WPF Application初始化（支持Dispatcher）
            if (System.Windows.Application.Current == null)
            {
                new System.Windows.Application();
            }

            _mockCommandHandler = new Mock<IFormulaCommandHandler>();
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLogger = new Mock<ILogger<FormulaManagementViewModel>>();
            _mockRegionManager = new Mock<IRegionManager>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockUserNotificationService = new Mock<IUserNotificationService>();

            _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);

            _viewModel = new FormulaManagementViewModel(
                _mockCommandHandler.Object,
                _mockEventAggregator.Object,
                _mockLoggerFactory.Object,
                _mockRegionManager.Object,
                _mockSessionManager.Object,
                _mockUserNotificationService.Object);
        }

        private FormulaDto CreateFormula(string name)
        {
            return new FormulaDto
            {
                Id = Guid.NewGuid(),
                Name = name,
                Effect = $"{name}的功效",
                Indications = "主治症状",
                Usage = "用法用量",
                Status = CommonStatus.Enabled
            };
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_ShouldInitializeViewModel()
        {
            // Assert
            _viewModel.Should().NotBeNull();
            _viewModel.PageTitle.Should().Be("配方管理");
            _viewModel.Items.Should().NotBeNull();
            _viewModel.Items.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_ShouldInitializeCommands()
        {
            // Assert
            _viewModel.SearchCommand.Should().NotBeNull();
            _viewModel.RefreshCommand.Should().NotBeNull();
            _viewModel.AddCommand.Should().NotBeNull();
            _viewModel.DeleteCommand.Should().NotBeNull();
            _viewModel.ViewDetailCommand.Should().NotBeNull();
            _viewModel.EditCommand.Should().NotBeNull();
            _viewModel.CopyCommand.Should().NotBeNull();
            _viewModel.ImportFormulasCommand.Should().NotBeNull();
            _viewModel.ExportTemplateCommand.Should().NotBeNull();
            _viewModel.ExportFormulasCommand.Should().NotBeNull();
            _viewModel.ClearFiltersCommand.Should().NotBeNull();
            _viewModel.SearchByCategoryCommand.Should().NotBeNull();
            _viewModel.PreviousPageCommand.Should().NotBeNull();
            _viewModel.NextPageCommand.Should().NotBeNull();
            _viewModel.FirstPageCommand.Should().NotBeNull();
            _viewModel.LastPageCommand.Should().NotBeNull();
        }

        #endregion

        #region 配方列表加载测试

        [Fact]
        public async Task LoadPageAsync_ShouldLoadFormulas_WhenSuccessful()
        {
            // Arrange
            var formulas = new List<FormulaDto>
            {
                CreateFormula("桂枝汤"),
                CreateFormula("麻黄汤")
            };

            var pagedData = new PagedResult<FormulaDto>
            {
                Items = formulas,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 20
            };

            _mockCommandHandler.Setup(x => x.GetPagedAsync(1, 20, null))
                .ReturnsAsync((true, pagedData, null));

            // Act
            await _viewModel.LoadPageAsync();

            // Assert
            _viewModel.Items.Should().HaveCount(2);
            _viewModel.TotalCount.Should().Be(2);
            _viewModel.CurrentPage.Should().Be(1);
        }

        [Fact]
        public async Task LoadPageAsync_ShouldHandleEmptyResult()
        {
            // Arrange
            var pagedData = new PagedResult<FormulaDto>
            {
                Items = new List<FormulaDto>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20
            };

            _mockCommandHandler.Setup(x => x.GetPagedAsync(1, 20, null))
                .ReturnsAsync((true, pagedData, null));

            // Act
            await _viewModel.LoadPageAsync();

            // Assert
            _viewModel.Items.Should().BeEmpty();
            _viewModel.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task LoadPageAsync_ShouldHandleException()
        {
            // Arrange
            _mockCommandHandler.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((false, null, "查询失败"));

            // Act & Assert - 不应崩溃
            await _viewModel.LoadPageAsync();

            _viewModel.Items.Should().BeEmpty();
        }

        #endregion

        #region 配方删除测试

        [Fact]
        public async Task DeleteFormulaAsync_ShouldDeleteAndReload_WhenSuccessful()
        {
            // Arrange
            var formula = CreateFormula("桂枝汤");
            _mockCommandHandler.Setup(x => x.DeleteAsync(formula.Id)).ReturnsAsync(true);

            var pagedData = new PagedResult<FormulaDto>
            {
                Items = new List<FormulaDto>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20
            };
            _mockCommandHandler.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((true, pagedData, null));

            _mockUserNotificationService.Setup(x => x.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var method = typeof(FormulaManagementViewModel)
                .GetMethod("OnExecuteDeleteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(_viewModel, new object[] { formula })!;

            // Assert
            _mockCommandHandler.Verify(x => x.DeleteAsync(formula.Id), Times.Once);
        }

        [Fact]
        public async Task BatchDeleteAsync_ShouldDeleteMultipleFormulas()
        {
            // Arrange
            var formulas = new List<FormulaDto>
            {
                CreateFormula("桂枝汤"),
                CreateFormula("麻黄汤")
            };

            _mockCommandHandler.Setup(x => x.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            _mockUserNotificationService.Setup(x => x.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var method = typeof(FormulaManagementViewModel)
                .GetMethod("OnExecuteBatchDeleteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(_viewModel, new object[] { formulas })!;

            // Assert
            _mockCommandHandler.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Exactly(2));
        }

        #endregion

        #region 分页测试

        [Fact]
        public void FirstPageCommand_ShouldSetCurrentPageToFirst()
        {
            // Arrange - 使用反射设置CurrentPage
            var currentPageProperty = typeof(FormulaManagementViewModel).BaseType!
                .GetProperty("CurrentPage");
            currentPageProperty!.SetValue(_viewModel, 5);

            var pagedData = new PagedResult<FormulaDto>
            {
                Items = new List<FormulaDto>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20
            };
            _mockCommandHandler.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((true, pagedData, null));

            // Act
            var method = typeof(FormulaManagementViewModel).BaseType!
                .GetMethod("ExecuteFirstPage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method!.Invoke(_viewModel, null);

            // Allow async operation to complete
            System.Threading.Thread.Sleep(100);

            // Assert
            var currentPage = (int)currentPageProperty.GetValue(_viewModel)!;
            currentPage.Should().Be(1);
        }

        #endregion

        #region 命令功能测试

        [Fact]
        public void ViewDetailsCommand_ShouldNavigateToDetailView()
        {
            // Arrange
            var formula = CreateFormula("桂枝汤");
            NavigationParameters? capturedParameters = null;

            _mockRegionManager.Setup(x => x.RequestNavigate(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<NavigationParameters>()))
                .Callback<string, string, NavigationParameters>((region, target, parameters) =>
                {
                    capturedParameters = parameters;
                });

            // Act
            _viewModel.ViewDetailCommand.Execute(formula);

            // Assert
            _mockRegionManager.Verify(x => x.RequestNavigate(
                "ContentRegion",
                "FormulaDetailView",
                It.IsAny<NavigationParameters>()), Times.Once);

            capturedParameters.Should().NotBeNull();
            capturedParameters!.GetValue<Guid>("FormulaId").Should().Be(formula.Id);
            capturedParameters.GetValue<bool>("ReadOnly").Should().BeTrue();
        }

        [Fact]
        public void EditCommand_ShouldNavigateToEditView()
        {
            // Arrange
            var formula = CreateFormula("桂枝汤");
            NavigationParameters? capturedParameters = null;

            _mockRegionManager.Setup(x => x.RequestNavigate(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<NavigationParameters>()))
                .Callback<string, string, NavigationParameters>((region, target, parameters) =>
                {
                    capturedParameters = parameters;
                });

            // Act
            _viewModel.EditCommand.Execute(formula);

            // Assert
            _mockRegionManager.Verify(x => x.RequestNavigate(
                "ContentRegion",
                "FormulaDetailView",
                It.IsAny<NavigationParameters>()), Times.Once);

            capturedParameters.Should().NotBeNull();
            capturedParameters!.GetValue<Guid>("FormulaId").Should().Be(formula.Id);
        }

        [Fact]
        public void CopyCommand_ShouldNavigateToDetailViewWithCopyMode()
        {
            // Arrange
            var formula = CreateFormula("桂枝汤");
            _mockSessionManager.Setup(x => x.HasPermission(UserRole.Admin)).Returns(true);

            NavigationParameters? capturedParameters = null;

            _mockRegionManager.Setup(x => x.RequestNavigate(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<NavigationParameters>()))
                .Callback<string, string, NavigationParameters>((region, target, parameters) =>
                {
                    capturedParameters = parameters;
                });

            // Act
            _viewModel.CopyCommand.Execute(formula);

            // Assert
            _mockRegionManager.Verify(x => x.RequestNavigate(
                "ContentRegion",
                "FormulaDetailView",
                It.IsAny<NavigationParameters>()), Times.Once);

            capturedParameters.Should().NotBeNull();
            capturedParameters!.GetValue<Guid>("SourceFormulaId").Should().Be(formula.Id);
            capturedParameters.GetValue<string>("Mode").Should().Be("Copy");
        }

        #endregion

        #region 导入导出命令测试

        [Fact]
        public async Task ImportFormulasCommand_ShouldShowInfoDialog()
        {
            // Arrange
            _mockUserNotificationService.Setup(x => x.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            _viewModel.ImportFormulasCommand.Execute();
            await Task.Delay(50); // 等待async void完成

            // Assert
            _mockUserNotificationService.Verify(x => x.ShowSuccessAsync(
                It.Is<string>(s => s.Contains("导入配方功能开发中")), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ExportTemplateCommand_ShouldShowInfoDialog()
        {
            // Arrange
            _mockUserNotificationService.Setup(x => x.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            _viewModel.ExportTemplateCommand.Execute();
            await Task.Delay(50); // 等待async void完成

            // Assert
            _mockUserNotificationService.Verify(x => x.ShowSuccessAsync(
                It.Is<string>(s => s.Contains("导出模板功能开发中")), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ExportFormulasCommand_ShouldShowInfoDialog()
        {
            // Arrange
            // 先加载一些数据，让Items不为空
            var formulas = new List<FormulaDto> { CreateFormula("桂枝汤") };
            var pagedData = new PagedResult<FormulaDto>
            {
                Items = formulas,
                TotalCount = 1,
                CurrentPage = 1,
                PageSize = 20
            };

            _mockCommandHandler.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((true, pagedData, null));

            await _viewModel.LoadPageAsync();

            _mockUserNotificationService.Setup(x => x.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            _viewModel.ExportFormulasCommand.Execute();
            await Task.Delay(50); // 等待async void完成

            // Assert
            _mockUserNotificationService.Verify(x => x.ShowSuccessAsync(
                It.Is<string>(s => s.Contains("导出配方功能开发中")), It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region 搜索功能测试

        [Fact]
        public async Task SearchByCategory_ShouldUpdateSearchTextAndReload()
        {
            // Arrange
            var category = "解表剂";

            var pagedData = new PagedResult<FormulaDto>
            {
                Items = new List<FormulaDto>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20
            };

            _mockCommandHandler.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((true, pagedData, null));

            // Act
            _viewModel.SearchByCategoryCommand.Execute(category);
            await Task.Delay(100); // 等待async void完成

            // Assert
            _viewModel.SearchText.Should().Be($"分类:{category}");
        }

        #endregion
    }
}
