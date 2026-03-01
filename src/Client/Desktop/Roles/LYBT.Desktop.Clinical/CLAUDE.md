# LYBT.Desktop.Clinical 代码知识

## 架构决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 统计仪表盘+单一核心操作模式 | 医生工作台聚焦诊疗流程,减少操作步骤 | 2025-10-29 |
| OnNavigatedTo自动刷新统计 | 每次进入页面保证数据最新,无需手动刷新 | 2025-10-29 |
| ExecuteSafelyAsync包装异步操作 | 自动处理异常和加载状态,避免重复样板代码 | 2025-10-29 |

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

### ViewModels/MedicalCaseWorkspaceViewModel.cs (1275行)

医案工作区ViewModel，4:6统一看诊界面（左诊断右处方）。继承 `NavigableViewModelBase`。

依赖服务 (14个):
- `MedicalCaseService` (_dataManager) - 数据管理
- `IMedicalCaseService` - 医案业务服务
- `MedicalCaseWorkspaceCoordinator` - 工作区协调器（数据加载/保存）
- `MedicalCaseEditModeStateMachine` - 编辑模式状态机
- `INavigationCoordinator` - 统一导航
- `IActiveConsultationService` - 活跃诊断服务
- `IPendingQueueManager` - 待诊队列管理
- `PrescriptionPrintHandler` - 打印处理器
- `PendingQueueHandler` - 待诊队列操作处理器
- `PrescriptionImportHandler` - 处方导入处理器
- `CardReaderWorkspaceHandler` - 读卡器处理器
- `IPatientCardReaderIntegration` - 患者读卡器集成
- `IDialogService?` - 对话框 (可选)
- `IRegionManager` (从 services 获取)

核心属性:
- `State` (WorkspaceState) - 工作区状态聚合对象
- `Consultation` (ConsultationItem) - 诊断数据Item
- `Prescription` (PrescriptionItem) - 处方数据Item
- `AllHerbs` (ObservableCollection<HerbListDto>) - 药材库数据
- `CurrentPatient` (PatientDetailDto?) - 当前患者
- `MedicalCaseId` (Guid) - 当前医案ID
- `PendingQueue` (ObservableCollection<PendingMedicalCaseDto>) - 待诊队列
- `NeedsPrescription` / `IsPrescriptionEnabled` / `CanComplete` / `CanPrintPrescription` - 状态控制

编辑模式委托属性 (来自 `MedicalCaseEditModeStateMachine`):
- `IsEditing`, `IsReadOnly`, `ShowEditButton`, `ShowSaveButton`, `ShowSuspendButton`, `ShowCompleteButton`
- `IsHistoricalEditMode`, `CanEdit`, `EditReason`, `WorkspaceMode`, `HeaderTitle`, `BackButtonText`

读卡器属性: `IsCardReaderConnected`, `IsAutoReadEnabled`, `IsReading`, `CardReaderStatusMessage`

命令 (DelegateCommand):
- `BackCommand` / `BackToPatientSelectionCommand` - 返回
- `SuspendCommand` - Clinical模式挂起医案
- `SaveChangesCommand` - Management模式保存修改
- `CompleteMedicalCaseCommand` - 完成医案
- `SaveCommand` - 保存
- `EnterEditModeCommand` - 进入编辑模式
- `PrintPrescriptionCommand` - 打印处方
- `RefreshQueueCommand` - 刷新待诊队列
- `SelectPendingCaseCommand` (DelegateCommand<PendingMedicalCaseDto>) - 选择待诊患者
- `OpenFormulaImportDialogCommand` - 打开验方导入对话框
- `OpenHistoryCopyDialogCommand` - 打开历史处方复制对话框
- `ClearHerbItemsCommand` - 清空药材列表
- `ReadCardCommand` - 手动读卡
- `ToggleAutoReadCommand` - 切换自动读卡
- `ViewPatientHistoryCommand` - 查看患者历史

### Handlers/CardReaderWorkspaceHandler.cs (481行)

读卡器工作台处理器，实现 `IDisposable`。从 ViewModel 提取读卡器相关业务逻辑。

依赖服务: `ICardReaderService`, `IPatientCardReaderIntegration`, `IMedicalCaseService`, `INavigationCoordinator`

