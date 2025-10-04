# LYBT Desktop Prism 架构重构计划

**文档版本**: 1.0
**创建日期**: 2025-10-01
**分析方法**: UltraThink 深度分析（22步）
**负责人**: Claude Code
**审批状态**: 待审批

---

## 📋 执行摘要

### 当前状态评估

**Prism 框架符合度**: 53%

| Prism 核心特性 | 使用情况 | 符合度 |
|---------------|---------|--------|
| 依赖注入 (DI) | ✅ 使用 DryIoc | 90% |
| 模块化 | ✅ 8个业务模块 | 70% |
| MVVM 基础 | ✅ BindableBase | 85% |
| ViewModelLocator | ✅ 自动发现 | 90% |
| **Region 管理** | ❌ **未使用** | **0%** |
| **导航服务** | ❌ **未使用** | **0%** |
| **对话框服务** | ❌ **非标准** | **10%** |
| 事件聚合器 | ❓ 部分使用 | 50% |
| 命令模式 | ✅ DelegateCommand | 85% |

### 核心问题

1. 🔴 **完全缺失 Region 管理系统** - 严重性：Critical
   - 无法动态视图组合
   - 无法使用 Prism 导航机制
   - 无法管理视图生命周期

2. 🔴 **未使用标准导航服务** - 严重性：Critical
   - 无法使用导航参数传递
   - 无法使用导航历史
   - 无法实现 INavigationAware

3. 🟡 **对话框服务非标准** - 严重性：High
   - 使用传统 Window.ShowDialog()
   - 未实现 IDialogAware
   - 耦合度高，难以测试

4. 🟡 **模块依赖未声明** - 严重性：High
   - 未使用 [ModuleDependency] 属性
   - 加载顺序不可控
   - 潜在循环依赖风险

5. 🟢 **Service Locator 反模式** - 严重性：Medium
   - App.xaml.cs 中少量使用
   - 仅限启动代码，影响有限

### 重构目标

**将 Prism 符合度从 53% 提升到 95%+**

**解锁核心能力**：
- ✅ Region 动态视图组合
- ✅ 标准化导航和参数传递
- ✅ 统一对话框管理
- ✅ 声明式模块依赖
- ✅ 完整的生命周期管理

---

## 🎯 重构方案

### 总体策略

**渐进式迁移** - 保持新旧系统并存，降低风险

- 创建 Feature Toggle 控制切换
- 分阶段迁移，每阶段可独立验证
- 保留回滚方案

### 三阶段路线图

```
Phase 1: 基础重构 (2-3周)
  ├─ 模块依赖声明
  ├─ 事件聚合器标准化
  └─ Service Locator 消除

Phase 2: 架构升级 (6-8周)
  ├─ Region 系统引入
  ├─ 导航服务实现
  ├─ 试点模块迁移
  └─ 全量模块迁移

Phase 3: 标准化完成 (2-3周)
  ├─ 对话框服务迁移
  ├─ 集成测试
  └─ 文档更新
```

**总工期**: 10-13周（2.5-3个月）

---

## 📅 Phase 1: 基础重构（2-3周）

### 目标

建立 Prism 最佳实践的基础设施

### Task 1.1: 声明模块依赖关系（1天）

**当前问题**：
```csharp
// 仅通过注释说明依赖
// 方剂管理 - 依赖药材
moduleCatalog.AddModule<FormulaModule>(InitializationMode.OnDemand);
```

**重构方案**：
```csharp
// 使用 Prism 属性声明依赖
[Module(ModuleName = "FormulaModule")]
[ModuleDependency("HerbsModule")]
public class FormulaModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册服务
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化
    }
}
```

**模块依赖关系图**：
```
Auth
  └─ (无依赖)

Users
  └─ Auth

Patients
  └─ Users

Consultation
  └─ Patients

MedicalCase
  ├─ Patients
  └─ Consultation

Herbs
  └─ (无依赖)

Formula
  └─ Herbs

Prescriptions
  ├─ Patients
  ├─ Herbs
  ├─ Formula
  └─ Consultation
```

