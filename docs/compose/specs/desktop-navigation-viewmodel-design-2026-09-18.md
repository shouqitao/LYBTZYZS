# Desktop 导航架构详细设计（View/ViewModel 跳转修复）

> 版本: v1.0 | 日期: 2026-09-18 | 状态: 设计待实施（需求/方案已对齐审查结论）
> 输入: [desktop-navigation-audit-2026-09-18.md](../reports/desktop-navigation-audit-2026-09-18.md)
> 关联正式文档（实施后须同步）: [desktop-ui-detailed-design.md](../../07-ui-ux/desktop-ui-detailed-design.md)、[02-desktop.md](../../03-architecture/02-desktop.md)
> 约束: 文档先行；0 错误 0 警告；外科手术式修改；禁止兼容层；权限变更同步双控制器树（若服务端策略调整）

---

## 1. 设计目标

| 目标 | 验收 |
|------|------|
| 每个角色可完成其核心业务导航闭环，无守卫误拦 | Receptionist 能接诊进入医案工作台；Doctor 从 ClinicalWorkspace「开始看诊」进入工作台且有患者/医案上下文 |
| 导航参数生产=消费成对存在 | 架构测试：每个 NavParams 键在目标 VM 至少被 GetValue/ContainsKey 一次 |
| 唯一导航门面 | ViewModel 不得直接 `RegionManager.RequestNavigate`；统一走 `INavigationCoordinator` |
| 菜单与权限矩阵一致 | 无权入口对当前角色不可见（非点击后 toast） |
| 返回路径可预期 | 可见后退按钮；Journal 空时 fallback 到角色主页 |
| 对话框视觉与职责单一 | 业务弹窗走 Prism Dialog（MDIX）；消灭 MessageBox 双轨 |

**非目标（本设计不覆盖）**：服务端 API 权限模型重构；UI 视觉重绘；患者/医案业务规则变更。

---

## 2. 目标架构

### 2.1 导航栈（唯一门面）

```
ViewModel / Menu / SideNav / Shell 快捷键
        │
        ▼
INavigationCoordinator.NavigateTo(viewName, NavigationParameters)
        │  1) RoleAccessGuard（ViewRoleAccess + 可选业务守卫）
        │  2) Debounce + Timeout
        │  3) IModuleLazyLoader.EnsureModuleLoadedAsync
        │  4) IRegionManager.RequestNavigate(ContentRegion, view, params)
        │  5) INavigationHistoryService.Record + NavigationChanged
        ▼
目标 View + ViewModel.OnNavigatedTo（消费 NavigationParameters）
```

**废弃路径**（一次性删除，不做兼容层）：

- `NavigableViewModelBase.NavigateTo(string region, string view)` 直调 RegionManager（`NavigableViewModelBase.Navigation.cs:171-183`）
- `NavigableViewModelBase.NavigateToHome()`（`:149-162`）改为解析 `INavigationCoordinator` 并委托（或基类注入 coordinator）

架构测试：`DesktopLayerArchTests` 现有 DP09 保持——VM 禁注入 `IRegionManager`；**新增**：VM 基类/子类中不得出现 `RegionManager.RequestNavigate` 字面量（白名单仅 NavigationCoordinator + LoginRegion 清理）。

### 2.2 角色×视图访问矩阵（目标态）

更新 `NavigationCoordinator.ViewRoleAccess`：

| ViewName | Doctor | Receptionist | Admin | SuperAdmin | 变更 |
|----------|:------:|:------------:|:-----:|:----------:|------|
| AdminHome | | | ✓ | | 不变 |
| SysadminHome | | | | ✓ | 不变 |
| ClinicalHome | ✓ | | | | 不变（仍无 UI 入口，见 §5.4） |
| ReceptionistHome | | ✓ | | | 不变 |
| ClinicalWorkspace | ✓ | | | | 不变 |
| MedicalCaseWorkspace | ✓ | **✓** | **✓** | **✓** | **改**：接诊/管理查看需要；业务上前台 StartVisit 必达 |
| PatientSelection | ✓ | ✓ | | | 不变 |
| RegistrationList | ✓ | ✓ | | | 不变 |
| PatientManagement | ✓ | ✓ | ✓ | ✓ | 不变 |
| MedicalCaseManagement | ✓ | | ✓ | ✓ | 不变 |
| Herb/Formula Management | ✓ | | ✓ | ✓ | 不变 |
| UserManagement | | | ✓ | ✓ | 不变 |
| SystemSettings | | | ✓ | ✓ | 不变 |
| Backup/SecurityAudit | | | ✓ | ✓ | 不变 |
| LogLevel/Deployment | | | | ✓ | 不变 |
| ReportsHome / AuditLog / MedicalCaseMasterDetail | ✓ | | ✓ | ✓ | 不变 |
| AccountSettings | （不限制） | | | | 保持未列入=放行 |
| Login | （匿名放行） | | | | 不变 |

