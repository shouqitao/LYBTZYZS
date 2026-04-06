# LYBT.Desktop.Clinical

> 临床医生角色模块 | 诊疗工作台 | 今日统计 + 开始看诊

## 项目定位

- **层级**: Desktop Roles
- **职责**: 提供临床医生角色专属工作台,核心功能为"开始看诊"入口,展示今日接诊统计和待处理病案数量,优化医生日常诊疗工作效率
- **状态**: Active

## 目录结构

```
LYBT.Desktop.Clinical/
├── ClinicalModule.cs              # Prism模块注册
├── ViewModels/
│   └── ClinicalHomeViewModel.cs   # 医生工作台ViewModel(统计+看诊)
└── Views/
    ├── ClinicalHomeView.xaml       # 医生工作台视图
    └── ClinicalHomeView.xaml.cs    # 视图后置代码
```

## 核心组件

| 名称 | 说明 |
|------|------|
| ClinicalModule | Prism模块注册,自动发现Views和ViewModels |
| ClinicalHomeViewModel | 今日统计(TodayConsultationCount/PendingCaseCount) + StartMedicalCaseCommand |
| ClinicalHomeView | 医生工作台UI,包含统计卡片和"开始看诊"按钮 |

## 设计依据

临床工作台采用"统计仪表盘 + 核心操作"模式:顶部展示今日接诊数和待处理病案的实时统计,中部提供"开始看诊"核心按钮。与Admin模块的多导航枢纽不同,Clinical模块聚焦单一诊疗流程入口,减少医生操作步骤。统计数据在OnNavigatedTo时自动刷新,确保每次进入页面看到最新数据。

## 依赖关系

### 依赖
- LYBT.Desktop.Foundation (Desktop端基础类型和接口)
- LYBT.Desktop.Infrastructure (区域管理、导航服务)
- LYBT.Desktop.Models (ViewModelBase基类)
- LYBT.Desktop.Contracts (区域名称常量)
- LYBT.Shared.Models (共享DTO模型,病案数据)

### 被依赖
- LYBT.Desktop.Shell (Prism模块注册,加载临床医生模块)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 按精简规范重写README,代码示例迁移至CLAUDE.md |
| 2025-10-29 | 初始版本 |

## 开发笔记

# LYBT.Desktop.Clinical 代码知识

## 架构决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 统计仪表盘+单一核心操作模式 | 医生工作台聚焦诊疗流程,减少操作步骤 | 2025-10-29 |
| OnNavigatedTo自动刷新统计 | 每次进入页面保证数据最新,无需手动刷新 | 2025-10-29 |
| ExecuteSafelyAsync包装异步操作 | 自动处理异常和加载状态,避免重复样板代码 | 2025-10-29 |
| Composite ViewModel Pattern | MedicalCaseWorkspaceViewModel重构为子VM组合模式(ConsultationEditor/PrescriptionEditor/Commands/PendingQueue/CardReader)，消除Handler回调 | 2026-03-05 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| OnNavigatedTo中async调用无法await | Prism的INavigationAware.OnNavigatedTo是同步方法 | 使用 `_ = LoadTodayStatistics()` 触发fire-and-forget,异常由ExecuteSafelyAsync捕获 |
| 统计数据闪烁 | 每次导航都触发API调用 | 可考虑短时缓存或仅在数据变更事件时刷新 |

## 代码示例

### Shell层加载模块(App.xaml.cs)

```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    moduleCatalog.AddModule<ClinicalModule>();
}
```

### 导航到医生工作台

```csharp
// ClinicalHomeViewModel 使用 INavigationCoordinator 进行导航
_navigationCoordinator.NavigateTo("PatientSelectionView");
```

### ViewModel 当前架构

```csharp
// ClinicalHomeViewModel 继承 NavigableViewModelBase，使用 [RelayCommand] 属性
public partial class ClinicalHomeViewModel : NavigableViewModelBase, INavigationAware
{
    private readonly IAuthenticationService _authService;
    private readonly INavigationCoordinator _navigationCoordinator;

    // 使用 CommunityToolkit.Mvvm [ObservableProperty] 生成属性
    [ObservableProperty] private int _todayConsultationCount;
    [ObservableProperty] private int _pendingCaseCount;

    [RelayCommand]
    private void StartMedicalCase()
    {
        _navigationCoordinator.NavigateTo("PatientSelectionView");
    }
}
```

## 代码文件结构

### 模块注册 (ClinicalModule.cs, 42行)

Prism模块入口，`[Module(ModuleName = nameof(ClinicalModule))]`。
模块依赖: `PatientsModule`, `MedicalCaseModule`。

