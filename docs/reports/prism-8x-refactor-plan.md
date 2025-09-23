# Prism 8.x 重构方案

整合以下审计与分析成果，形成分阶段的重构行动计划：

- `docs/reports/prism-8x-audit-2025-09-23.md`
- `docs/analysis/prism-8-implementation-analysis-report.md`
- `Prism_Implementation_Report.md`

目标是在保持现有业务与 UltraThink 架构优势的前提下，修复高风险问题、统一实现模式，并为后续升级（如 Prism 9.x、CompositeCommand 等）奠定基础。

---

## 1. 重构目标

1. **可靠启动**：消除初始化隐患，确保 Shell/主窗口生命周期与依赖注入正确执行。
2. **依赖一致性**：统一服务注册与注入方式，移除 Service Locator/重复注册。
3. **导航规范化**：推广集中式导航服务，改进 Region 生命周期与历史管理。
4. **对话框治理**：简化 `WpfDialogService`，让 ViewModel 主导业务初始化逻辑。
5. **ViewModel 基线统一**：构造函数注入 + `BindableBase` 继承 + Prism 推荐接口。
6. **命令/事件优化**：全面采用 Prism 8.x 最佳实践（`DelegateCommand` 响应式、EventAggregator 策略）。
7. **可测试性提升**：通过 Facade/统一模式减少隐藏依赖，便于编写单元与集成测试。

---

## 2. 重构原则

- **最小化破坏**：每个阶段保持可运行；优先处理高风险缺陷。
- **模块自治优先**：业务服务注册、导航、对话框逻辑尽量留在模块内部。
- **显式依赖**：弃用 `ContainerLocator` 或隐式解析，改用构造注入/Facade。
- **分阶段交付**：每阶段结束必须通过 `dotnet build`, `dotnet test` 及关键场景冒烟。

---

## 3. 分阶段计划

### 阶段 1：启动与容器整合

- **MainWindow 启动修复**  
  - 将 `MainWindowViewModel.InitializeViewModel()` 整合进构造函数或工厂。  
    - 参考：`src/Client/Desktop/Shell/App.xaml.cs:71-73`、`src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:133-177`
  - 更新 `ConfigureViewModelLocator()`，使用工厂调用 `MainWindowViewModel.Create(...)` 或改为自动解析。
- **简化 `App.OnInitialized`**  
  - 将错误处理注册、预热任务迁移到 `RegisterTypes` 或专用启动服务。  
    - 参考：`src/Client/Desktop/Shell/App.xaml.cs:83-137`
- **服务注册去重**  
  - 确定单一注册来源（建议保留模块内注册）。  
    - 清理 `RegisterLayer*Modules` 中的业务服务注册（`src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs:320-352`）。  
    - 核对 `UsersModule` 等模块的 `RegisterTypes` 是否完整（`src/Client/Desktop/Modules/Users/UsersModule.cs:26-35`）。

### 阶段 2：导航与 Region 治理

- **集中导航服务落地**  
  - 在 Shell/模块/工作台 VM 中使用 `INavigationService` 注入，替换直接 `_regionManager.RequestNavigate`。  
    - 示例：`src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:433`、`src/Client/Desktop/Workbenches/SystemWorkbench/ViewModels/SystemWorkbenchMainViewModel.cs:200-268`
- **Region 生命周期规范**  
  - 移除对 `RegionManager.Regions.ContainsRegionWithName` 的轮询和手动清除。  
  - 采用 View `Loaded`/RegionAdapter 或 `IRegionNavigationJournal` 处理视图回退。
- **导航目标核对**  
  - 补齐 `ControlExamplesView` 定义/注册或删除对应命令（`src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:509`）。

### 阶段 3：对话框体系重构

- **重写 `WpfDialogService`**  
  - 将 `ShowDialogAsync(string ...)` 的分支/反射替换为：解析窗口 → 检测 `ICustomDialogAware` → 调用 `OnDialogOpened`。  
  - 让 ViewModel（如处方/药材/用户对话框）自行处理初始化逻辑。  
    - 当前问题代码：`src/Client/Desktop/Core/Services/WpfDialogService.cs:200-360`
- **统一对话框依赖**  
  - 全部使用 `ICustomDialogService` 或提供 `IDialogService` 适配层，避免模块间混用。  
    - 例：`src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionComposerViewModel.cs:31`

### 阶段 4：ViewModel 基类与依赖注入

- **移除 Service Locator**  
  - 取消 `ContainerLocator` 用法，改为显式注入或 Facade（`src/Client/Desktop/Core/ViewModels/Base/BaseListViewModel.cs:59-107`）。
- **统一基类实现**  
  - 确保所有 VM 继承 `BindableBase`，实现 `INavigationAware`/`IDestructible` 等接口。  
  - 更新 `ModernViewModelBase` / `ServiceViewModel` 模板，默认支持构造注入、ObservesProperty。
- **ViewModelServices Facade（可选）**  
  - 设计 `IViewModelServicesFacade` 聚合常用服务，减少构造参数数量。

### 阶段 5：命令与事件优化

- **命令响应式**  
  - 使用 `DelegateCommand` 的 `ObservesProperty/ObservesCanExecute`，移除手动 `RaiseCanExecuteChanged`。  
    - 示例：`src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:143-154`
- **事件聚合器策略**  
  - 若需要 `EnhancedEventAggregator`，在容器注册替换默认 `IEventAggregator`；否则移除未使用的实现（`src/Client/Desktop/Core/Events/EnhancedEventAggregator.cs`）。
- **日志/诊断清理**  
  - 移除手动写入桌面调试文件等临时代码，改用结构化日志。

### 阶段 6：验证与后续扩展

- **测试体系**  
  - 为对话框、导航、启动流程增加集成测试；使用容器验证确保注册无冲突。
- **文档同步**  
  - 更新开发文档、T4 模板与指南，反映新的 ViewModel/导航/对话框模式。
- **升级评估**  
  - 在完成上述重构后，评估 Prism 9.x、CompositeCommand、Prism.Validation 等增量特性。

---

## 4. 实施建议

1. **分支策略**：按阶段建立短期特性分支，逐步合入主干。
2. **回归验证**：每个阶段至少执行 `dotnet build`, `dotnet test`, 核心场景手动冒烟；对登录/工作台/对话框重点回归。
3. **Feature Flag（可选）**：对于导航服务替换等影响面大的改动，可引入开关平滑迁移。
4. **代码审查**：重构提交前后要进行专门的 Prism 设计复审，确保契合最佳实践。

---

## 5. 预期收益

- 启动流程稳定、可预测，避免界面空白或命令失效。
- 依赖关系显式化，方便测试与未来演进。
- 导航/对话框模式统一，降低维护成本。
- 代码更贴近 Prism 官方模式，为后续升级及团队培训铺平道路。

