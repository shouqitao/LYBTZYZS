# 偏差修正计划

> **版本**: v1.0
> **创建日期**: 2026-04-12
> **状态**: 待评审
> **范围**: 医生接诊 + 前台挂号 流程中的所有偏差和短缺修正
> **关联文档**:
> - `consultation-flow-design.md` (完整设计文档)
> - `consultation-flow-architecture-alignment.md` (架构对齐分析)
> - `start-consultation-optimization.md` (开始接诊优化需求)

---

## 一、偏差总览

### 1.1 偏差合并列表

将两个来源文档中的偏差/需求合并去重后，共 **12 项**修正需求：

| 编号 | 来源编号 | 标题 | 优先级 | 类型 |
|------|----------|------|--------|------|
| **COR-001** | P0-2 / 偏差3 / ARCH-002 | 医生直接选患者时缺少 Registration 静默创建 | P0 | 功能修复 |
| **COR-002** | P0-1 / 偏差4 / ARCH-004 | 统计数据不准确 (医生硬编码0 / 前台无日期过滤) | P0 | 功能修复 |
| **COR-003** | 偏差1 / ARCH-001 | 待诊列表数据源不一致 (Registration vs MedicalCase) | P0 | 架构修复 |
| **COR-004** | P0-3 | 待诊队列切换患者时 PatientDetail 为 null | P0 | Bug 修复 |
| **COR-005** | P1-2 / REQ-004 | BR-001 碰撞处理只有二选一，缺少"取消"选项 | P1 | 功能补齐 |
| **COR-006** | P1-1 / REQ-005 | 底部按钮文案"诊断"应为"开始看诊" | P1 | UI 优化 |
| **COR-007** | P1-3 / REQ-006 | ClinicalHomeViewModel 多余命令未清理 | P1 | 代码清理 |
| **COR-008** | P1-4 / REQ-008 | MedicalCaseId 为空时缺少统一错误守卫 | P1 | 代码质量 |
| **COR-009** | P1-5 / REQ-007 / ARCH-007 | 患者选择页缺少待诊队列自动刷新 + 跨角色通知 | P1 | 功能补齐 |
| **COR-010** | 偏差2 / ARCH-003 | 前台"新建挂号"缺少独立创建页，入口跳转不直观 | P1 | 功能补齐 |
| **COR-011** | 偏差5 / ARCH-005 | 前台主页缺少 RefreshDataCommand 实现 | P2 | 功能补齐 |
| **COR-012** | P2-1 / REQ-009 | 患者搜索无分页 | P2 | 体验优化 |

### 1.2 修正优先级矩阵

```
P0 (必须立即修复)     P1 (近期修复)         P2 (体验优化)
┌─────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ COR-001 注册缺失 │  │ COR-005 三选一   │  │ COR-011 刷新命令 │
│ COR-002 统计错误 │  │ COR-006 按钮文案 │  │ COR-012 搜索分页 │
│ COR-003 数据源   │  │ COR-007 多余命令 │  │                  │
│ COR-004 null传递 │  │ COR-008 错误守卫 │  │                  │
│                 │  │ COR-009 自动刷新 │  │                  │
│                 │  │ COR-010 挂号创建 │  │                  │
└─────────────────┘  └──────────────────┘  └──────────────────┘
```

---

## 二、详细修正方案

### COR-001: 医生直接选患者时缺少 Registration 静默创建

**优先级**: P0
**类型**: 功能修复
**关联**: `consultation-flow-design.md` §2.2 路径 A

**问题描述**:
- `PatientSelectionViewModel.StartMedicalCaseAsync()` 直接调用 `CreateMedicalCaseAsync(patientId)`
- 不创建 Registration 记录，导致：
  - 运营数据缺失（无法统计接诊量）
  - 前台无法知道医生已直接接诊某患者
  - 与 PRD `registration.md` Section 4.1 不符

**修正方案**:

修改 `PatientSelectionViewModel.StartMedicalCaseAsync()` 方法，在创建医案前先处理 Registration：

