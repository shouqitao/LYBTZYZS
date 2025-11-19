using FluentAssertions;
using LYBT.Desktop.Herbs.Components;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Herbs.Tests.ViewModels;

/// <summary>
/// HerbManagementViewModel 单元测试 - Issue #2165
/// 测试药材管理ViewModel的核心功能
/// </summary>
public class HerbManagementViewModelTests : IDisposable
{
    private readonly Mock<HerbDataManager> _mockDataManager;
    private readonly Mock<IHerbRepository> _mockHerbRepository;
    private readonly Mock<ICommonDialogService> _mockDialogService;
    private readonly Mock<IEventAggregator> _mockEventAggregator;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<HerbManagementViewModel>> _mockLogger;
    private readonly Mock<ILogger<HerbDataManager>> _mockDataLogger;
    private readonly Mock<IRegionManager> _mockRegionManager;
    private readonly Mock<ISessionManager> _mockSessionManager;
    private readonly Mock<IUserNotificationService> _mockNotificationService;
    private readonly HerbManagementViewModel _viewModel;
    private readonly System.Windows.Application? _wpfApp;

    public HerbManagementViewModelTests()
    {
        // 初始化WPF Application以支持Dispatcher
        if (System.Windows.Application.Current == null)
        {
            _wpfApp = new System.Windows.Application();
        }

        // Arrange - Setup Mocks
        _mockHerbRepository = new Mock<IHerbRepository>();
        _mockDataLogger = new Mock<ILogger<HerbDataManager>>();

        // 创建HerbDataManager mock
        _mockDataManager = new Mock<HerbDataManager>(
            MockBehavior.Loose,
            _mockHerbRepository.Object,
            _mockDataLogger.Object);

        _mockDialogService = new Mock<ICommonDialogService>();
        _mockEventAggregator = new Mock<IEventAggregator>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<HerbManagementViewModel>>();
        _mockRegionManager = new Mock<IRegionManager>();
        _mockSessionManager = new Mock<ISessionManager>();
        _mockNotificationService = new Mock<IUserNotificationService>();

        // Setup LoggerFactory to return mock logger
        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        // Create ViewModel instance
        _viewModel = new HerbManagementViewModel(
            _mockDataManager.Object,
            _mockHerbRepository.Object,
            _mockDialogService.Object,
            _mockEventAggregator.Object,
            _mockLoggerFactory.Object,
            _mockRegionManager.Object,
            _mockSessionManager.Object,
            _mockNotificationService.Object
        );
    }

    #region 构造函数测试

    [Fact]
    public void Constructor_ShouldInitializeViewModel()
    {
        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.PageTitle.Should().Be("药材管理");
        _viewModel.PageSize.Should().Be(20);
    }

    [Fact]
    public void Constructor_ShouldInitializeCommands()
    {
        // Assert
        // 基类提供的命令
        _viewModel.RefreshCommand.Should().NotBeNull();
        _viewModel.DeleteCommand.Should().NotBeNull();
        _viewModel.PreviousPageCommand.Should().NotBeNull();
        _viewModel.NextPageCommand.Should().NotBeNull();
        _viewModel.BatchDeleteCommand.Should().NotBeNull();

        // HerbManagementViewModel 特定命令
        _viewModel.AddCommand.Should().NotBeNull();
        _viewModel.ViewDetailsCommand.Should().NotBeNull();
        _viewModel.EditCommand.Should().NotBeNull();
        _viewModel.CopyCommand.Should().NotBeNull();
        _viewModel.ToggleStatusCommand.Should().NotBeNull();
        _viewModel.ImportHerbsCommand.Should().NotBeNull();
        _viewModel.ExportTemplateCommand.Should().NotBeNull();
        _viewModel.ExportHerbsCommand.Should().NotBeNull();
        _viewModel.FirstPageCommand.Should().NotBeNull();
        _viewModel.LastPageCommand.Should().NotBeNull();
    }

    #endregion

    #region 药材列表加载测试