**验收标准**：
- ✅ 所有模块类添加 [Module] 和 [ModuleDependency] 属性
- ✅ 模块按正确顺序加载（通过日志验证）
- ✅ 无循环依赖错误

### Task 1.2: 标准化事件聚合器使用（2天）

**验证步骤**：
1. 审查 `Core/Events/` 中的事件定义
2. 确保事件继承自 `PubSubEvent<T>`
3. 验证所有模块通过 `IEventAggregator` 通信

**标准化示例**：
```csharp
// 定义事件
public class PatientSelectedEvent : PubSubEvent<Guid> { }

// 发布事件
_eventAggregator.GetEvent<PatientSelectedEvent>().Publish(patientId);

// 订阅事件
_eventAggregator.GetEvent<PatientSelectedEvent>()
    .Subscribe(OnPatientSelected, ThreadOption.UIThread);
```

**验收标准**：
- ✅ 所有自定义事件继承自 `PubSubEvent<T>`
- ✅ 模块间通信统一使用 `IEventAggregator`
- ✅ 线程调度正确配置（ThreadOption.UIThread）

### Task 1.3: 消除 Service Locator 反模式（1天）

**当前问题**：
```csharp
// App.xaml.cs
protected override void OnInitialized()
{
    _bootstrapper = Container.Resolve<IApplicationBootstrapper>();
    _bootstrapper.InitializeErrorHandlingService();
}
```

**重构方案**：
```csharp
// 创建应用启动主机服务
public class PrismApplicationHost
{
    private readonly IApplicationBootstrapper _bootstrapper;

    public PrismApplicationHost(IApplicationBootstrapper bootstrapper)
    {
        _bootstrapper = bootstrapper;
    }

    public void Initialize()
    {
        _bootstrapper.InitializeErrorHandlingService();
        _bootstrapper.InitializeSimplifiedModuleCoordinator();
    }

    public async Task InitializeAsync()
    {
        await _bootstrapper.InitializeCoreServicesAsync();
        await _bootstrapper.InitializeApplicationWarmupAsync();
    }
}

// App.xaml.cs
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterSingleton<PrismApplicationHost>();
}

protected override void OnInitialized()
{
    var host = Container.Resolve<PrismApplicationHost>();
    host.Initialize();
    _ = Task.Run(() => host.InitializeAsync());
}
```

**验收标准**：
- ✅ `Container.Resolve` 仅在 `CreateShell` 和 `OnInitialized` 中使用
- ✅ 其他所有依赖通过构造函数注入
- ✅ 编译通过，应用正常启动

### Phase 1 总体验收标准

- ✅ 0 编译错误，0 警告
- ✅ 所有模块按依赖顺序加载
- ✅ 事件聚合器标准化
- ✅ Service Locator 使用最小化
- ✅ 应用功能无回归

---

## 📅 Phase 2: 架构升级（6-8周）

### 目标

引入 Prism Region 管理系统和标准导航服务

### Step 2.1: 重构 MainWindow 为 Region 容器（1周）

**当前结构（推测）**：
```xml
<Window>
    <Grid>
        <!-- 可能是单一 ContentControl -->
        <ContentControl Content="{Binding CurrentView}" />
    </Grid>
</Window>
```