```csharp
// 伪代码流程
async Task StartMedicalCaseAsync(Guid patientId)
{
    // 1. 检查患者是否有 Waiting 状态的 Registration
    var waitingReg = await _registrationService.GetPatientWaitingRegistrationAsync(patientId, currentDoctorId);

    if (waitingReg != null)
    {
        // 1a. 有 Waiting Registration → 调用 StartVisit 接诊
        await _registrationService.StartVisitAsync(waitingReg.RegistrationId);
    }
    else
    {
        // 1b. 无 Registration → 静默创建 (Source=Doctor, Status=InProgress)
        await _registrationService.CreateDoctorDirectRegistrationAsync(patientId, currentDoctorId);
    }

    // 2. 后续医案创建/打开逻辑 (保持不变)
    // ...
}
```

**后端新增接口**:
- `GET /api/registrations/patient/{patientId}/waiting?doctorId={doctorId}` - 检查 Waiting 挂号
- `POST /api/registrations/doctor-direct` - 医生直接接诊时静默创建 Registration

**验收标准**:
- [ ] 从"开始接诊"入口创建医案后，数据库中存在对应的 Registration 记录 (Source=Doctor, Status=InProgress)
- [ ] 从挂号队列接诊后，Registration 状态正确更新为 InProgress
- [ ] 两种路径最终都导航到同一个 MedicalCaseWorkspaceView (Clinical 模式)
- [ ] 相关单元测试覆盖两条路径

**涉及文件**:
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Registration/Services/IRegistrationService.cs` (新增接口)
- 后端 RegistrationController + Service (新增 API)

---

### COR-002: 统计数据不准确

**优先级**: P0
**类型**: 功能修复

**问题描述**:

| 角色 | 当前行为 | 问题 |
|------|----------|------|
| 医生 | `TodayConsultationCount = 0; PendingCaseCount = 0;` (硬编码) | 永远显示 0，误导用户 |
| 前台 | `GetPagedAsync(pageSize: 100)` 然后前端按状态计数 | 未按日期过滤，超 100 条时完全不准确 |

**修正方案 (方案 A: 暂时隐藏)**:

1. **医生端**: 删除统计卡片
   - 从 `ClinicalHomeView.xaml` 移除统计卡片 UI
   - 从 `ClinicalHomeViewModel.cs` 删除 `TodayConsultationCount`、`PendingCaseCount` 属性及 `LoadTodayStatistics()` 方法
   - 从 `OnNavigatedTo()` 中移除对 `LoadTodayStatistics()` 的调用

2. **前台端**: 暂时隐藏统计，或标记为"开发中"
   - 在 `ReceptionistHomeViewModel.LoadStatisticsAsync()` 中添加注释标记
   - 或在 XAML 中显示"统计数据开发中..."占位文字

**验收标准**:
- [ ] 医生首页不再显示"今日统计"卡片
- [ ] ViewModel 中不再有硬编码的统计属性和方法
- [ ] XAML 布局移除统计卡片后仍然美观

**涉及文件**:
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalHomeViewModel.cs`
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml`

---

### COR-003: 待诊列表数据源不一致

**优先级**: P0
**类型**: 架构修复
**关联**: `consultation-flow-design.md` §1.3

**问题描述**:
当前有两个独立的待诊数据源，它们之间没有关联：

| 数据源 | 位置 | 数据来源 | 状态字段 |
|--------|------|----------|----------|
| 挂号队列 | `RegistrationListViewModel` | `IRegistrationService.GetQueueAsync()` | `RegistrationStatus.Waiting` |
| 待诊队列 | `PendingQueueManager` | `IMedicalCaseApi.GetPendingCasesAsync()` | `MedicalCaseStatus.Active/Suspended` |

前台创建挂号后，医生在待诊队列中看不到新患者。

**修正方案**:

方案一（推荐 - 后端统一 API）：

1. 后端新增 `GET /api/medical-cases/combined-pending?doctorId={id}` 接口
   - 合并查询：`SELECT * FROM Registrations WHERE Status='Waiting' AND DoctorId={id} UNION SELECT * FROM MedicalCases WHERE Status IN ('Active','Suspended') AND DoctorId={id}`
   - 按优先级排序：Registration Waiting > MedicalCase Active > MedicalCase Suspended
   - 同一患者去重（优先显示 Registration）

2. 前端修改 `PendingQueueManager.LoadPendingCasesAsync()` 调用新 API

3. 前端 `PendingQueueViewModel` 修改数据模型，支持两种类型的记录展示

方案二（前端合并 - 无需后端大改）：

1. 在 `PendingQueueManager` 中同时调用两个现有 API
2. 在前端合并结果，按优先级排序
3. 同一患者去重

**验收标准**:
- [ ] 前台新建挂号后，医生待诊队列在 30 秒内显示新患者
- [ ] 待诊队列同时显示 Registration Waiting 和 MedicalCase Active/Suspended 记录
- [ ] 同一患者的两条记录只显示一条（优先 Registration）
- [ ] 点击任一记录都能正确接诊

**涉及文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/PendingQueueManager.cs`
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs`
- 后端 MedicalCaseController / RegistrationController (新增合并 API)

---

### COR-004: 待诊队列切换患者时 PatientDetail 为 null

**优先级**: P0
**类型**: Bug 修复

**问题描述**:
- `PendingQueueViewModel.GetPatientDetail(patientId)` 仅在 `CurrentPatient.Id == patientId` 时返回详情
- 从待诊队列选择非当前选中患者时，返回 null → 工作台患者信息为空

**修正方案**:

修改 `PendingQueueViewModel.GetPatientDetail` 方法：

```csharp
private async Task<PatientDetailDto?> GetPatientDetail(Guid patientId)
{
    // 1. 如果当前患者匹配 → 直接返回
    if (CurrentPatient?.Id == patientId)
    {
        return CurrentPatientDetail;
    }

    // 2. 如果不匹配 → 从队列项中获取基本信息，然后加载详情
    var queueItem = Queue.FirstOrDefault(q => q.PatientId == patientId);
    if (queueItem == null)
    {
        Logger.LogWarning("Patient {PatientId} not found in queue", patientId);
        return null;
    }

    // 3. 通过 PatientService 加载详情
    try
    {
        var detail = await _patientService.GetByIdAsync(patientId);
        return detail;
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to load patient detail for {PatientId}", patientId);
        // 4. 降级：使用队列项中的基本信息构造简单 PatientDetailDto
        return new PatientDetailDto
        {
            Id = queueItem.PatientId,
            Name = queueItem.PatientName,
            Phone = queueItem.PhoneMasked
        };
    }
}
```

**验收标准**:
- [ ] 从待诊队列选择非当前选中患者时，医案工作台能正确显示患者信息
- [ ] 患者详情加载失败时显示错误提示而非空白

**涉及文件**:
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs`

