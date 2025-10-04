# Issue: [Desktop] Phase 2 - 增强导航系统实现NavigationJournal

## 问题描述

当前导航系统缺少历史管理功能，用户无法进行前进/后退操作。同时区域导航（Region Navigation）未充分利用，导航逻辑分散在各个ViewModel中，缺乏统一管理。

## 影响范围

### 需要修改的核心文件

1. **src/Client/Desktop/Core/Constants/RegionNames.cs** (新建)
2. **src/Client/Desktop/Core/Services/Navigation/NavigationService.cs**
3. **src/Client/Desktop/Shell/Views/MainWindow.xaml**
4. **src/Client/Desktop/Workstationes/MedicalWorkstation/Views/MedicalWorkstationMainView.xaml**
5. **所有需要导航历史的ViewModel**

## 详细优化方案

### 1. 定义区域常量

#### 1.1 创建RegionNames.cs
```csharp
// 新建文件：src/Client/Desktop/Core/Constants/RegionNames.cs
namespace LYBT.Desktop.Core.Constants
{
    /// <summary>
    /// Prism区域名称常量定义
    /// </summary>
    public static class RegionNames
    {
        // Shell级别区域
        public const string MainContentRegion = "MainContentRegion";
        public const string MenuRegion = "MenuRegion";
        public const string StatusBarRegion = "StatusBarRegion";
        public const string ToolBarRegion = "ToolBarRegion";

        // 工作台级别区域
        public const string WorkstationContentRegion = "WorkstationContentRegion";
        public const string WorkstationNavigationRegion = "WorkstationNavigationRegion";
        public const string WorkstationDetailRegion = "WorkstationDetailRegion";

        // 模块级别区域
        public const string PatientListRegion = "PatientListRegion";
        public const string PatientDetailRegion = "PatientDetailRegion";
        public const string ConsultationRegion = "ConsultationRegion";
        public const string PrescriptionRegion = "PrescriptionRegion";
        public const string HerbsRegion = "HerbsRegion";

        // 对话框区域
        public const string DialogRegion = "DialogRegion";
        public const string PopupRegion = "PopupRegion";
    }
}
```

### 2. 增强NavigationService

#### 2.1 IEnhancedNavigationService接口
```csharp
// 新建文件：src/Client/Desktop/Core/Services/Navigation/IEnhancedNavigationService.cs
using Prism.Regions;

namespace LYBT.Desktop.Core.Services.Navigation
{
    public interface IEnhancedNavigationService
    {
        // 基础导航
        Task<IRegionNavigationResult> NavigateAsync(string regionName, string target);
        Task<IRegionNavigationResult> NavigateAsync(string regionName, string target, NavigationParameters parameters);

        // 导航历史
        bool CanGoBack(string regionName);
        bool CanGoForward(string regionName);
        void GoBack(string regionName);
        void GoForward(string regionName);

        // 区域管理
        void ClearHistory(string regionName);
        IRegionNavigationJournal GetJournal(string regionName);

        // 全局导航
        Task<IRegionNavigationResult> NavigateToWorkstationAsync(string workbenchName);
        Task<IRegionNavigationResult> NavigateToModuleAsync(string moduleName, NavigationParameters parameters = null);
    }
}
```