> **服务端对齐**：MedicalCaseWorkspace 对 Receptionist 放开属于客户端守卫变更。若服务端 `DoctorOrAdminOrReceptionist` 已覆盖读写医案接口则无需改控制器；若创建/更新医案策略仅 Doctor，需按「双控制器树」同步 Remote+Local 策略，并在需求文档 `01-product/04-permissions.md` 确认。**实施前必须 grep 两棵控制器树**。

### 2.3 模块加载矩阵（目标态）

| 角色 RequiredModules 增量 | 原因 |
|---------------------------|------|
| SuperAdmin + `"ClinicalModule"` 或至少保证 ModuleLazyLoader 可拉起 | SuperAdmin 权限矩阵含 Herb/Formula/Patient/MedicalCase 管理视图（注册于 ClinicalModule） |
| Receptionist + `"ClinicalModule"` | ReceptionistHome 与 PatientManagement 薄包装均在 ClinicalModule（当前靠导航时懒加载，登录后主页导航会触发；建议写入 RequiredModules 使登录路径确定） |
| Doctor | 保持现状 + 登录后 NavigateTo 已懒加载 ClinicalWorkspace→ClinicalModule；**或**将 `"ClinicalModule"` 显式加入 Doctor RequiredModules，消除首屏空白风险 |

设计决策：**角色 Home 所属模块必须出现在该角色 RequiredModules**（SSOT 对齐 RoleDefinition），懒加载仅用于次级业务页。

- Doctor: + `ClinicalModule`
- Receptionist: + `ClinicalModule`
- SuperAdmin: + `ClinicalModule`（管理页）+ `ReportsModule`（若矩阵含 ReportsHome）+ `MedicalCaseModule`（AuditLog/MasterDetail）

失败策略：`EnsureModuleLoadedAsync` 失败时 **禁止静默**——回调 `IUserNotificationService.ShowErrorAsync("模块加载失败：{module}")`，并尝试 fallback `NavigateToHome()`。

---

## 3. 导航参数契约（详细设计）

### 3.1 统一契约类型

放 `LYBT.Desktop.Contracts/Models/Navigation/`（与现有 `MedicalCaseNavigationParameters` 同命名空间层）：

```csharp
// 医案工作台 — 唯一入口参数工厂
public static class MedicalCaseNav
{
    public const string MedicalCaseId = "MedicalCaseId";
    public const string CurrentPatient = "CurrentPatient"; // PatientDetailDto
    public const string PatientId = "PatientId";           // 可选：目标可据此回填 CurrentPatient
    public const string WorkspaceMode = "WorkspaceMode";
    public const string InitialEditState = "InitialEditState";
    public const string EditMode = "EditMode";

    /// 完整：已有医案 + 患者详情
    public static NavigationParameters ForExistingCase(Guid medicalCaseId, PatientDetailDto patient,
        WorkspaceMode mode = WorkspaceMode.Clinical, EditState edit = EditState.Editing);

    /// 新建：仅患者（目标负责 CreateMedicalCase）
    public static NavigationParameters ForNewCase(PatientDetailDto patient,
        WorkspaceMode mode = WorkspaceMode.Clinical, EditState edit = EditState.Editing);

    /// 禁止：无 patient / medicalCaseId 的裸导航（不提供 API）
}

public static class PatientManagementNav
{
    public const string Action = "Action"; // "AddNew" | "Create" | "Search"
    public const string SearchKeyword = "SearchKeyword";
    public static NavigationParameters AddNew();
    public static NavigationParameters Search(string keyword);
}

public static class RegistrationListNav
{
    public const string Action = "Action"; // "Create"
    public const string PatientId = "PatientId";
    public const string PatientName = "PatientName";
    public static NavigationParameters Create();
    public static NavigationParameters CreateForPatient(Guid patientId, string patientName);
}

public static class AuditLogNav
{
    public const string MedicalCaseId = "MedicalCaseId";
    public static NavigationParameters ForCase(Guid medicalCaseId);
}

public static class AccountSettingsNav
{
    public const string Tab = "Tab"; // "Password"
    public static NavigationParameters PasswordTab();
}
```

