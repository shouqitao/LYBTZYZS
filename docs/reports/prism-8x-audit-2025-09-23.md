# Prism 8.x 实现审计报告（LYBT 桌面端）

本报告基于项目当前代码（Prism 8.1.97 + WPF + DryIoc）进行静态审计，聚焦 Prism 启动、模块化、区域/导航、事件与对话框等关键实现，给出问题与改进建议。

## 结论摘要

- 框架选型与基础架构整体正确：使用 `Prism.DryIoc`、`Prism.Wpf`，`App` 继承 `PrismApplication`，模块化与区域导航均按 Prism 8 规范实现。
- 主要风险与改进点：
  - MainWindowViewModel 未执行初始化方法，导致登录导航与命令未初始化（重大）。
  - 导航集中化服务已实现但未在全局替换 RegionManager 直连用法（中等）。
  - 模块服务在 Shell 与各模块重复注册，存在重复/冲突风险（中等）。
  - 自定义 EnhancedEventAggregator 未注册未使用（可清理）。
  - 个别导航目标视图未注册或缺失（潜在）。

## 版本与环境

- Prism 版本：`8.1.97`（集中版本管理）
  - 见 `Directory.Packages.props:47-50`
- DI 容器：DryIoc
  - 见 `src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj:34-40`

## 启动与容器（Bootstrapping & DI）

- App 继承与 Shell 创建：正确
  - `src/Client/Desktop/Shell/App.xaml:1`
  - `src/Client/Desktop/Shell/App.xaml.cs:41` `CreateShell()` 解析 `MainWindow`
- 类型注册：集中在扩展方法中完成
  - `src/Client/Desktop/Shell/App.xaml.cs:52-61` 调用 `containerRegistry.RegisterAllServices()`
  - 导航服务注册：`src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs:370-371`
- 模块目录：WhenAvailable + OnDemand 合理
  - `src/Client/Desktop/Shell/App.xaml.cs:101-153` 角色化按需加载封装

问题（重大）：MainWindowViewModel 初始化未执行
- 代码现状：`ViewModelLocationProvider` 直接解析 VM 实例
  - `src/Client/Desktop/Shell/App.xaml.cs:71-73`
- VM 初始化逻辑在 `InitializeViewModel()`，但未被调用
  - `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:133-164`
  - 预期触发的登录导航、命令初始化、事件订阅均在此方法内
- 后果：
  - 登录区 `LoginRegion` 可能不会导航到 `LoginView`（界面空白）
    - `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:387`
  - 顶部按钮与快捷键命令未初始化

建议修复（二选一）：
- A. 使用工厂注册 VM，并在工厂内调用 `Create(...).InitializeViewModel()`
  - 参考：在 `ConfigureViewModelLocator()` 中通过 `Container.Resolve<...>` 取依赖，调用 `MainWindowViewModel.Create(...)`
- B. 将初始化逻辑移入构造函数或 `OnLoaded` 钩子，避免额外初始化方法

## 模块化（Modules）

- 模块实现：各模块 `IModule` 的 `RegisterTypes/OnInitialized` 基本规范
  - 例：`src/Client/Desktop/Modules/Patients/PatientsModule.cs:27-45`
- 风险：服务重复注册（Shell 与模块均注册）
  - 例：用户、患者等服务在 Shell 的分层注册与模块内同时注册（单例/Scoped 混用）
  - 可能引发：解析不确定性、最后注册覆盖、生命周期混乱

建议：
- 统一注册边界：
  - 要么完全由各模块在 `RegisterTypes` 内注册；
  - 要么保留 Shell 侧集中注册并删除模块内重复注册。
- 建议采用模块内注册，降低耦合、利于独立演进与测试。

## 区域与导航（Regions & Navigation）

- 区域定义与导航：整体符合 Prism 规范
  - Shell 区域：`ContentRegion`、`LoginRegion`（`src/Client/Desktop/Shell/Views/MainWindow.xaml:44, 99`）
  - 工作台区域：`SystemWorkbenchContentRegion`、`MedicalWorkbenchContentRegion`
    - `src/Client/Desktop/Workbenches/SystemWorkbench/Views/SystemWorkbenchMainView.xaml:156`
    - `src/Client/Desktop/Workbenches/MedicalWorkbench/Views/MedicalWorkbenchMainView.xaml:73`
- 导航注册：大部分视图均使用 `RegisterForNavigation<TView[, TViewModel]>`
  - 例：`src/Client/Desktop/Modules/Patients/PatientsModule.cs:37-40`