#### 2.2 EnhancedNavigationService实现
```csharp
// 修改文件：src/Client/Desktop/Core/Services/Navigation/NavigationService.cs
using Prism.Regions;
using System.Collections.Concurrent;

namespace LYBT.Desktop.Core.Services.Navigation
{
    public class EnhancedNavigationService : IEnhancedNavigationService
    {
        private readonly IRegionManager _regionManager;
        private readonly ILogger<EnhancedNavigationService> _logger;
        private readonly ConcurrentDictionary<string, IRegionNavigationJournal> _journalCache;

        public EnhancedNavigationService(
            IRegionManager regionManager,
            ILogger<EnhancedNavigationService> logger)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _journalCache = new ConcurrentDictionary<string, IRegionNavigationJournal>();
        }

        #region 基础导航

        public Task<IRegionNavigationResult> NavigateAsync(string regionName, string target)
        {
            return NavigateAsync(regionName, target, null);
        }

        public async Task<IRegionNavigationResult> NavigateAsync(
            string regionName,
            string target,
            NavigationParameters parameters)
        {
            try
            {
                _logger.LogInformation($"导航到: {target} in {regionName}");

                var region = _regionManager.Regions[regionName];
                var result = await region.NavigationService.RequestNavigateAsync(target, parameters);

                if (result.Result == true)
                {
                    // 缓存Journal以便快速访问
                    _journalCache[regionName] = region.NavigationService.Journal;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"导航失败: {target} in {regionName}");
                throw;
            }
        }

        #endregion

        #region 导航历史

        public bool CanGoBack(string regionName)
        {
            var journal = GetJournal(regionName);
            return journal?.CanGoBack ?? false;
        }

        public bool CanGoForward(string regionName)
        {
            var journal = GetJournal(regionName);
            return journal?.CanGoForward ?? false;
        }

        public void GoBack(string regionName)
        {
            var journal = GetJournal(regionName);
            if (journal?.CanGoBack == true)
            {
                journal.GoBack();
                _logger.LogInformation($"导航后退 in {regionName}");
            }
        }

        public void GoForward(string regionName)
        {
            var journal = GetJournal(regionName);
            if (journal?.CanGoForward == true)
            {
                journal.GoForward();
                _logger.LogInformation($"导航前进 in {regionName}");
            }
        }

        public void ClearHistory(string regionName)
        {
            var journal = GetJournal(regionName);
            journal?.Clear();
            _logger.LogInformation($"清除导航历史 in {regionName}");
        }

        public IRegionNavigationJournal GetJournal(string regionName)
        {
            if (_journalCache.TryGetValue(regionName, out var cachedJournal))
            {
                return cachedJournal;
            }

            if (_regionManager.Regions.ContainsRegionWithName(regionName))
            {
                var region = _regionManager.Regions[regionName];
                var journal = region.NavigationService.Journal;
                _journalCache[regionName] = journal;
                return journal;
            }

            return null;
        }

        #endregion

        #region 全局导航

        public async Task<IRegionNavigationResult> NavigateToWorkstationAsync(string workbenchName)
        {
            var parameters = new NavigationParameters
            {
                { "WorkstationName", workbenchName }
            };

            return await NavigateAsync(RegionNames.MainContentRegion, workbenchName, parameters);
        }

        public async Task<IRegionNavigationResult> NavigateToModuleAsync(
            string moduleName,
            NavigationParameters parameters = null)
        {
            parameters ??= new NavigationParameters();
            parameters.Add("ModuleName", moduleName);

            return await NavigateAsync(RegionNames.WorkstationContentRegion, moduleName, parameters);
        }

        #endregion
    }
}
```

### 3. 更新XAML视图支持区域

#### 3.1 MainWindow.xaml
```xml
<!-- 修改文件：src/Client/Desktop/Shell/Views/MainWindow.xaml -->
<Window x:Class="LYBT.Desktop.Shell.Views.MainWindow"
        xmlns:prism="http://prismlibrary.com/"
        Title="凌隐宝堂中医诊所管理系统">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- 工具栏 -->
            <RowDefinition Height="*"/>    <!-- 主内容 -->
            <RowDefinition Height="Auto"/> <!-- 状态栏 -->
        </Grid.RowDefinitions>

        <!-- 工具栏区域 -->
        <ContentControl Grid.Row="0"
                       prism:RegionManager.RegionName="{x:Static constants:RegionNames.ToolBarRegion}"/>

        <!-- 主内容区域 -->
        <ContentControl Grid.Row="1"
                       prism:RegionManager.RegionName="{x:Static constants:RegionNames.MainContentRegion}"/>

        <!-- 状态栏区域 -->
        <ContentControl Grid.Row="2"
                       prism:RegionManager.RegionName="{x:Static constants:RegionNames.StatusBarRegion}"/>
    </Grid>
</Window>
```