保留 `MedicalCaseNavigationParameters` 类型时：**改为薄封装上述工厂**，删除 ForClinical 旧重载（禁止兼容层）。

### 3.2 生产方 × 消费方矩阵（目标）

| 导航 | 生产方（改） | 消费方（改） | 参数 |
|------|--------------|--------------|------|
| ClinicalWorkspace → MedicalCaseWorkspace | `ClinicalWorkspaceViewModel.StartConsultation` | `MedicalCaseWorkspaceViewModel.OnNavigatedToAsync` | ForNewCase(selectedPatient)：传 CurrentPatient；目标 Id 空时 CreateMedicalCase |
| PatientSelection → MedicalCaseWorkspace | `NavigateToMedicalCase` | 同上 | ForExistingCase(id, PatientDetail) |
| RegistrationList StartVisit → Workspace | `RegistrationListViewModel.StartVisitAsync` | 同上 | ForExistingCase(result.Data, patientDto) |
| 侧栏/Ctrl+Shift+C「开始看诊」 | MenuManager / NavigationManager | — | **改为** `NavigateTo(ClinicalWorkspace)`（医生）；非医生隐藏命令 |
| Receptionist 预填挂号 | ReceptionistHome（Create/CardReader/Search） | `RegistrationListViewModel` OnNavigatedTo/Initialize | Create() / CreateForPatient |
| 新建患者 | MenuManager / Receptionist / ClinicalWorkspace | `PatientMasterDetailViewModel` OnNavigatedToAsync 或薄包装 code-behind | AddNew() → 控件 SetAddNew / 进入编辑 |
| 搜索患者 | ReceptionistHome | PatientMasterDetail | Search(keyword) |
| Workspace → AuditLog | MedicalCaseWorkspaceViewModel | AuditLogViewModel | ForCase(MedicalCaseId) |
| Workspace → 患者历史 | MedicalCaseWorkspaceViewModel | PatientManagement 或专用入口 | Search(patient.Id) 或未来 PatientDetail 路由；**短期**：PatientManagement + SearchKeyword=姓名 |
| 个人资料/改密 | ClinicalWorkspace（需入口）/ Header | AccountSettingsViewModel | 默认资料；PasswordTab() |

### 3.3 消费端实现要点

**MedicalCaseWorkspaceViewModel.OnNavigatedToAsync**（`MedicalCaseWorkspaceViewModel.cs:325-345`）：

```csharp
MedicalCaseId = parameters.GetValue<Guid>(MedicalCaseNav.MedicalCaseId);
CurrentPatient = parameters.GetValue<PatientDetailDto>(MedicalCaseNav.CurrentPatient);

// 修复：PatientId-only 路径
if (CurrentPatient == null)
{
    var pid = parameters.GetValue<Guid>(MedicalCaseNav.PatientId);
    if (pid != Guid.Empty)
        CurrentPatient = await _patientService.GetByIdAsync(pid) …;
}

// 无上下文导航：明确失败而非空白
if (CurrentPatient == null && MedicalCaseId == Guid.Empty)
{
    await ShowErrorMessageAsync("请从患者列表或挂号队列选择患者后再开始看诊");
    _navigationCoordinator.NavigateToHome();
    return;
}
// 其后沿用 InitializePatientInfoAsync（CurrentPatient 非空且 Id 空 → Create）
```

**RegistrationListViewModel**：`InitializeAsync` / `OnNavigatedToCore` 读取 `RegistrationListNav`；`Action==Create` 时自动 `CreateRegistrationCommand`；携带 PatientId 时打开对话框并预填。