---

### COR-005: BR-001 碰撞处理改为三选一弹窗

**优先级**: P1
**类型**: 功能补齐

**问题描述**:
- 当前使用 `ShowConfirmAsync(message, "选择操作")` → Yes=继续, No=新建
- 缺少"取消"选项，用户无法取消操作

**修正方案**:

1. 检查 `ICommonDialogService` 接口是否已有三选一方法
2. 如果没有，新增方法：

```csharp
public enum TripleChoiceResult { Continue, CreateNew, Cancel }

Task<TripleChoiceResult> ShowTripleChoiceAsync(
    string title,
    string message,
    string continueText = "继续看诊",
    string createNewText = "新建医案",
    string cancelText = "取消");
```

3. 修改 `PatientSelectionViewModel.HandleSuspendedCaseAsync()`:
```csharp
var result = await _dialogService.ShowTripleChoiceAsync(
    "选择操作",
    $"患者 [{patientName}] 有未完成的医案。\n\n" +
    $"• 继续看诊 - 打开原医案继续编辑\n" +
    $"• 新建医案 - 暂存原医案并创建新的\n" +
    $"• 取消 - 返回患者列表",
    continueText: "继续看诊",
    createNewText: "新建医案",
    cancelText: "取消");

switch (result)
{
    case TripleChoiceResult.Continue:
        await OpenExistingMedicalCaseAsync(suspendedCaseId);
        return;
    case TripleChoiceResult.CreateNew:
        await SuspendExistingCaseAsync(suspendedCaseId);
        break; // 继续创建新医案
    case TripleChoiceResult.Cancel:
        return; // 取消，返回患者选择页
}
```

4. 同样修改 `PendingQueueViewModel.HandleSuspendedCaseAsync()`