注册内容:
- ViewModel: `ClinicalHomeViewModel`, `PatientSelectionViewModel`, `MedicalCaseWorkspaceViewModel`
- 导航视图: `ClinicalHomeView`, `PatientSelectionView`, `MedicalCaseWorkspaceView`
- 薄包装管理视图: `HerbManagementView`, `FormulaManagementView`, `PatientManagementView`, `MedicalCaseManagementView` (复用业务模块Control，无独立ViewModel；权限区分: 诊所共享数据只读、医生自创数据可管理)

### ViewModels/ClinicalHomeViewModel.cs (299行)

医生工作台主页ViewModel。继承 `NavigableViewModelBase`。

依赖服务:
- `IAuthenticationService` - 获取当前用户信息
- `IDialogService` - 对话框
- `INavigationCoordinator` - 统一导航

可观察属性:
- `CurrentUserName` (string) - 当前用户名，默认"医生"
- `TodayConsultationCount` (int) - 今日接诊数量
- `PendingCaseCount` (int) - 待完成医案数量

命令 (全部 `[RelayCommand]`):
- `StartMedicalCase()` - 开始看诊，导航到 `PatientSelectionView`
- `NavigateToPatientManagement()` - 导航到患者管理
- `NavigateToMedicalCaseQuery()` - 导航到医案管理
- `NavigateToHerbLibrary()` - 导航到药材管理
- `NavigateToFormulaLibrary()` - 导航到经验方管理
- `NavigateToSync()` - 导航到数据同步
- `EditProfile()` - 导航到账户设置(个人资料)
- `ChangePassword()` - 导航到账户设置(修改密码)，传递 `Tab=Password` 参数

关键方法:
- `LoadCurrentUser()` (async void) - 构造函数调用，通过 `IAuthenticationService.GetCurrentUserAsync()` 加载用户信息
- `LoadTodayStatistics()` - 加载今日统计数据 (当前为TODO，使用模拟数据0)

INavigationAware: `OnNavigatedTo` 刷新统计数据，`IsNavigationTarget` 返回 true

### ViewModels/PatientSelectionViewModel.cs (429行)

患者选择ViewModel，医生选择患者并开始看诊。继承 `NavigableViewModelBase`。

依赖服务:
- `IPatientApi` - 患者API (搜索、详情)
- `IMedicalCaseApi` - 医案API (查询待处理)
- `IMedicalCaseService` - 医案业务服务 (创建、取消)
- `ICommonDialogService` - 通用对话框 (从 `IViewModelServices` 获取)
- `INavigationCoordinator` - 统一导航

可观察属性:
- `Patients` (ObservableCollection<PatientListDto>) - 患者列表
- `SelectedPatient` (PatientListDto?) - 选中患者，联动 `HasSelection` 计算属性和 `StartMedicalCaseCommand.CanExecute`
- `PatientDetail` (PatientDetailDto?) - 患者详情
- `SearchKeyword` (string) - 搜索关键词
- `PageStatusMessage` (string) - 页面状态消息
- `IsError` (bool) - 错误状态

命令 (全部 `[RelayCommand]`):
- `BackToHome()` - 返回主页
- `NewPatient()` - 导航到患者管理视图
- `RefreshAsync()` - 刷新患者列表
- `SearchAsync()` - 搜索患者
- `StartMedicalCaseAsync()` - 开始看诊 (`CanExecute = nameof(CanStartMedicalCase)`)，检查是否有进行中医案，处理Suspended/Active/新建三种情况

关键方法:
- `OnSelectedPatientChanged()` - partial方法，选中患者变更时加载详情
- `LoadPatientsAsync()` - 调用 `IPatientApi.GetPatientsAsync` 加载列表 (page=1, pageSize=100)
- `LoadPatientDetailAsync()` - 调用 `IPatientApi.GetPatientByIdAsync` 加载详情
- `HandleSuspendedCaseAsync()` - 处理挂起医案，双选项弹窗 (继续/新建)
- `CreateAndNavigateToNewMedicalCaseAsync()` - 通过 `IMedicalCaseService.CreateMedicalCaseAsync` 创建并导航
- `NavigateToMedicalCase(Guid)` - 导航到医案工作区，传递 MedicalCaseId + CurrentPatient + WorkspaceMode.Clinical + EditState.Editing

INavigationAware: `OnNavigatedTo` 加载患者列表

### ViewModels/MedicalCaseWorkspaceViewModel.cs (631行)

医案工作区 Composite ViewModel，薄壳编排层。继承 `NavigableViewModelBase`，实现 `IMedicalCaseWorkspaceContext` 和 `IWorkspaceHost`。

依赖服务 (8个，从14个精简):
- `IMedicalCaseService` - 医案业务服务
- `INavigationCoordinator` - 统一导航
- `IActiveConsultationService` - 活跃诊断服务
- `IPendingQueueManager` - 待诊队列管理
- `PrescriptionPrintHandler` - 打印处理器
- `IPatientCardReaderIntegration` - 患者读卡器集成
- `IDialogService?` - 对话框 (可选)
- `IRegionManager` (从 services 获取)