#### 3.2 添加导航工具栏
```xml
<!-- 新建文件：src/Client/Desktop/Shell/Views/NavigationToolBar.xaml -->
<UserControl x:Class="LYBT.Desktop.Shell.Views.NavigationToolBar">
    <ToolBar>
        <!-- 导航按钮 -->
        <Button Command="{Binding GoBackCommand}"
                ToolTip="后退">
            <Image Source="/Resources/Images/back.png" Width="16" Height="16"/>
        </Button>

        <Button Command="{Binding GoForwardCommand}"
                ToolTip="前进">
            <Image Source="/Resources/Images/forward.png" Width="16" Height="16"/>
        </Button>

        <Separator/>

        <!-- 主页按钮 -->
        <Button Command="{Binding GoHomeCommand}"
                ToolTip="主页">
            <Image Source="/Resources/Images/home.png" Width="16" Height="16"/>
        </Button>

        <Separator/>

        <!-- 面包屑导航 -->
        <ItemsControl ItemsSource="{Binding NavigationPath}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <StackPanel Orientation="Horizontal"/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text=" > " Margin="3,0"/>
                        <Button Content="{Binding DisplayName}"
                               Command="{Binding NavigateCommand}"
                               Style="{StaticResource LinkButtonStyle}"/>
                    </StackPanel>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </ToolBar>
</UserControl>
```

### 4. ViewModel支持导航历史

#### 4.1 NavigationAwareViewModel基类
```csharp
// 新建文件：src/Client/Desktop/Core/ViewModels/Base/NavigationAwareViewModel.cs
namespace LYBT.Desktop.Core.ViewModels.Base
{
    public abstract class NavigationAwareViewModel : UnifiedViewModelBase, INavigationAware
    {
        private IRegionNavigationService _navigationService;
        protected IEnhancedNavigationService EnhancedNavigation { get; }

        protected NavigationAwareViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILogger logger,
            IEnhancedNavigationService enhancedNavigation)
            : base(regionManager, eventAggregator, logger)
        {
            EnhancedNavigation = enhancedNavigation ?? throw new ArgumentNullException(nameof(enhancedNavigation));
        }

        #region INavigationAware

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            _navigationService = navigationContext.NavigationService;

            // 更新导航命令状态
            GoBackCommand.RaiseCanExecuteChanged();
            GoForwardCommand.RaiseCanExecuteChanged();

            // 记录导航参数
            NavigationParameters = navigationContext.Parameters;

            // 派生类可重写此方法处理导航参数
            HandleNavigationParameters(navigationContext.Parameters);
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 保存状态
            SaveState();
        }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 默认创建新实例
            return true;
        }

        #endregion

        #region 导航命令

        private DelegateCommand _goBackCommand;
        public DelegateCommand GoBackCommand =>
            _goBackCommand ??= new DelegateCommand(ExecuteGoBack, CanGoBack);

        private void ExecuteGoBack()
        {
            _navigationService?.Journal.GoBack();
        }

        private bool CanGoBack()
        {
            return _navigationService?.Journal.CanGoBack ?? false;
        }

        private DelegateCommand _goForwardCommand;
        public DelegateCommand GoForwardCommand =>
            _goForwardCommand ??= new DelegateCommand(ExecuteGoForward, CanGoForward);

        private void ExecuteGoForward()
        {
            _navigationService?.Journal.GoForward();
        }

        private bool CanGoForward()
        {
            return _navigationService?.Journal.CanGoForward ?? false;
        }

        #endregion

        #region 辅助方法

        protected NavigationParameters NavigationParameters { get; private set; }

        protected virtual void HandleNavigationParameters(NavigationParameters parameters)
        {
            // 子类实现参数处理逻辑
        }

        protected virtual void SaveState()
        {
            // 子类实现状态保存逻辑
        }

        protected Task<IRegionNavigationResult> NavigateToAsync(string target)
        {
            return NavigateToAsync(target, null);
        }

        protected Task<IRegionNavigationResult> NavigateToAsync(string target, NavigationParameters parameters)
        {
            if (_navigationService == null)
                throw new InvalidOperationException("NavigationService尚未初始化");

            return _navigationService.RequestNavigateAsync(target, parameters);
        }

        #endregion
    }
}
```