**验收标准**:
- [ ] 弹窗提供三个选项（继续看诊/新建医案/取消）
- [ ] 选择"取消"时关闭弹窗，不进行任何操作
- [ ] PatientSelection 和 PendingQueue 两处弹窗行为一致

**涉及文件**:
- `src/Client/Desktop/Contracts/Services/ICommonDialogService.cs` (新增接口)
- `src/Client/Desktop/Infrastructure/Services/CommonDialogService.cs` (新增实现)
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs`
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs`

---

### COR-006: 底部按钮文案"诊断"改为"开始看诊"

**优先级**: P1
**类型**: UI 优化

**修正方案**:

修改 `PatientSelectionView.xaml` 第 181 行：
```xml
<!-- 修改前 -->
<Button Grid.Column="1" Content="诊断" .../>

<!-- 修改后 -->
<Button Grid.Column="1" Content="开始看诊" .../>
```

**验收标准**:
- [ ] `PatientSelectionView.xaml` 底部按钮显示"开始看诊"
- [ ] 与首页"开始接诊"卡片文案保持一致

**涉及文件**:
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml:181`

---

### COR-007: 清理 ClinicalHomeViewModel 多余命令

**优先级**: P1
**类型**: 代码清理

**问题描述**:
按重构计划应移除的命令：
- `NavigateToHerbLibraryCommand` → 移除
- `NavigateToRegistrationQueueCommand` → 移除
- `NavigateToSyncCommand` → 移除

应保留的命令：
- `StartMedicalCaseCommand` → 主入口
- `NavigateToPatientManagementCommand` → 快捷导航
- `NavigateToMedicalCaseQueryCommand` → 快捷导航
- `NavigateToFormulaLibraryCommand` → 快捷导航 (保留)
- `EditProfileCommand` → 快捷导航
- `ChangePasswordCommand` → 快捷导航

**修正方案**:

1. ViewModel 中删除三个命令方法
2. XAML 中删除对应的三个功能卡片
3. 调整布局（移除卡片后主卡片居中正常）

**验收标准**:
- [ ] ViewModel 中不再有多余的导航命令
- [ ] XAML 中不再有多余的功能卡片
- [ ] 编译通过，无绑定错误

**涉及文件**:
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalHomeViewModel.cs`
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml`

---

### COR-008: MedicalCaseId 为空时统一错误守卫

**优先级**: P1
**类型**: 代码质量

**问题描述**:
- 多处 `if (existingCase.MedicalCaseId.HasValue)` 为 false 时弹错后继续执行
- 部分路径有 return 守卫，但不一致

**修正方案**:

所有 MedicalCaseId 为空的分支统一处理模式：
```csharp
if (!existingCase.MedicalCaseId.HasValue)
{
    var errorMsg = $"创建医案失败: MedicalCaseId 为空 (PatientId: {patientId}, Status: {existingCase.Status})";
    Logger.LogError(errorMsg);
    StatusMessage = errorMsg;
    IsError = true;
    return; // 必须有 return 守卫
}
```

需要检查的路径：
- `PatientSelectionViewModel.StartMedicalCaseAsync()` 中创建医案后
- `PendingQueueViewModel.NavigateToNewMedicalCaseAsync()` 中创建医案后

**验收标准**:
- [ ] 所有 MedicalCaseId 为空的路径都有 `return` 终止执行
- [ ] 错误日志包含 PatientId, CaseStatus 等上下文

**涉及文件**:
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs`
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs`

---

### COR-009: 患者选择页缺少待诊队列自动刷新 + 跨角色通知

**优先级**: P1
**类型**: 功能补齐

**问题描述**:
- `PatientSelectionViewModel.OnNavigatedTo` 只调用一次 `RefreshQueueAsync()`
- 前台新建挂号后，医生端看不到新患者

**修正方案 (双保险)**:

1. **定时刷新** (与 RegistrationListViewModel 保持一致):
```csharp
// 在 PatientSelectionViewModel 中添加
private PeriodicTimer? _autoRefreshTimer;
private const int AutoRefreshIntervalSeconds = 30;