**PatientManagement**：优先在 `PatientManagementView.xaml.cs` 实现 `INavigationAware.OnNavigatedTo`（对齐 `UserManagementView.xaml.cs:20-25` 先例）→ 调用嵌入 Control 的公开方法（`SetAddNewMode` / `SetSearchKeyword`）；若 Control 无 API，则在 Patients 模块 `PatientMasterDetailViewModel.OnNavigatedToAsync` 消费。**禁止**参数只写不读。

**AuditLogViewModel**：无参时加载**全部最近日志**或提示「请从医案进入」，禁止空白页。

### 3.4 架构测试（设计定案后先写测试）

`tests/LYBT.Tests.Architecture/`：

1. `NavParams_ContractKeys_ConsumedByTargetViewModel`：反射扫描 Contracts 导航常量类 ↔ Desktop ViewModels 中 `GetValue`/`ContainsKey` 字符串，键必须成对。
2. `ViewModels_MustNot_RequestNavigate_Directly`：源码/编译产物扫描 `RequestNavigate`，白名单 NavigationCoordinator/LoginCoordinator/ClearRegion。
3. `ViewRoleAccess_CoversAllRegisterForNavigationViews`：ViewNames 全集 ⊆ ViewRoleAccess 键（AccountSettings/Login 可显式豁免表）。
4. `RoleRequiredModules_ContainHomeViewModule`：HomeView→ModuleLazyLoader 映射的模块名 ∈ 该角色 RequiredModules。

---

## 4. 返回与历史设计

### 4.1 UI

- Header 或 SideNav 顶增加 **后退** 按钮（MaterialDesign `ArrowLeft`），绑定 `NavigateBackCommand`。
- `NavigationCoordinator.NavigationChanged` 事件 → MenuManager/Header 订阅 → `RaiseCanExecuteChanged`（Back/Forward）。

### 4.2 NavigateBack 语义（NavigationCoordinator.cs:232-252）

```csharp
public void NavigateBack()
{
    if (journal.CanGoBack) { journal.GoBack(); return; }
    _userNotification.ShowWarningAsync("已是最早的页面，已返回主页");
    _ = NavigateToHome(); // fallback
}
```

MedicalCaseWorkspace Clinical 模式返回目标（`WorkspaceNavigationHandler.cs:85`）：

| 来源 | 返回目标 |
|------|----------|
| 从 ClinicalWorkspace 进入 | `ClinicalWorkspace`（KeepAlive 恢复选中患者） |
| 从 RegistrationList 进入 | `RegistrationList` |
| 从 PatientSelection 进入 | `PatientSelection` |
| 侧栏直达（应已禁止） | `NavigateToHome` |

实现：导航生产时将 `ReturnView` 写入参数，或 Workspace VM 在 OnNavigatedTo 记录 `fromView = coordinator.CurrentView`；Back 时优先显式 ReturnView。

### 4.3 HistoryService

- 面包屑仅展示用；**不**作为 GoBack 数据源（与 Journal 职责写进 `02-desktop.md`）。
- `ClearHistory()` 同时清 Journal（Region NavigationService.Journal.Clear()）——登出/切换用户时调用。

---

## 5. 菜单 / 侧栏 / 快捷键设计

### 5.1 角色矩阵（C+ 对齐 + 修复）

| 角色 | 侧栏 3 项（目标） | 主页卡片（目标） |
|------|-------------------|------------------|
| Doctor | 主页 ClinicalWorkspace；患者选择；挂号队列 | 开始看诊→Workspace 内嵌；患者管理；药材；验方；挂号；报表；审计；账户 |
| Receptionist | 主页 ReceptionistHome；挂号列表；患者管理 | 新建挂号；读卡；患者搜索；队列 |
| Admin | 主页 AdminHome；用户管理；药材/验方 | 现有卡片 + 报表/审计（已有命令） |
| SuperAdmin | 主页 SysadminHome；备份；部署 | 用户/日志/部署/**备份**/安全审计 + 可选系统设置 |

变更点：

- NavigationManager Doctor：去掉无参「医案工作台」，改为「挂号队列」或保留 PatientSelection + RegistrationList。
- SysadminHome 增加 `NavigateToBackupManagement` + XAML 卡片。
- MenuManager：命令按角色 `CanExecute`；SystemSettings/QuickStartMedicalCase 对非授权角色隐藏。