属性: `IsConnected`, `IsAutoReadEnabled`, `IsReading`, `StatusMessage`

回调属性 (与ViewModel通信):
- `SetBusy`, `ShowErrorMessage`, `ShowSuccessMessage`, `GetCommonDialogService`, `OnPropertyChanged`
- `OnPatientReadyForMedicalCase` - 读卡成功并找到/创建患者后的回调

公共方法:
- `InitializeAsync()` - 初始化读卡器连接
- `ManualReadCardAsync()` - 手动读卡
- `ToggleAutoRead()` / `StartAutoRead()` / `StopAutoRead()` - 自动读卡控制
- `DisconnectAsync()` - 断开连接

私有方法:
- `HandleCardReadResultAsync()` - 处理读卡结果
- `ProcessPatientFromCardAsync()` - 根据身份证号查找或创建患者
- `HandleNewPatientFromCardAsync()` - 新患者确认创建弹窗
- `MaskIdNumber()` - 身份证号隐私掩码 (保留前6后4)

事件处理: `OnConnectionStateChanged`, `OnCardReadCompleted`, `OnCardReadError`

### Handlers/PrescriptionImportHandler.cs (337行)

处方导入处理器。负责验方导入、历史处方复制、药材清空操作。

依赖: `IDialogService?`, `ILoggerFactory`

回调属性: `GetCommonDialogService`, `SetBusy`, `ShowErrorMessage`, `ShowSuccessMessage`, `ShowConfirmMessage`, `GetCurrentPatient`, `GetPrescription`, `GetAllHerbs`

公共方法:
- `OpenFormulaImportDialog()` - 打开 `FormulaImportDialog`，导入验方药材
- `OpenHistoryCopyDialog()` - 打开 `HistoryCopyDialog`，复制历史处方药材
- `ClearHerbItemsAsync()` - 清空药材列表 (带确认)

私有方法:
- `FilterDisabledHerbs()` - 过滤禁用药材 (T5-P2-19, T5-P2-21)
- `HandleFormulaImportResultAsync()` - 处理验方导入结果，记录引用验方名
- `HandleHistoryCopyResultAsync()` - 处理历史复制结果，复制来源信息和处方级字段 (T5-P2-23, T5-P3-09)

### Handlers/PendingQueueHandler.cs (379行)

待诊队列操作处理器。负责待诊队列刷新、选择、切换操作。

依赖: `IMedicalCaseService`, `IPendingQueueManager`, `INavigationCoordinator`

回调属性: `GetCommonDialogService`, `SetBusy`, `ShowErrorMessage`, `GetCurrentMedicalCaseId`, `GetCurrentPatient`, `GetIsReadOnly`, `SuspendOnly`, `OnPropertyChanged`

公共方法:
- `RefreshQueueAsync(Action<bool>)` - 刷新待诊队列
- `SelectPendingCaseAsync(PendingMedicalCaseDto?)` - 选择待诊患者并切换医案，处理 Active/Suspended/新建三种状态

私有方法:
- `HandleSuspendedCaseAsync()` - 处理挂起医案，双选项弹窗 (继续/新建)
- `NavigateToNewMedicalCaseAsync()` - 创建新医案并导航
- `NavigateToExistingMedicalCaseAsync()` - 导航到已存在医案
- `GetPatientDetailAsync()` - 获取患者详情 (优先返回当前患者)

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
- 3个ViewModel - 模块注册 + Shell Logger注册
- 3个Handler - `MedicalCaseWorkspaceViewModel` 构造函数注入使用
- 7个View - 模块 `RegisterForNavigation` + ViewModel 导航命令引用
- 模块由 Shell `App.xaml.cs` 加载: `moduleCatalog.AddModule<ClinicalModule>(InitializationMode.WhenAvailable)`
- 模块声明依赖 `PatientsModule` 和 `MedicalCaseModule`

注意: `MedicalCaseWorkspaceViewModel` 有 1275 行，超过推荐的 800 行上限。已通过 Handler 提取 (CardReaderWorkspaceHandler, PrescriptionImportHandler, PendingQueueHandler) 进行了 SRP 分解，但核心编排逻辑仍较长。

## 模块演进记录

| 日期 | 变更 |
|------|------|
| 2025-10-29 | 初始版本,今日统计+开始看诊 |