**重构后结构**：
```xml
<Window x:Class="LYBT.Desktop.Shell.Views.MainWindow"
        xmlns:prism="http://prismlibrary.com/"
        prism:ViewModelLocator.AutoWireViewModel="True">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>   <!-- 顶部导航栏 -->
            <RowDefinition Height="*"/>      <!-- 主内容区 -->
            <RowDefinition Height="Auto"/>   <!-- 状态栏 -->
        </Grid.RowDefinitions>

        <!-- 顶部导航区域 -->
        <ContentControl Grid.Row="0"
                        prism:RegionManager.RegionName="{x:Static inf:RegionNames.NavigationRegion}" />

        <!-- 主内容区域 -->
        <ContentControl Grid.Row="1"
                        prism:RegionManager.RegionName="{x:Static inf:RegionNames.MainContentRegion}" />

        <!-- 状态栏区域 -->
        <ContentControl Grid.Row="2"
                        prism:RegionManager.RegionName="{x:Static inf:RegionNames.StatusBarRegion}" />
    </Grid>
</Window>
```

**定义 Region 常量**：
```csharp
// Infrastructure/Constants/RegionNames.cs
public static class RegionNames
{
    public const string NavigationRegion = "NavigationRegion";
    public const string MainContentRegion = "MainContentRegion";
    public const string StatusBarRegion = "StatusBarRegion";
    public const string DialogRegion = "DialogRegion";
    public const string WorkstationRegion = "WorkstationRegion";
}
```

**验收标准**：
- ✅ MainWindow 包含至少 2 个 Region
- ✅ Region 名称使用常量定义
- ✅ RegionManager 正确初始化

### Step 2.2: 试点模块迁移到 Region（2周）

**选择 Herbs 模块作为试点**（最简单，无复杂依赖）

**模块注册视图**：
```csharp
public class HerbsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册服务
        containerRegistry.Register<IHerbsQueryService, HerbsQueryService>();
        containerRegistry.Register<IHerbsBusinessService, HerbsBusinessService>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<HerbsListView>();
        containerRegistry.RegisterForNavigation<HerbDetailsView>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var regionManager = containerProvider.Resolve<IRegionManager>();

        // 可选：使用视图发现方式注册
        regionManager.RegisterViewWithRegion(RegionNames.MainContentRegion,
            typeof(HerbsListView));
    }
}
```

**ViewModel 实现 INavigationAware**：
```csharp
public class HerbsListViewModel : BindableBase, INavigationAware
{
    private readonly IHerbsQueryService _queryService;
    private readonly IRegionManager _regionManager;

    public HerbsListViewModel(IHerbsQueryService queryService, IRegionManager regionManager)
    {
        _queryService = queryService;
        _regionManager = regionManager;

        ViewDetailsCommand = new DelegateCommand<Guid?>(OnViewDetails);
    }

    public DelegateCommand<Guid?> ViewDetailsCommand { get; }

    private void OnViewDetails(Guid? herbId)
    {
        if (herbId == null) return;

        var parameters = new NavigationParameters
        {
            { "HerbId", herbId.Value }
        };

        _regionManager.RequestNavigate(RegionNames.MainContentRegion,
            nameof(HerbDetailsView), parameters);
    }

    // INavigationAware 实现
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 页面激活时加载数据
        LoadHerbsAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // 决定是否重用此实例
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // 页面离开时清理资源
    }
}
```

**验收标准**：
- ✅ Herbs 模块视图注册到 Region
- ✅ 导航通过 RegionManager 执行
- ✅ 导航参数传递正确
- ✅ ViewModel 实现 INavigationAware

### Step 2.3: 全量模块迁移到 Region（3-4周）

**按优先级迁移顺序**：
1. Week 1: Auth, Users（核心模块）
2. Week 2: Patients, Herbs（已完成）, Formula
3. Week 3: Consultation, MedicalCase
4. Week 4: Prescriptions（最复杂）

**每个模块迁移清单**：
- [ ] 注册视图到 Region (`RegisterForNavigation`)
- [ ] ViewModel 实现 `INavigationAware`
- [ ] 导航调用改用 `RegionManager.RequestNavigate`
- [ ] 导航参数使用 `NavigationParameters`
- [ ] 测试导航流程
- [ ] 更新模块文档