#### 4.2 具体ViewModel实现示例
```csharp
// 修改文件：src/Client/Desktop/Modules/Patients/ViewModels/PatientDetailViewModel.cs
namespace LYBT.Desktop.Modules.Patients.ViewModels
{
    public class PatientDetailViewModel : NavigationAwareViewModel
    {
        private readonly IPatientService _patientService;
        private PatientDto _currentPatient;

        public PatientDetailViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILogger<PatientDetailViewModel> logger,
            IEnhancedNavigationService enhancedNavigation,
            IPatientService patientService)
            : base(regionManager, eventAggregator, logger, enhancedNavigation)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            InitializeCommands();
        }

        #region 属性

        public PatientDto CurrentPatient
        {
            get => _currentPatient;
            set => SetProperty(ref _currentPatient, value);
        }

        #endregion

        #region 导航重写

        protected override void HandleNavigationParameters(NavigationParameters parameters)
        {
            if (parameters.TryGetValue<Guid>("PatientId", out var patientId))
            {
                LoadPatient(patientId);
            }
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 如果是同一个患者，重用实例
            if (navigationContext.Parameters.TryGetValue<Guid>("PatientId", out var patientId))
            {
                return CurrentPatient?.Id == patientId;
            }
            return false;
        }

        protected override void SaveState()
        {
            // 保存当前编辑状态
            if (HasUnsavedChanges)
            {
                _patientService.SaveDraft(CurrentPatient);
            }
        }

        #endregion

        #region 命令

        private DelegateCommand _viewConsultationHistoryCommand;
        public DelegateCommand ViewConsultationHistoryCommand =>
            _viewConsultationHistoryCommand ??= new DelegateCommand(ExecuteViewConsultationHistory);

        private async void ExecuteViewConsultationHistory()
        {
            var parameters = new NavigationParameters
            {
                { "PatientId", CurrentPatient.Id },
                { "ReturnView", "PatientDetailView" }
            };

            // 导航到诊疗历史
            await EnhancedNavigation.NavigateAsync(
                RegionNames.WorkstationContentRegion,
                "ConsultationHistoryView",
                parameters);
        }

        private DelegateCommand _createPrescriptionCommand;
        public DelegateCommand CreatePrescriptionCommand =>
            _createPrescriptionCommand ??= new DelegateCommand(ExecuteCreatePrescription);

        private async void ExecuteCreatePrescription()
        {
            var parameters = new NavigationParameters
            {
                { "PatientId", CurrentPatient.Id },
                { "PatientName", CurrentPatient.Name },
                { "Mode", "Create" }
            };

            // 导航到处方创建
            await NavigateToAsync("PrescriptionEditView", parameters);
        }

        #endregion

        #region 私有方法

        private async void LoadPatient(Guid patientId)
        {
            try
            {
                IsBusy = true;
                var result = await _patientService.GetByIdAsync(patientId);
                if (result.IsSuccess)
                {
                    CurrentPatient = result.Data;
                }
                else
                {
                    ShowError("加载患者信息失败", result.Message);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion
    }
}
```

### 5. 注册服务

#### 5.1 在App.xaml.cs中注册
```csharp
// 修改文件：src/Client/Desktop/Shell/App.xaml.cs
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // ... 其他注册

    // 注册增强导航服务
    containerRegistry.RegisterSingleton<IEnhancedNavigationService, EnhancedNavigationService>();

    // 注册导航工具栏
    containerRegistry.RegisterForNavigation<NavigationToolBar>();

    // ... 其他注册
}

protected override void OnInitialized()
{
    base.OnInitialized();

    // 注册导航工具栏到区域
    var regionManager = Container.Resolve<IRegionManager>();
    regionManager.RegisterViewWithRegion(RegionNames.ToolBarRegion, typeof(NavigationToolBar));
}
```