问题（中等）：导航集中化服务未得到全局采用
- 已实现集中式导航服务：`src/Client/Desktop/Core/Services/Navigation/NavigationService.cs`
- 但大量代码仍直接使用 `IRegionManager.RequestNavigate`
  - 例：`src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:433`、
        `src/Client/Desktop/Modules/*/ViewModels/*ViewModel.cs` 多处
- 建议：逐步替换为集中式 `INavigationService`，统一历史、错误处理与回退逻辑

问题（次要）：手动等待/清理 Region 视图，易脆弱
- 示例：在 SystemWorkbench 主 VM 中手动检测/等待 Region 是否存在并清理视图
  - `src/Client/Desktop/Workbenches/SystemWorkbench/ViewModels/SystemWorkbenchMainViewModel.cs:205` 开始的区域存在性轮询与清理
- 建议：
  - 利用 Prism 的 Region 生命周期与 `NavigationJournal`，避免主动清理全部视图；
  - 若需等待视图加载，倾向在 View 的 `Loaded` 事件或 RegionAdapter 层处理。

问题（潜在）：导航目标缺失
- `ControlExamplesView` 在导航中被引用，但未发现注册/定义
  - `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:509`
- 建议：如保留命令需补齐注册；否则移除该导航代码。

## 事件（EventAggregator）

- 使用 Prism 自带 `IEventAggregator` 与 `PubSubEvent`，订阅/退订规范
  - 例：`src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:155-157, 582-594`
- 增强聚合器未使用：
  - `src/Client/Desktop/Core/Events/EnhancedEventAggregator.cs` 定义了增强版，但未注册/注入到系统
  - 建议：确认不需要则删除；如需保留请在容器中以 `IEventAggregator` 实现注册替换默认实现

## 对话框（Dialogs）

- 自定义对话框服务：`ICustomDialogService` + `WpfDialogService`（集中注册）
  - `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs:430-467`
- 同时存在 Prism `IDialogService` 的直接依赖（例如处方模块）
  - `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionComposerViewModel.cs:31, 46-61`

风险：对话框服务双轨并存
- 如果未启用 Prism 默认 `IDialogService` 注册（依赖具体版本/配置），将导致解析失败
建议：
- 统一一种对话服务：要么全面使用 Prism `IDialogService`（并通过 `RegisterDialog` / `RegisterDialogWindow` 配置），要么在所有 VM 中改为注入 `ICustomDialogService`。

## 具体改进清单（可执行）

1) 修复 MainWindowViewModel 初始化（高优先）
- 将 `InitializeViewModel()` 纳入构造或通过 ViewModelLocator 工厂调用。
  - 参考：`src/Client/Desktop/Shell/App.xaml.cs:71-73`、`src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:133-177`

2) 消除服务重复注册（高优先）
- 选择“模块内注册”或“Shell 侧集中注册”之一，移除另一侧重复项。
  - 例：`UsersModule` 与 `ServiceCollectionExtensions` 对用户服务的重复注册
    - `src/Client/Desktop/Modules/Users/UsersModule.cs:27-36`
    - `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`（多处）

3) 推进导航集中化（中优先）
- 将直接 `IRegionManager.RequestNavigate` 的代码迁移到 `INavigationService`，并充分利用其历史/失败事件。
  - 参考：`src/Client/Desktop/Core/Services/Navigation/NavigationService.cs`

4) 规范 Region 生命周期处理（中优先）
- 避免在 VM 中手动清理 Region 全部视图，改用导航期望、`IRegionNavigationJournal`、或在 View 层处理。
  - 参考：`src/Client/Desktop/Workbenches/SystemWorkbench/ViewModels/SystemWorkbenchMainViewModel.cs:205-219, 241-268`

5) 统一对话框服务（中优先）
- 若继续使用 Prism 对话框：显式注册 `IDialogService`/对话窗口，并替换自定义接口；或反之。

6) 清理未使用的增强聚合器（低优先）
- `src/Client/Desktop/Core/Events/EnhancedEventAggregator.cs`

---

如需，我可以基于上述建议提交一组最小改动的 PR（不改变业务逻辑）：
- 修复 MainWindow VM 初始化；
- 去重一处服务注册（示范模式）；
- 将一处直接 Region 导航改为集中式导航；
- 补注册或删除 `ControlExamplesView` 导航。