### 5.2 快捷键

| 快捷键 | 行为 |
|--------|------|
| Alt+Left / Right | 后退/前进（CanExecute 随导航更新；无历史时后退→主页） |
| Alt+Home | 角色主页 |
| Ctrl+N | 仅 Doctor/Receptionist/Admin：PatientManagement AddNew |
| Ctrl+Shift+C | **仅 Doctor**：NavigateTo(ClinicalWorkspace)；其他角色命令不可用 |
| Ctrl+, | 仅 Admin/SuperAdmin：SystemSettings；其他隐藏或打开 AccountSettings |

### 5.3 角色可见性原则

**能隐藏不 toast**：`NavigationItem`/`MenuItem` 的 `IsVisible` 由 `ViewRoleAccess` 或 `IRoleDefinition` 推导；守卫仍保留作二次防线。

### 5.4 ClinicalHome 处置（决策）

推荐：**删除导航死代码 + 保留注册用于未注册角色 fallback 不够体面 → 改为 fallback 也指向各角色 Home；角色未注册时显示错误页/登录重试，而不是 ClinicalHome。**

- 代码：`RoleRegistry.DefaultHomeView` 改为 null/特殊值，GetHomeViewName 失败时由调用方处理。
- ClinicalHomeView：下个批次删除注册与文件，或降级为 Doctor「统计仪表盘」并挂回侧栏——**产品决策，实施前需 Hermes/用户确认**。设计默认：**暂不删 View，删除过时注释与误导性命令文案，标记 `[Obsolete]` 不作为入口**。

---

## 6. 对话框服务收敛

### 6.1 目标结构

```
业务确认/消息/输入
    → IDialogManager（Prism MessageDialog / ConfirmationDialog / InputDialog）  # 唯一门面
业务专用弹窗
    → Prism IDialogService.ShowDialog(已 RegisterDialog 名)
文件
    → IFileDialogService
Toast 轻提示
    → IToastService / IUserNotificationService
```

### 6.2 变更

| 项 | 动作 |
|----|------|
| `CommonDialogService` MessageBox 实现 | 改为委托 `IDialogManager`（或 Prism Dialog）实现同一接口；删除 `System.Windows.MessageBox` |
| 离开确认三选项 | 使用 `UnsavedChangesDialog`（Yes=暂存 / No=取消医案 / Cancel=留下）或扩展 ConfirmationDialog；禁止原生 YesNoCancel |
| `ShellDialogHelper` | 全部改走 IDialogManager + IToastService |
| Dialog 参数键 | 常量化：`DialogParams.Message/Title/Type`；MessageDialog 与 ConfirmationDialog 键风格统一为 Pascal（同步改 DialogManager 与 MessageDialogViewModel） |
| InputDialog | grep 调用点；无调用则文档标注「预留」或删除注册 |

对话框清单状态列以本设计实施结果更新 inventory。

---

## 7. View/ViewModel 专项问题设计

| 问题 | 设计 |
|------|------|
| PatientMasterDetail ViewMedicalRecords / NewConsultation 空实现 | ViewMedicalRecords → `AuditLogNav`/`MedicalCaseManagement` + PatientId；NewConsultation → 校验角色后 `MedicalCaseNav.ForNewCase(patientDetail)`。Doctor 外禁用命令 |
| ClinicalWorkspace 与 MedicalCaseWorkspace 双工作台 | 明确：ClinicalWorkspace=选患者+历史摘要+入口；MedicalCaseWorkspace=诊疗编辑。**不在** ClinicalWorkspace 内嵌完整诊疗 VM（保持模块边界） |
| ReceptionistHome 确认框结果忽略（`:242`） | `if (!confirmed) return;` 后再导航 |
| ReceptionistHome 统计 GetPagedAsync(pageSize:100) 冒充今日 | 非本设计导航范围，记 backlog（数据正确性） |
| UserManagement DefaultRoleFilter | Sysadmin「管理员账号」入口导航时传入 `UserRole.Admin`；无入口则删死契约 |
| AuditLog 无参空白 | 默认加载最近 N 条 + 医案筛选可选 |
| 防抖与 CurrentView | CurrentView 返回 View 类名，与 ViewNames 一致时防抖有效；若失败仅影响重复导航，保持现状 |

