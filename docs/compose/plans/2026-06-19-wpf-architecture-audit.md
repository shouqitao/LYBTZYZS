# WPF 前端架构审计 — 2026-06-19

> Task 1 交付物。Read-only 分析，为 wpf-frontend-arch-refactor 计划的后续任务提供依据。

## A1. 大文件（>300 行，40 个）

**重构计划直接目标（核心架构文件）：**

| 行数 | 文件 | 对应任务 |
|------|------|---------|
| 786 | Shell/ViewModels/MainWindowViewModel.cs | （前置已拆 Token，仍偏大） |
| 590 | Infrastructure/ViewModels/MasterDetailViewModelBase.cs | Task 2 |
| 571 | Infrastructure/Navigation/EnhancedNavigationService.cs | Task 3 |
| 525 | Infrastructure/Navigation/NavigationAnalyticsService.cs | Task 3 相关 |
| 442 | Models/ViewModels/Base/NavigableViewModelBase.cs | Task 2 |
| 419 | Shell/Services/NavigationCoordinator.cs | Task 3 |
| 316 | Shell/ViewModels/AccountSettingsViewModel.cs | — |

**业务逻辑文件（"天然偏大"，非架构问题）：**
PrescriptionPrintService (659)、MedicalCaseWorkspaceViewModel (653)、LoginViewModel (637)、MedicalCaseService (624)、HttpClientApiClient (612)、HistoryCopyDialogViewModel (464)、MedicalCaseCommandsViewModel (487)、TokenRefreshHandler (470)、CredentialVault (470) 等。

**自动生成 / 资源（不可改）：** StringResources.Designer.cs (512)。

## A2. 跨模块非法引用

仅 3 处，全部集中在 Registration 模块（领域耦合，可能合理）：

| 源模块 | 引用 | 文件 |
|--------|------|------|
| Registration | MedicalCase | RegistrationListViewModel.cs |
| Registration | Patients | RegistrationCreateDialogViewModel.cs |
| Registration | Users | RegistrationCreateDialogViewModel.cs |

其他模块无跨模块直接引用（均通过 Contracts 层）。

## A3. DI 注册分布

**Shell/Extensions + Infrastructure/DI（7 个文件，~814 行）：**
| 文件 | 行数 | 职责 |
|------|------|------|
| ServiceCollectionExtensions.cs | 185 | 应用服务 |
| LoggingRegistrationExtensions.cs | 162 | 日志 |
| PrismConfigurationExtensions.cs | 125 | Prism |
| UnifiedApiClientExtensions.cs | 120 | API 客户端 |
| HttpServiceRegistrationExtensions.cs | 80 | HTTP |
| ViewModelServicesExtensions.cs | 78 | VM 服务 |
| DataSourceRegistrationExtensions.cs | 64 | 数据源 |

各 Module.cs 的 DI 调用很少（0-2 个），主要注册已在 Extensions 文件中。当前按职责分文件，组织清晰。

## A4. UI 硬编码颜色（89 处）

| 位置 | 数量 | 性质 |
|------|------|------|
| DesignSystem.xaml | ~30 | **Token 定义本身（正确，保留）** |
| LoginView.xaml | 23 | 刚重建的登录界面（刻意美学） |
| PreviewStyles / PanelStyles / MedicalCaseStyles / ValidationStyles | ~20 | 旧样式文件，可 token 化 |
| Printing 模板（4 个） | ~12 | 打印模板固定配色（纸张输出，有意） |

真正可清理的硬编码颜色：旧样式文件 ~20 处。LoginView 与打印模板应保留。

---

## 关键判断（供决策）