**验收标准**：
- ✅ 8个业务模块全部迁移
- ✅ 所有模块间导航使用 RegionManager
- ✅ 所有 ViewModel 实现 INavigationAware
- ✅ 导航参数传递标准化
- ✅ 应用功能无回归

### Step 2.4: 实现导航历史和生命周期（1周）

**导航历史（Journal）**：
```csharp
public class PatientDetailsViewModel : BindableBase, INavigationAware
{
    private IRegionNavigationService _navigationService;

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _navigationService = navigationContext.NavigationService;
    }

    public DelegateCommand GoBackCommand => new DelegateCommand(
        () => _navigationService.Journal.GoBack(),
        () => _navigationService.Journal.CanGoBack);
}
```

**视图生命周期管理**：
```csharp
public class PatientsListViewModel : BindableBase, IRegionMemberLifetime
{
    // 控制视图是否在 Region 中保持活动
    public bool KeepAlive => true;  // 保持实例，不销毁
}
```

**验收标准**：
- ✅ 支持后退导航（GoBack）
- ✅ 关键视图实现 IRegionMemberLifetime
- ✅ 导航历史正确记录

### Phase 2 总体验收标准

- ✅ MainWindow 使用 Region 布局
- ✅ 所有模块视图注册到 Region
- ✅ 导航通过 RegionManager 或 INavigationService
- ✅ 导航参数传递标准化
- ✅ ViewModel 实现 INavigationAware
- ✅ 支持导航历史
- ✅ 应用功能无回归
- ✅ 性能无明显下降

---

## 📅 Phase 3: 对话框标准化（2-3周）

### 目标

迁移所有对话框到 Prism IDialogService

### Step 3.1: 对话框清单和优先级（1天）

**现有对话框**（从 Prescriptions 模块推断）：
1. FormulaTemplateDialog
2. HerbSelectionDialog
3. PrescriptionEditorDialog
4. SelectFormulaDialog
5. （其他模块约 6-10 个对话框）

**迁移优先级**：
- P1: 简单确认对话框（Message Box 替代）
- P2: 表单对话框（单一数据输入）
- P3: 复杂选择对话框（列表选择）
- P4: 编辑对话框（多字段编辑）

### Step 3.2: 迁移对话框视图（1周）

**从 Window 迁移到 UserControl**：

**旧实现**：
```xml
<!-- HerbSelectionDialog.xaml -->
<Window x:Class="LYBT.Desktop.Prescriptions.Views.HerbSelectionDialog"
        Title="选择中药材" Height="600" Width="800">
    <Grid>
        <!-- 对话框内容 -->
    </Grid>
</Window>
```

**新实现**：
```xml
<!-- HerbSelectionDialog.xaml -->
<UserControl x:Class="LYBT.Desktop.Prescriptions.Views.HerbSelectionDialog"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">
    <prism:Dialog.WindowStyle>
        <Style TargetType="Window">
            <Setter Property="Title" Value="选择中药材"/>
            <Setter Property="Height" Value="600"/>
            <Setter Property="Width" Value="800"/>
        </Style>
    </prism:Dialog.WindowStyle>
    <Grid>
        <!-- 对话框内容保持不变 -->
    </Grid>
</UserControl>
```

**注册对话框**：
```csharp
// Module.RegisterTypes
containerRegistry.RegisterDialog<HerbSelectionDialog, HerbSelectionDialogViewModel>();
```

### Step 3.3: ViewModel 实现 IDialogAware（1周）