public override void OnNavigatedTo(NavigationContext navigationContext)
{
    base.OnNavigatedTo(navigationContext);
    StartAutoRefreshTimer();
}

public override void OnNavigatedFrom(NavigationContext navigationContext)
{
    base.OnNavigatedFrom(navigationContext);
    StopAutoRefreshTimer();
}

private void StartAutoRefreshTimer()
{
    _autoRefreshTimer?.Dispose();
    _autoRefreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(AutoRefreshIntervalSeconds));
    _ = Task.Run(async () =>
    {
        while (await _autoRefreshTimer.WaitForNextTickAsync())
        {
            await PendingQueue.RefreshQueueAsync();
        }
    });
}

private void StopAutoRefreshTimer()
{
    _autoRefreshTimer?.Dispose();
    _autoRefreshTimer = null;
}
```

2. **EventAggregator 即时通知** (ARCH-007):
```csharp
// 前台创建挂号后发布事件
_eventAggregator.GetEvent<RegistrationCreatedEvent>()
    .Publish(new RegistrationCreatedEventArgs { RegistrationId = regId, PatientId = patientId, DoctorId = doctorId });

// 医生端订阅
_eventAggregator.GetEvent<RegistrationCreatedEvent>()
    .Subscribe(OnRegistrationCreated);

private void OnRegistrationCreated(RegistrationCreatedEventArgs args)
{
    if (args.DoctorId == _currentDoctorId)
    {
        // 刷新待诊队列
        PendingQueue.RefreshQueueAsync().SafeFireAndForget();
    }
}
```

**验收标准**:
- [ ] 患者选择页的待诊队列每 30 秒自动刷新
- [ ] 离开页面后停止自动刷新
- [ ] 前台新建挂号后，医生端通过 EventAggregator 即时收到通知
- [ ] 刷新期间不影响用户操作

**涉及文件**:
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs`
- `src/Client/Desktop/Contracts/Events/RegistrationCreatedEvent.cs` (新文件)
- `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/ViewModels/ReceptionistHomeViewModel.cs` (发布事件)
- `src/Client/Desktop/Modules/LYBT.Desktop.Registration/ViewModels/RegistrationListViewModel.cs` (发布事件)

---

### COR-010: 前台"新建挂号"缺少独立创建页

**优先级**: P1
**类型**: 功能补齐

**问题描述**:
- `ReceptionistHomeViewModel.CreateNewRegistration()` 导航到 `ViewNames.RegistrationList` 并传参 `{ Action: "Create" }`
- `RegistrationListView` 是队列列表，不是独立的挂号创建表单
- `RegistrationCreateDialog` 已存在但前台主页没有直接打开它

**修正方案**:

修改 `ReceptionistHomeViewModel.CreateNewRegistration()`:
```csharp
[RelayCommand]
private async Task CreateNewRegistration()
{
    try
    {
        var result = await _dialogService.ShowDialogAsync<RegistrationCreateDialog>();
        if (result == DialogResult.OK)
        {
            // 挂号创建成功后刷新数据和统计
            await LoadStatisticsAsync();
            // 发布事件通知全系统
            _eventAggregator.GetEvent<RegistrationCreatedEvent>().Publish(...);
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "创建挂号失败");
    }
}
```

同时保留"挂号队列"快捷入口作为第二功能卡片，用于查看/管理已创建的挂号。

**验收标准**:
- [ ] 点击"新建挂号"直接打开 RegistrationCreateDialog
- [ ] 挂号创建成功后刷新统计和队列
- [ ] 通过 EventAggregator 通知全系统

**涉及文件**:
- `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/ViewModels/ReceptionistHomeViewModel.cs`

---

### COR-011: 前台主页缺少 RefreshDataCommand

**优先级**: P2
**类型**: 功能补齐

**问题描述**:
- `ReceptionistHomeView.xaml:265` 绑定了 `RefreshDataCommand`
- `ReceptionistHomeViewModel.cs` 中没有定义该命令

**修正方案**:

在 `ReceptionistHomeViewModel` 中添加：
```csharp
[RelayCommand]
private async Task RefreshData()
{
    try
    {
        await LoadStatisticsAsync();
        // 如果有其他需要刷新的数据，在此调用
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "刷新数据失败");
    }
}
```