    [Fact]
    public async Task LoadPageAsync_ShouldCallDataManager()
    {
        // Arrange
        var expectedHerbs = CreateSampleHerbs();
        var pagedResult = new PagedResult<HerbDto>
        {
            Items = expectedHerbs,
            TotalCount = expectedHerbs.Count,
            CurrentPage = 1,
            PageSize = 20
        };

        _mockDataManager
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(pagedResult);

        // Act - 调用基类protected方法GetItemsAsync（避免WPF Dispatcher）
        var method = typeof(HerbManagementViewModel).BaseType!
            .GetMethod("GetItemsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = await (Task<IEnumerable<HerbDto>>)method!.Invoke(_viewModel, new object?[] { 1, 20, null })!;

        // Assert - 验证DataManager被调用
        _mockDataManager.Verify(x => x.GetPagedAsync(1, 20, null), Times.Once);
        result.Should().NotBeNull();
        result.Should().HaveCount(expectedHerbs.Count);
    }

    [Fact]
    public async Task LoadPageAsync_WithSearchText_ShouldCallDataManagerWithSearchText()
    {
        // Arrange
        var searchText = "当归";

        var filteredHerbs = new List<HerbDto>
        {
            CreateHerb("当归", "补血药")
        };

        var pagedResult = new PagedResult<HerbDto>
        {
            Items = filteredHerbs,
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 20
        };

        _mockDataManager
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), searchText))
            .ReturnsAsync(pagedResult);

        // Act - 直接调用GetItemsAsync，避免WPF Dispatcher
        var method = typeof(HerbManagementViewModel).BaseType!
            .GetMethod("GetItemsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = await (Task<IEnumerable<HerbDto>>)method!.Invoke(_viewModel, new object[] { 1, 20, searchText })!;

        // Assert - 验证DataManager被正确调用
        _mockDataManager.Verify(x => x.GetPagedAsync(1, 20, searchText), Times.Once);
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("当归");
    }

    [Fact]
    public async Task LoadPageAsync_WhenDataManagerReturnsNull_ShouldHandleGracefully()
    {
        // Arrange
        _mockDataManager
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((PagedResult<HerbDto>)null!);

        // Act - 直接调用GetItemsAsync，避免WPF Dispatcher
        var method = typeof(HerbManagementViewModel).BaseType!
            .GetMethod("GetItemsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = await (Task<IEnumerable<HerbDto>>)method!.Invoke(_viewModel, new object?[] { 1, 20, null })!;

        // Assert - 应该返回空列表，而不是抛出异常
        result.Should().BeEmpty();
    }

    #endregion

    #region 药材删除测试

    [Fact]
    public async Task DeleteHerbAsync_ShouldCallDataManagerDelete()
    {
        // Arrange
        var herb = CreateHerb("黄芪", "补气药");

        _mockDataManager
            .Setup(x => x.DeleteHerbAsync(herb.Id))
            .ReturnsAsync(true);

        // 模拟LoadPageAsync
        _mockDataManager
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new PagedResult<HerbDto> { Items = new List<HerbDto>(), TotalCount = 0 });

        // Act - 使用反射调用protected方法
        var method = typeof(HerbManagementViewModel)
            .GetMethod("OnExecuteDeleteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(_viewModel, new object[] { herb })!;

        // Assert
        _mockDataManager.Verify(x => x.DeleteHerbAsync(herb.Id), Times.Once);
    }

    [Fact]
    public async Task BatchDeleteAsync_ShouldDeleteMultipleHerbs()
    {
        // Arrange
        var herbs = new List<HerbDto>
        {
            CreateHerb("当归", "补血药"),
            CreateHerb("黄芪", "补气药"),
            CreateHerb("人参", "补气药")
        };

        _mockDataManager
            .Setup(x => x.DeleteHerbAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // 模拟LoadPageAsync
        _mockDataManager
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new PagedResult<HerbDto> { Items = new List<HerbDto>(), TotalCount = 0 });

        // Act - 使用反射调用protected方法
        var method = typeof(HerbManagementViewModel)
            .GetMethod("OnExecuteBatchDeleteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(_viewModel, new object[] { herbs })!;

        // Assert
        _mockDataManager.Verify(x => x.DeleteHerbAsync(It.IsAny<Guid>()), Times.Exactly(3));
    }


    #endregion

    #region 分页测试

    [Fact]
    public void FirstPageCommand_ShouldSetCurrentPageTo1()
    {
        // Arrange
        // 使用反射设置CurrentPage
        var property = typeof(HerbManagementViewModel).BaseType!
            .GetProperty("CurrentPage");
        property!.SetValue(_viewModel, 5);

        // Act
        var method = typeof(HerbManagementViewModel)
            .GetMethod("ExecuteFirstPage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_viewModel, null);

        // Assert
        var currentPage = (int)property.GetValue(_viewModel)!;
        currentPage.Should().Be(1);
    }


    #endregion

    #region 命令功能测试

    [Fact]
    public void ViewDetailsCommand_ShouldNavigateToDetailView()
    {
        // Arrange
        var herb = CreateHerb("当归", "补血药");
        NavigationParameters? capturedParameters = null;

        _mockRegionManager
            .Setup(x => x.RequestNavigate(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NavigationParameters>()))
            .Callback<string, string, NavigationParameters>((region, target, parameters) =>
            {
                capturedParameters = parameters;
            });

        // Act - 使用反射调用私有方法
        var method = typeof(HerbManagementViewModel)
            .GetMethod("ViewHerbDetail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_viewModel, new object[] { herb });

        // Assert
        _mockRegionManager.Verify(x => x.RequestNavigate(
            "ContentRegion",
            "HerbDetailView",
            It.IsAny<NavigationParameters>()), Times.Once);

        capturedParameters.Should().NotBeNull();
        capturedParameters!.GetValue<Guid>("HerbId").Should().Be(herb.Id);
        capturedParameters.GetValue<bool>("ReadOnly").Should().BeTrue();
    }

    [Fact]
    public void EditCommand_ShouldNavigateToEditView()
    {
        // Arrange
        var herb = CreateHerb("黄芪", "补气药");
        NavigationParameters? capturedParameters = null;

        _mockRegionManager
            .Setup(x => x.RequestNavigate(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NavigationParameters>()))
            .Callback<string, string, NavigationParameters>((region, target, parameters) =>
            {
                capturedParameters = parameters;
            });

        // Act - 使用反射调用私有方法
        var method = typeof(HerbManagementViewModel)
            .GetMethod("EditHerb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_viewModel, new object[] { herb });

        // Assert
        _mockRegionManager.Verify(x => x.RequestNavigate(
            "ContentRegion",
            "HerbDetailView",
            It.IsAny<NavigationParameters>()), Times.Once);

        capturedParameters.Should().NotBeNull();
        capturedParameters!.GetValue<Guid>("HerbId").Should().Be(herb.Id);
    }

    [Fact]
    public void CopyCommand_ShouldNavigateToCreateView()
    {
        // Arrange
        var herb = CreateHerb("人参", "补气药");
        NavigationParameters? capturedParameters = null;

        _mockRegionManager
            .Setup(x => x.RequestNavigate(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NavigationParameters>()))
            .Callback<string, string, NavigationParameters>((region, target, parameters) =>
            {
                capturedParameters = parameters;
            });

        // Act - 使用反射调用私有方法
        var method = typeof(HerbManagementViewModel)
            .GetMethod("CopyHerb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_viewModel, new object[] { herb });

        // Assert
        _mockRegionManager.Verify(x => x.RequestNavigate(
            "ContentRegion",
            "HerbDetailView",
            It.IsAny<NavigationParameters>()), Times.Once);

        capturedParameters.Should().NotBeNull();
        capturedParameters!.GetValue<Guid>("SourceHerbId").Should().Be(herb.Id);
        capturedParameters.GetValue<string>("Mode").Should().Be("Copy");
    }

    #endregion

    #region 导入导出命令测试

    [Fact]
    public void ImportHerbsCommand_ShouldBeInitialized()
    {
        // Assert
        _viewModel.ImportHerbsCommand.Should().NotBeNull();
        _viewModel.ImportHerbsCommand.CanExecute().Should().BeTrue();
    }

    [Fact]
    public void ExportTemplateCommand_ShouldBeInitialized()
    {
        // Assert
        _viewModel.ExportTemplateCommand.Should().NotBeNull();
        _viewModel.ExportTemplateCommand.CanExecute().Should().BeTrue();
    }

    [Fact]
    public void ExportHerbsCommand_WhenNoItems_ShouldNotExecute()
    {
        // Arrange - Items collection is empty by default

        // Assert
        _viewModel.ExportHerbsCommand.Should().NotBeNull();
        _viewModel.ExportHerbsCommand.CanExecute().Should().BeFalse();
    }

    #endregion

    #region 搜索功能测试

    [Fact]
    public async Task SearchByCategory_ShouldUpdateSearchText()
    {
        // Arrange
        var category = "补气药";
        var pagedResult = new PagedResult<HerbDto>
        {
            Items = new List<HerbDto>(),
            TotalCount = 0
        };

        _mockDataManager
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(pagedResult);

        // Act - 使用反射调用私有方法
        var method = typeof(HerbManagementViewModel)
            .GetMethod("SearchByCategory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_viewModel, new object[] { category });

        // 等待异步操作完成（SearchByCategory是async void）
        await Task.Delay(100);

        // Assert
        _viewModel.SearchText.Should().Be($"分类:{category}");
    }

    #endregion

    #region Helper Methods

    private List<HerbDto> CreateSampleHerbs()
    {
        return new List<HerbDto>
        {
            CreateHerb("当归", "补血药", "补血活血，调经止痛"),
            CreateHerb("黄芪", "补气药", "补气固表，利水消肿"),
            CreateHerb("人参", "补气药", "大补元气，复脉固脱"),
            CreateHerb("甘草", "补气药", "补脾益气，清热解毒")
        };
    }

    private HerbDto CreateHerb(
        string name,
        string category,
        string? effect = null,
        CommonStatus status = CommonStatus.Enabled)
    {
        return new HerbDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            Effect = effect ?? $"{name}的功效",
            Properties = "温，甘",
            Origin = "四川",
            Spec = "统货",
            Usage = "内服：煎汤，3-10g",
            Unit = "克",
            Price = 1.0m,
            CostPrice = 0.5m,
            Status = status,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _viewModel?.Dispose();
        // WPF Application需要在正确的线程上关闭，测试环境中暂时不关闭
        // _wpfApp?.Shutdown();
    }

    #endregion
}