**标准实现模式**：
```csharp
public class HerbSelectionDialogViewModel : BindableBase, IDialogAware
{
    private readonly IHerbsQueryService _herbsQuery;

    public HerbSelectionDialogViewModel(IHerbsQueryService herbsQuery)
    {
        _herbsQuery = herbsQuery;

        ConfirmCommand = new DelegateCommand(OnConfirm, CanConfirm)
            .ObservesProperty(() => SelectedHerb);
        CancelCommand = new DelegateCommand(OnCancel);
    }

    // IDialogAware 必需属性
    public string Title => "选择中药材";

    // 对话框关闭事件
    public event Action<IDialogResult> RequestClose;

    // 对话框打开时
    public void OnDialogOpened(IDialogParameters parameters)
    {
        // 接收参数
        var category = parameters.GetValue<string>("Category");
        LoadHerbs(category);
    }

    // 对话框关闭时
    public void OnDialogClosed()
    {
        // 清理资源
    }

    // 是否可关闭
    public bool CanCloseDialog() => true;

    // 确认命令
    public DelegateCommand ConfirmCommand { get; }

    private void OnConfirm()
    {
        var result = new DialogResult(ButtonResult.OK, new DialogParameters
        {
            { "SelectedHerb", SelectedHerb }
        });
        RequestClose?.Invoke(result);
    }

    // 取消命令
    public DelegateCommand CancelCommand { get; }

    private void OnCancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    private bool CanConfirm() => SelectedHerb != null;

    private HerbDto _selectedHerb;
    public HerbDto SelectedHerb
    {
        get => _selectedHerb;
        set => SetProperty(ref _selectedHerb, value);
    }
}
```

### Step 3.4: 调用方迁移到 IDialogService（1周）

**旧方式**：
```csharp
var dialog = new HerbSelectionDialog();
dialog.DataContext = new HerbSelectionDialogViewModel(herbsQuery);
if (dialog.ShowDialog() == true)
{
    var selected = (dialog.DataContext as HerbSelectionDialogViewModel).SelectedHerb;
    // 处理选中的药材
}
```

**新方式**：
```csharp
public class PrescriptionComposerViewModel : BindableBase
{
    private readonly IDialogService _dialogService;

    public PrescriptionComposerViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        SelectHerbCommand = new DelegateCommand(OnSelectHerb);
    }

    public DelegateCommand SelectHerbCommand { get; }

    private void OnSelectHerb()
    {
        var parameters = new DialogParameters
        {
            { "Category", "补益药" }
        };

        _dialogService.ShowDialog("HerbSelectionDialog", parameters, result =>
        {
            if (result.Result == ButtonResult.OK)
            {
                var selected = result.Parameters.GetValue<HerbDto>("SelectedHerb");
                AddHerbToPrescription(selected);
            }
        });
    }
}
```

### Phase 3 总体验收标准

- ✅ 所有对话框迁移到 UserControl
- ✅ ViewModel 实现 IDialogAware
- ✅ 参数传递使用 DialogParameters
- ✅ 结果返回使用 IDialogResult
- ✅ 调用方使用 IDialogService
- ✅ 应用功能无回归
- ✅ 对话框样式一致性保持

---

## 🔄 回滚和应急方案

### 分阶段回滚策略

**Phase 1 回滚**（低风险）：
- 移除模块依赖属性
- 恢复原有事件实现
- 回滚成本：1天

**Phase 2 回滚**（高风险）：
- 保留 Git 分支 `feature/region-navigation`
- 主分支保持原有实现
- 使用 Feature Toggle 控制切换
  ```csharp
  if (Configuration.UseRegionNavigation)
  {
      // 新的 Region 导航
  }
  else
  {
      // 旧的导航逻辑
  }
  ```
- 回滚成本：2-3天

**Phase 3 回滚**（中风险）：
- 对话框适配器同时支持新旧方式
- 逐个对话框回滚
- 回滚成本：3-5天

### 应急预案

**场景1：Region 导航性能问题**
- 应急方案：优化 Region 激活策略
- 降级方案：部分模块保留旧导航
- 预估概率：20%

**场景2：模块加载依赖冲突**
- 应急方案：调整模块加载顺序
- 降级方案：移除部分依赖声明
- 预估概率：30%

**场景3：对话框迁移兼容性问题**
- 应急方案：保留对话框适配器
- 降级方案：暂时不迁移复杂对话框
- 预估概率：40%