**验收标准**:
- [ ] XAML 绑定不再报错
- [ ] 点击刷新按钮能正常刷新数据

**涉及文件**:
- `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/ViewModels/ReceptionistHomeViewModel.cs`

---

### COR-012: 患者搜索无分页

**优先级**: P2
**类型**: 体验优化

**问题描述**:
- `GetPatientsAsync(page: 1, pageSize: 100)` 固定加载前 100 条
- 患者超过 100 人时无法搜索到后面的患者

**修正方案 (搜索驱动模式)**:

改为"输入关键词后搜索"模式：
1. 默认加载最近就诊的 20 人（或不加载）
2. 用户输入搜索关键词后调用 `GetPatientsAsync(keyword, page, pageSize)`
3. 支持"加载更多"或无限滚动

由于此修改影响较大且非核心功能，建议改为搜索驱动模式而非分页模式。

**验收标准**:
- [ ] 患者超过 100 人时能搜索到所有匹配结果
- [ ] 搜索响应时间 < 1 秒

**涉及文件**:
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs`
- `src/Client/Modules/LYBT.Desktop.Patients/Controls/PatientSelectionControl.xaml` (可能需要修改)

---

## 三、实施阶段

### Phase 1: 快速修复 (预计 4-6 小时)

执行顺序: COR-004 → COR-002 → COR-006 → COR-008

| 编号 | 涉及文件 | 风险 | 说明 |
|------|----------|------|------|
| COR-004 | `PendingQueueViewModel.cs` | 低 | 纯 Bug 修复，不改变外部行为 |
| COR-002 | `ClinicalHomeView.xaml`, `ClinicalHomeViewModel.cs` | 低 | 删除无用代码和 UI |
| COR-006 | `PatientSelectionView.xaml:181` | 极低 | 仅改文案 |
| COR-008 | `PatientSelectionViewModel.cs`, `PendingQueueViewModel.cs` | 低 | 添加 return 守卫 |

### Phase 2: 功能补齐 (预计 8-12 小时，含后端)

执行顺序: COR-007 → COR-005 → COR-009 → COR-001 → COR-010

| 编号 | 涉及文件 | 后端依赖 | 说明 |
|------|----------|----------|------|
| COR-007 | `ClinicalHomeView.xaml/ViewModel.cs` | 无 | 清理多余命令 |
| COR-005 | `PatientSelectionViewModel.cs`, `PendingQueueViewModel.cs`, `ICommonDialogService` | 无 | 三选一弹窗 |
| COR-009 | `PatientSelectionViewModel.cs`, `RegistrationCreatedEvent` | 无 | 前端即可 |
| COR-001 | `PatientSelectionViewModel.cs`, `IRegistrationService` | **需要** | 需要后端新增 API |
| COR-010 | `ReceptionistHomeViewModel.cs` | 无 | 前端即可 |

### Phase 3: 架构修正 (预计 6-8 小时，含后端)

| 编号 | 涉及文件 | 后端依赖 | 说明 |
|------|----------|----------|------|
| COR-003 | `PendingQueueManager.cs`, `PendingQueueViewModel.cs` | **需要** | 统一数据源 |

### Phase 4: 体验优化 (预计 5-6 小时)

| 编号 | 涉及文件 | 说明 |
|------|----------|------|
| COR-011 | `ReceptionistHomeViewModel.cs` | 补充缺失命令 |
| COR-012 | `PatientSelectionViewModel.cs` | 搜索驱动模式 |

---

## 四、测试策略

### 4.1 单元测试

| 测试场景 | 测试文件 | 覆盖需求 |
|----------|----------|----------|
| 有 Waiting Registration 时正确调用 StartVisit | `PatientSelectionViewModelTests.cs` | COR-001 |
| 无 Registration 时静默创建 | `PatientSelectionViewModelTests.cs` | COR-001 |
| 切换患者时正确加载 PatientDetail | `PendingQueueViewModelTests.cs` | COR-004 |
| 三选一弹窗各分支逻辑 | `PatientSelectionViewModelTests.cs` | COR-005 |
| 自动刷新定时触发 | `PatientSelectionViewModelTests.cs` | COR-009 |
| MedicalCaseId 为空时正确 return | `PatientSelectionViewModelTests.cs` | COR-008 |

### 4.2 集成测试

| 测试场景 | 说明 | 覆盖需求 |
|----------|------|----------|
| 端到端：开始接诊 → 选择患者 → 创建医案 | 验证完整链路 | COR-001 |
| 端到端：挂号队列 → 接诊 → 导航到工作台 | 验证路径 B | COR-001 |
| 并发：前台新建挂号后 30 秒内医生可见 | 验证自动刷新 | COR-009 |
| EventAggregator: RegistrationCreated 事件传播 | 验证跨角色通知 | COR-009 |

### 4.3 手动测试清单

| 测试项 | 步骤 | 预期 | 覆盖需求 |
|--------|------|------|----------|
| 统计卡片隐藏 | 打开 Clinical 首页 | 不显示统计卡片 | COR-002 |
| 按钮文案统一 | 打开患者选择页 | 底部按钮显示"开始看诊" | COR-006 |
| BR-001 三选一 | 选择有挂起医案的患者 | 弹窗显示三个选项 | COR-005 |
| 待诊队列自动刷新 | 前台新建挂号，观察医生端 | 30 秒内队列出现新患者 | COR-009 |
| 新建挂号直接弹窗 | 前台主页点击"新建挂号" | 直接打开创建对话框 | COR-010 |
| 切换患者信息正确 | 从待诊队列选择非当前患者 | 工作台正确显示该患者信息 | COR-004 |
| Registration 记录完整性 | 医生直接选患者创建医案 | 数据库中存在 Registration 记录 | COR-001 |

---

## 五、风险评估

| 风险 | 影响范围 | 概率 | 缓解措施 |
|------|----------|------|----------|
| COR-001 涉及后端 API 变更 | 前后端需同步发布 | 中 | 先实现前端兼容层，后端 API 就绪后切换 |
| COR-003 数据源合并可能影响现有查询性能 | 待诊列表加载变慢 | 低 | 后端添加复合索引，前端添加加载指示器 |
| COR-005 三选一弹窗需要新的 DialogService 方法 | 现有 ICommonDialogService 不支持 | 中 | 检查接口能力，必要时新增 |
| COR-009 EventAggregator 跨模块通知 | 模块解耦可能影响事件传播 | 低 | 事件定义在 Contracts 层，确保模块间共享 |

---

## 六、回滚策略

每个 Phase 的变更都是独立可回滚的：

1. **Phase 1**: 纯前端修复，回滚只需 `git revert`
2. **Phase 2**: 部分依赖后端 API，前端兼容层可以独立回滚
3. **Phase 3**: 架构修正涉及后端 API，需要协调回滚
4. **Phase 4**: 体验优化，低风险，可独立回滚

数据库层面：所有修正不涉及数据结构变更，无需迁移回滚。

---

## 七、验收总检查清单

### Phase 1 验收
- [ ] 医生端统计卡片已移除 (COR-002)
- [ ] 底部按钮文案为"开始看诊" (COR-006)
- [ ] 从待诊队列切换患者时工作台显示正确信息 (COR-004)
- [ ] MedicalCaseId 为空时所有路径都有 return (COR-008)
- [ ] 所有单元测试通过

### Phase 2 验收
- [ ] 医生直接选患者后数据库有 Registration 记录 (COR-001)
- [ ] BR-001 弹窗提供三个选项 (COR-005)
- [ ] ClinicalHomeViewModel 无多余命令 (COR-007)
- [ ] 患者选择页待诊队列 30 秒自动刷新 (COR-009)
- [ ] 前台"新建挂号"直接打开创建对话框 (COR-010)
- [ ] 所有集成测试通过

### Phase 3 验收
- [ ] 待诊队列同时显示 Registration 和 MedicalCase 记录 (COR-003)
- [ ] 前台新建挂号后医生端可见新患者 (COR-003)
- [ ] 同一患者不重复显示 (COR-003)

### Phase 4 验收
- [ ] 前台主页 RefreshDataCommand 正常工作 (COR-011)
- [ ] 患者搜索能获取超过 100 条的结果 (COR-012)

---

*文档版本: v1.0 | 创建日期: 2026-04-12 | 状态: 待评审*