1. **跨模块耦合面极小**（仅 3 处，且可能是合理领域耦合）——架构层并未"散架"。
2. **导航拆分（Breadcrumb + History managers）刚在前置任务中完成**——Task 3 提议把 NavigationCoordinator 与 EnhancedNavigationService 重新合并，可能与刚做的拆分方向相反，且合并后单文件接近 900 行。
3. **DI 当前 7 文件按职责分，清晰**——合并成 2 文件会丢失职责划分，收益不明确。
4. **LoginView 刚重建**——立即替换 23 处颜色为 token 是返工。
5. **真正低风险高价值的**：Task 6（TODO/死代码清理）+ 旧样式文件 token 化（A4 中 ~20 处）。

---

## 执行结果（2026-06-19）

用户确认 scoped subset 范围 + 在 master 上直接工作。已完成并通过验证：

### ✅ 已完成（已验证）

**Task 2 — ViewModel 基类（MINOR_CLEANUP）：** 删除 `NavigableViewModelBase` 中 6 个零调用/零重写的 protected 方法（`GetNavigationParameter<T>` ×2、`TryGetNavigationParameter<T>`、`NavigateBack(string)`、`GetCurrentUserInfo`、`IsUserLoggedIn`）。**未改动继承层级** —— 分析证实计划前提错误：`MasterDetailViewModelBase` 是 `NavigableViewModelBase` 的兄弟而非第三层（受 Models→Infrastructure 项目依赖方向强制），4 层结构是合理的 Prism MVVM 模式，23 个屏幕继承这些基类，重构会危及全部界面。

**Task 6 — 死代码清理：** 删除整个导航分析死代码簇：
- `NavigationAnalyticsService.cs`（525 行）
- `INavigationAnalyticsService.cs`
- `NavigationShortcuts.cs`
- `NavigationServiceRegistration.cs`（两个重载零调用）
- `EnhancedNavigationService.Phase4_Analytics.cs`（185 行 partial）
- `NavigationAnalyticsServiceTests.cs`
- 移除 `EnhancedNavigationService.cs` 中孤立的 `partial void OnAnalyticsInitialized()` 声明

Track* 方法从未被 live 主 partial 调用（仅 analytics partial 内部互调），`SuggestionType` 保留（live，4+ 文件在用）。

### 验证证据

- `dotnet build tests/LYBT.Tests.Desktop` — **0 错误**（44 警告，均为既有）
- 基线对照：`git stash` 后在干净 HEAD 运行 `NavigableViewModelBaseTests` + `BreadcrumbBarTests` + `EnhancedNavigationServiceTests` —— **同样 18 个失败**。证明这些失败为既有问题（NSubstitute 无法代理 Prism `NavigationContext`、WPF 控件需 STA 线程、控制器集成测试需 DB/server），与本次删除无关。
- 全量 `dotnet test` 的 185 失败 / 793 通过中，超时（5min）也是因素；失败签名均与本次改动无关。

### ⏸️ 延后（需设计判断，非低风险快修）

**样式 token 化（Task 5 子集）：** 经查，旧样式文件（PreviewStyles/PanelStyles/MedicalCaseStyles）使用 Fluent 风格调色板（#201F1E/#605E5C/#107C10/#00B7C3 等），与 DesignSystem token（#212121/#757575/#2E8B57）**多数不匹配**。机械替换会改变视觉外观（实为重设计）。仅 ~2 个颜色精确匹配。需设计判断，延后。

**Registration 跨模块引用：** 3 处（RegistrationCreateDialogViewModel → Patients/Users；RegistrationListViewModel → MedicalCase）。技术上违反 Desktop AGENTS.md "模块间禁止互引" 规则，但属合理领域耦合。正确修复需引入 Contracts 层抽象（如 `IPatientLookupService`），是需 brainstorm 的设计重构，延后。

### ⏭️ 未执行（用户依据审计结论决定跳过）

- **Task 3（合并导航服务）：** 与前置刚完成的拆分方向相反，合并后单文件接近 900 行。
- **Task 4（DI 7→2 合并）：** 当前 7 文件按职责分，清晰；合并丢失组织，收益不明。
- **Task 5 的 LoginView 部分：** 登录界面刚重建，23 处硬编码颜色为刻意美学，立即 token 化是返工。