---

## 8. 实施切片（建议任务）

| 切片 | 内容 | 涉及文件（主） | 验收 |
|------|------|----------------|------|
| **N1 P0 守卫+契约** | ViewRoleAccess 扩权 MedicalCaseWorkspace；MedicalCaseNav 工厂；ClinicalWorkspace/RegistrationList/PatientSelection/Workspace 消费对齐；无参导航拒绝 | NavigationCoordinator、MedicalCaseNavigationParameters、ClinicalWorkspaceViewModel、MedicalCaseWorkspaceViewModel、RegistrationListViewModel、PatientSelectionViewModel、WorkspaceNavigationHandler | 前台接诊进工作台有患者；医生开始看诊进工作台有患者；空导航回主页+提示 |
| **N2 P0 参数消费** | RegistrationList/PatientManagement 消费 Action/Patient 预填 | RegistrationListViewModel、PatientManagementView.cs、PatientMasterDetailViewModel | 前台点「新建挂号」自动开弹窗并预填；Ctrl+N 进入新建患者 |
| **N3 P1 单门面** | 基类 NavigateTo/NavigateToHome 委托 coordinator；菜单角色可见性；快捷键矩阵；Sysadmin 备份入口 | NavigableViewModelBase.Navigation、MenuManager、NavigationManager、SysadminHome*、MainWindow | 架构测试 RequestNavigate 白名单；非管理员无 SystemSettings 快捷键 |
| **N4 P1 返回** | 后退按钮 + CanExecute 刷新 + Journal 空 fallback + Workspace 返回目标 | NavigationCoordinator、MenuManager/Header、WorkspaceNavigationHandler | 任意页面可见后退可回主页 |
| **N5 P1 模块矩阵** | RoleDefinition RequiredModules 对齐 Home 模块；懒加载失败可见错误 | Doctor/Receptionist/SuperAdmin RoleDefinition、ModuleLazyLoader | 登录后首屏 Home 不空白；架构测试 4 |
| **N6 P2 对话框** | CommonDialogService 去 MessageBox；参数键 Pascal 统一；ShellDialogHelper | CommonDialogService、DialogManager、MessageDialogViewModel、ShellDialogHelper | 离开确认为 MDIX 对话框 |
| **N7 文档 SSOT** | 同步 desktop-ui-detailed-design §3/§5、02-desktop 导航节、需求状态列、compose/README 索引 | docs/… | grep 导航关键词文档已更新 |

每切片：`dotnet build LYBTZYZS.sln --no-incremental` 0/0 → 架构测试 → 相关 Desktop 测试 → commit。

---

## 9. 风险与待确认

| 风险/待确认 | 说明 |
|-------------|------|
| Receptionist 打开 MedicalCaseWorkspace 的服务端策略 | 若仅 Doctor 可写医案，UI 放开后 API 403；需权限矩阵确认 + 双控制器树 |
| ClinicalHome 删除 vs 保留 | 产品决策（§5.4） |
| KeepAlive 与 Journal 交互 | 实施 N1/N4 后做一次手工回归（选患者→看诊→后退） |
| SuperAdmin 加载 ClinicalModule 的启动耗时 | 可测量；若过重则保持懒加载但必须错误可见 |
| 正式文档 §5 PatientId 叙述 | N7 必须改，避免文档 SSOT 继续描述未实现契约 |

---

## 10. 验收清单（总）

- [ ] 四角色登录 → 主页无空白
- [ ] Receptionist：新建挂号（含读卡预填）→ 队列接诊 → MedicalCaseWorkspace 患者/医案正确
- [ ] Doctor：ClinicalWorkspace 选患者 → 开始看诊 → 工作台正确；返回工作台选中保留
- [ ] Admin/Sysadmin：卡片与侧栏全部可达；无权快捷键不出现
- [ ] 后退按钮：有历史回上一页；无历史回角色主页
- [ ] 无 MessageBox 业务确认；对话框 MDIX 风格
- [ ] `dotnet build --no-incremental` 0 错误 0 警告
- [ ] `dotnet test tests/LYBT.Tests.Architecture/` 全绿（含新增导航守卫测试）
- [ ] 正式文档与需求状态列已同步（代码-文档一致性红线）