子 ViewModel (Composite Pattern):
- `ConsultationEditor` - 诊断编辑子VM
- `PrescriptionEditor` - 处方编辑子VM
- `Commands` (MedicalCaseCommandsViewModel) - 命令子VM (含验方导入/历史复制/药材清空)
- `PendingQueue` (PendingQueueViewModel) - 待诊队列子VM
- `CardReader` (CardReaderViewModel) - 读卡器子VM

核心属性:
- `State` (WorkspaceState) - 工作区状态聚合对象
- `Consultation` (ConsultationItem) - 诊断数据Item
- `Prescription` (PrescriptionItem) - 处方数据Item
- `AllHerbs` (ObservableCollection<HerbListDto>) - 药材库数据
- `CurrentPatient` (PatientDetailDto?) - 当前患者
- `MedicalCaseId` (Guid) - 当前医案ID

### ViewModels/Workspace/PendingQueueViewModel.cs

待诊队列子ViewModel。替代原 `PendingQueueHandler` 回调模式。

通过 `IWorkspaceHost` 和 `IMedicalCaseWorkspaceContext` 接口与父VM通信，消除回调属性。负责待诊队列刷新、选择、切换操作。

### ViewModels/Workspace/CardReaderViewModel.cs

读卡器子ViewModel。替代原 `CardReaderWorkspaceHandler` 回调模式。

通过 `IWorkspaceHost` 和 `IMedicalCaseWorkspaceContext` 接口与父VM通信，消除回调属性。负责读卡器连接、手动/自动读卡、患者查找/创建。

### Views/ClinicalHomeView.xaml.cs (16行)

医生工作台主页视图，标准code-behind，仅 `InitializeComponent()`。

### Views/PatientSelectionView.xaml.cs (33行)

患者选择视图。包含 `PatientSelectionControl_PatientDoubleClicked` 事件处理: 双击患者时触发 `StartMedicalCaseCommand`。

### Views/MedicalCaseWorkspaceView.xaml.cs (19行)

医案工作区视图。布局: 左侧50%诊断(Consultation) + 右侧50%处方(Prescription)。复用 MedicalCase 模块的 `ConsultationPanel`, `PrescriptionEditorPanel` 控件。

### Views/HerbManagementView.xaml.cs (23行)

药材管理薄包装视图，复用业务模块 `HerbMasterDetailControl`。无独立ViewModel。权限: 诊所共享只读、自创可管理。

### Views/FormulaManagementView.xaml.cs (23行)

经验方管理薄包装视图，复用业务模块 `FormulaMasterDetailControl`。无独立ViewModel。权限: 诊所共享只读、自创可管理。

### Views/PatientManagementView.xaml.cs (23行)

患者管理薄包装视图，复用业务模块 `PatientMasterDetailControl`。无独立ViewModel。权限: 诊所共享只读、自创可管理。

### Views/MedicalCaseManagementView.xaml.cs (23行)

医案管理薄包装视图，复用业务模块 `MedicalCaseMasterDetailControl`。无独立ViewModel。权限: 诊所共享只读、自创可管理。

### 死代码分析

无死代码。所有类型均有引用:
- 3个ViewModel + 子VM (Workspace/) - 模块注册 + Shell Logger注册
- 7个View - 模块 `RegisterForNavigation` + ViewModel 导航命令引用
- 模块由 Shell `App.xaml.cs` 加载: `moduleCatalog.AddModule<ClinicalModule>(InitializationMode.WhenAvailable)`
- 模块声明依赖 `PatientsModule` 和 `MedicalCaseModule`

已删除文件 (2026-03-05 Composite ViewModel 重构):
- `Handlers/CardReaderWorkspaceHandler.cs` - 替换为 `ViewModels/Workspace/CardReaderViewModel.cs`
- `Handlers/PendingQueueHandler.cs` - 替换为 `ViewModels/Workspace/PendingQueueViewModel.cs`
- `Handlers/PrescriptionImportHandler.cs` - 逻辑迁移到 `MedicalCaseCommandsViewModel`
- `Handlers/` 目录已不存在

注意: `PrescriptionPrintHandler` 仍在使用，位于 MedicalCase 模块 (非 Clinical 模块)。

`MedicalCaseWorkspaceViewModel` 从 1275 行精简到 631 行，低于 800 行推荐上限。

## 模块演进记录

| 日期 | 变更 |
|------|------|
| 2025-10-29 | 初始版本,今日统计+开始看诊 |
| 2026-03-05 | Composite ViewModel重构: 3个Handler删除,拆分为5个子VM,主VM从1275行精简到631行 |