---

## 📊 资源需求和时间表

### 资源配置

**开发团队**：
- WPF/Prism 高级开发工程师 × 1-2
- 测试工程师 × 1（Phase 2 后半程）
- 架构师（兼职指导）

**技能要求**：
- 熟悉 WPF 和 XAML
- 熟悉 Prism 框架（Region、导航、对话框）
- 熟悉依赖注入和 MVVM 模式
- 具备重构经验

### 详细时间表

| 阶段 | 任务 | 工作量 | 时间线 |
|------|-----|--------|--------|
| **准备** | 代码审查、环境搭建 | 40h | Week 0 |
| **Phase 1** | 基础重构 | 80-120h | Week 1-3 |
| - Task 1.1 | 模块依赖声明 | 8h | Day 1-2 |
| - Task 1.2 | 事件聚合器标准化 | 16h | Day 3-4 |
| - Task 1.3 | Service Locator 消除 | 8h | Day 5 |
| - 测试验证 | 集成测试和回归测试 | 48h | Week 2-3 |
| **Phase 2** | 架构升级 | 240-320h | Week 4-11 |
| - Step 2.1 | MainWindow 重构 | 40h | Week 4 |
| - Step 2.2 | 试点模块迁移（Herbs） | 40h | Week 5-6 |
| - Step 2.3 | 全量模块迁移 | 120h | Week 7-10 |
| - Step 2.4 | 导航历史和生命周期 | 40h | Week 11 |
| **Phase 3** | 对话框标准化 | 80-120h | Week 12-13 |
| - Step 3.1 | 对话框清单 | 8h | Day 1 |
| - Step 3.2 | 视图迁移 | 40h | Week 12 |
| - Step 3.3 | ViewModel IDialogAware | 40h | Week 12-13 |
| - Step 3.4 | 调用方迁移 | 40h | Week 13 |
| **总计** | | **440-560h** | **13周** |

### 成本效益分析

**投入成本**：
- 人力成本：约 520 小时（13周 × 40小时/周）
- 测试成本：额外 20% 时间（约 100 小时）
- 风险缓冲：20% 时间（约 120 小时）
- **总计**：约 740 工时

**预期收益**：
- ✅ 架构标准化，降低长期维护成本（年度节省约 200 工时）
- ✅ 提升可测试性，减少 bug（预计减少 30% 缺陷）
- ✅ 解锁 Prism 高级特性（Region 组合、导航管理）
- ✅ 团队技能提升，提高开发效率
- ✅ 符合 Prism 最佳实践，便于新人上手

**ROI 评估**：
- 第 1 年 ROI：约 50%（成本回收一半）
- 第 2 年 ROI：约 150%（完全回收并产生收益）
- 长期价值：极高（技术债清零，架构可持续）

---

## 🎯 验收标准

### Phase 1 验收标准

- [ ] 所有模块类添加 `[Module]` 和 `[ModuleDependency]` 属性
- [ ] 模块按正确依赖顺序加载（通过日志验证）
- [ ] 无循环依赖错误
- [ ] 所有自定义事件继承自 `PubSubEvent<T>`
- [ ] 模块间通信统一使用 `IEventAggregator`
- [ ] `Container.Resolve` 仅在必要位置使用
- [ ] 0 编译错误，0 警告
- [ ] 应用功能无回归

### Phase 2 验收标准

- [ ] MainWindow 定义至少 2 个 Region
- [ ] 所有业务模块视图注册到 Region
- [ ] 导航通过 `RegionManager.RequestNavigate` 执行
- [ ] 所有 ViewModel 实现 `INavigationAware` 接口
- [ ] 导航参数使用 `NavigationParameters` 传递
- [ ] 支持导航历史（Journal.GoBack）
- [ ] 关键视图实现 `IRegionMemberLifetime`
- [ ] 应用功能无回归
- [ ] 性能无明显下降（启动时间 < 3秒）