## 测试验证

### 单元测试
```csharp
[TestClass]
public class EnhancedNavigationServiceTests
{
    private Mock<IRegionManager> _regionManagerMock;
    private Mock<ILogger<EnhancedNavigationService>> _loggerMock;
    private EnhancedNavigationService _navigationService;

    [TestInitialize]
    public void Setup()
    {
        _regionManagerMock = new Mock<IRegionManager>();
        _loggerMock = new Mock<ILogger<EnhancedNavigationService>>();
        _navigationService = new EnhancedNavigationService(
            _regionManagerMock.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task NavigateAsync_ValidTarget_ShouldNavigate()
    {
        // Arrange
        var regionName = RegionNames.MainContentRegion;
        var target = "TestView";

        // Setup region mock
        var regionMock = new Mock<IRegion>();
        var navigationServiceMock = new Mock<IRegionNavigationService>();
        var navigationResult = new RegionNavigationResult(null, true);

        navigationServiceMock
            .Setup(x => x.RequestNavigateAsync(It.IsAny<Uri>(), It.IsAny<NavigationParameters>()))
            .ReturnsAsync(navigationResult);

        regionMock.Setup(x => x.NavigationService).Returns(navigationServiceMock.Object);

        var regions = new RegionCollection();
        regions.Add(regionName, regionMock.Object);
        _regionManagerMock.Setup(x => x.Regions).Returns(regions);

        // Act
        var result = await _navigationService.NavigateAsync(regionName, target);

        // Assert
        Assert.IsTrue(result.Result);
    }

    [TestMethod]
    public void CanGoBack_WithHistory_ShouldReturnTrue()
    {
        // Arrange
        var regionName = RegionNames.MainContentRegion;
        var journalMock = new Mock<IRegionNavigationJournal>();
        journalMock.Setup(x => x.CanGoBack).Returns(true);

        // Setup region
        var regionMock = new Mock<IRegion>();
        var navigationServiceMock = new Mock<IRegionNavigationService>();
        navigationServiceMock.Setup(x => x.Journal).Returns(journalMock.Object);
        regionMock.Setup(x => x.NavigationService).Returns(navigationServiceMock.Object);

        var regions = new RegionCollection();
        regions.Add(regionName, regionMock.Object);
        _regionManagerMock.Setup(x => x.Regions).Returns(regions);

        // Act
        var canGoBack = _navigationService.CanGoBack(regionName);

        // Assert
        Assert.IsTrue(canGoBack);
    }
}
```

## 实施步骤

1. **创建基础结构**（Day 1）
   - 创建RegionNames常量类
   - 创建IEnhancedNavigationService接口
   - 实现EnhancedNavigationService

2. **更新视图**（Day 2）
   - 修改MainWindow.xaml添加Region定义
   - 创建NavigationToolBar
   - 更新工作台视图

3. **更新ViewModels**（Day 3-4）
   - 创建NavigationAwareViewModel基类
   - 更新现有ViewModel继承新基类
   - 实现导航参数处理

4. **测试验证**（Day 5）
   - 编写单元测试
   - 集成测试
   - 修复问题

## 验收标准

- [ ] RegionNames常量定义完成
- [ ] EnhancedNavigationService实现并注册
- [ ] 所有主要视图支持Region
- [ ] NavigationToolBar显示并工作
- [ ] 导航历史前进/后退功能正常
- [ ] 至少5个核心ViewModel支持导航历史
- [ ] 导航参数传递正确
- [ ] 面包屑导航显示正确
- [ ] 单元测试覆盖率>80%

## 风险评估

- **风险等级**：中-高
- **技术风险**：区域管理可能影响现有布局
- **性能风险**：Journal缓存可能占用内存
- **回退方案**：保留原有导航逻辑作为后备

## 相关文档

- [Prism Region Navigation](https://prismlibrary.com/docs/region-navigation/index.html)
- [Navigation Journal](https://prismlibrary.com/docs/navigation/navigation-journal.html)