### Phase 3 验收标准

- [ ] 所有对话框迁移到 `UserControl`
- [ ] 所有对话框 ViewModel 实现 `IDialogAware`
- [ ] 对话框参数使用 `DialogParameters` 传递
- [ ] 对话框结果使用 `IDialogResult` 返回
- [ ] 调用方使用 `IDialogService.ShowDialog`
- [ ] 对话框样式一致性保持
- [ ] 应用功能无回归

### 最终验收标准

- [ ] **Prism 符合度达到 95%+**
- [ ] 所有核心 Prism 特性正确使用
- [ ] 代码质量：0 编译错误，0 警告
- [ ] 性能：启动时间无回归
- [ ] 稳定性：通过 100% 集成测试
- [ ] 文档：更新所有模块文档和架构文档
- [ ] 培训：团队成员理解新架构

---

## 📚 相关文档

### Prism 官方文档
- [Prism 官方文档](https://docs.prismlibrary.com/)
- [Region 管理指南](https://docs.prismlibrary.com/docs/wpf/regions/region-manager)
- [导航服务文档](https://docs.prismlibrary.com/docs/wpf/navigation/basic-region-navigation)
- [对话框服务文档](https://docs.prismlibrary.com/docs/wpf/dialog-service)

### 项目文档
- [Desktop 架构说明](../src/Client/Desktop/README.md)
- [双层架构标准](../src/Client/Desktop/TWO_LAYER_ARCHITECTURE_STANDARD.md)
- [模块化设计文档](./modules/README.md)

### 重构记录
- 本计划将生成 GitHub Issue 并跟踪实施进度

---

## 📝 附录

### A. Prism 最佳实践检查清单

#### 依赖注入
- [x] 使用构造函数注入
- [ ] 避免 Service Locator 模式
- [x] 服务接口和实现分离

#### 模块化
- [x] 实现 `IModule` 接口
- [ ] 使用 `[Module]` 和 `[ModuleDependency]` 属性
- [x] 模块独立注册服务

#### MVVM
- [x] ViewModel 继承 `BindableBase`
- [x] 使用 `SetProperty` 通知属性变更
- [x] 使用 `DelegateCommand`

#### Region 管理
- [ ] 定义 Region 常量
- [ ] 使用 `RegisterViewWithRegion` 或 `RegisterForNavigation`
- [ ] ViewModel 实现 `INavigationAware`

#### 导航
- [ ] 使用 `IRegionManager` 或 `INavigationService`
- [ ] 导航参数使用 `NavigationParameters`
- [ ] 实现导航历史

#### 对话框
- [ ] 使用 `IDialogService`
- [ ] ViewModel 实现 `IDialogAware`
- [ ] 参数和结果使用 Prism 标准

#### 事件聚合器
- [x] 事件继承 `PubSubEvent<T>`
- [x] 使用 `IEventAggregator` 发布和订阅
- [ ] 正确配置线程选项

### B. 模块迁移清单模板

```markdown
## {模块名称} 迁移清单

### 视图注册
- [ ] ListView 注册到 Region
- [ ] DetailsView 注册到 Region
- [ ] 其他视图...

### ViewModel 改造
- [ ] ListView ViewModel 实现 INavigationAware
- [ ] DetailsView ViewModel 实现 INavigationAware
- [ ] 导航调用改用 RegionManager

### 对话框迁移
- [ ] Dialog1 迁移到 IDialogService
- [ ] Dialog2 迁移到 IDialogService

### 测试验证
- [ ] 导航流程测试
- [ ] 参数传递测试
- [ ] 对话框功能测试
- [ ] 回归测试

### 文档更新
- [ ] 更新模块 README
- [ ] 更新 API 调用说明
```

---

**文档状态**: ✅ 完成
**下一步**: 创建 GitHub Issue 并分配任务
