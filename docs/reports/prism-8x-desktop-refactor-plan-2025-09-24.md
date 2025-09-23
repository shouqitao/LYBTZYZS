# PRISM 8.x 桌面实现综合整改报告

## 目录
1. 总览
2. 现有文档结论回顾
3. 新增发现 —— Desktop Service 层/Consultation 域
4. Prism 8.x 实现问题归因
5. 重构路线图
6. 桌面端测试与保障现状
7. 需求与术语校准
8. 下一步行动建议

---

## 1. 总览
- 目标：整合 `prism-8x-audit-2025-09-23.md`、`prism-8-implementation-analysis-report.md`、`Prism_Implementation_Report.md`（下称“既有报告”）与本轮新增分析，形成全面的整改方案。
- 范围：WPF Prism 8.x Desktop 端，包括 Shell、Core、Services、Modules（尤其 Consultation）、Workbenches、测试体系。
- 方法：
  - 对照既有报告提炼共识；
  - 深入分析当前 Service 层与事件系统；
  - 评估 UI 工作流与角色导航实现；
  - 汇总测试/覆盖率现状与差距；
  - 补充需求与命名优化信息。

---

## 2. 现有文档结论回顾
### 2.1 共识亮点
- Prism 8.x 技术栈配置正确，使用 `Prism.DryIoc` 并实现模块化加载。
- Shell 模块按角色加载（System vs Consultation Workbench），具备 OnDemand 策略。
- ViewModel 基类（`ModernViewModelBase`）扩展了状态管理、错误处理。

### 2.2 既有报告指出的主要问题
- `MainWindowViewModel` 初始化流程断裂，`InitializeViewModel` 未被调用，导致登陆区域与导航未正确启动。
- 模块/服务注册重复（Shell 与模块同时注册），存在生命周期冲突风险。
- 自定义 NavigationService、DialogService 过度包装 Prism 原生能力。
- 事件系统碎片化，存在多个版本的同名 Event/Args 导致冲突。
- ViewModelCommand 样式不统一，缺少 `ObservesProperty/CanExecute` 等 Prism 约定。

---

## 3. 新增发现 —— Desktop Service 层/Consultation 域
### 3.1 Service 层现状
| 类别 | 代表文件 | 核心作用 | 问题 / 风险 |
|------|-----------|-----------|--------------|
| 会话管理 | `src/Client/Desktop/Core/Services/SessionManager.cs` | 统一维护当前用户、患者、诊疗状态并抛出事件 | 与 `Core/Interfaces`、`Models` 多套 EventArgs 重复；事件系统与 Prism EventAggregator 并行使用，导致订阅混乱。
| 对话框 | `Core/Services/WpfDialogService.cs`、`Desktop/Services/CommonDialogService.cs` | 自定义接口与 Prism `IDialogService` 并存 | 服务层硬编码 ViewModel 初始化逻辑，违反 MVVM 分层；Dialog 体系重叠。
| 导航 | `Core/Services/Navigation/NavigationService.cs` | 包装 RegionManager 与自定义 Workflow | Workflow Step 枚举定义在 `Core/Models/Consultation/WorkflowStep.cs`，但 XAML/事件仍引用 `ConsultationStep` 等旧定义，造成类型缺失错误（CS0246）。
| 性能/模块调度 | `ModuleLoadingCoordinator.cs` | 管理模块加载优先级 | Consultation Workbench 被标记为低优先级，但主流程依赖；需结合角色导航调整。

### 3.2 事件定义冲突根因
- `src/Client/Desktop/Core/Events/` 目录下存在至少 5 套事件定义：`UnifiedEvents.cs`、`UnifiedEventArchitecture.cs`、`ConsultationEvents.cs`、`ConsultationEventArgs.cs`、`Models/Events/EventArgs.cs`、`AuthEvents.cs`。
- `LoginSuccessEvent`、`LogoutEvent`、`PatientSelectedEvent` 等在多个文件中重复声明（class 与 enum 同名），编译时触发 CS0101/CS0263。
- `StatusMessageEventArgs` 同名多实现（字段结构不同），`MessageType` 与 `StatusMessageType` 两套枚举并存，导致字段初始化/访问报错（CS0236/CS0176）。
- `WorkflowStep` 枚举定义在 `Core.Models.Consultation`，但 `UnifiedEvents.cs` 未引用正确命名空间；`WorkflowStepNavigationEventArgs` 使用未 fully qualified 类型引发 CS0246。

### 3.3 Consultation 模块功能状态
- 前端将“Consultation”扩展为“诊疗工作台”全流程（患者列表、四诊录入、诊断、处方），与需求中“Consultation=诊断环节”的设计不符。
- `ConsultationWorkbenchMainView.xaml` 构建了完整工作台布局（患者列表/病历/四诊/处方），并以 Region 请求 `ConsultationMainView` 为默认子视图。
- ViewModel 层（`ConsultationMainViewModel`）结合 `SessionManager` 事件驱动业务，命名术语多与“诊疗”“工作台”混用。
- 需求指出需将“看诊”术语替换为更贴近实际的“诊疗”或拆分为诊断、管理等模块；现有实现需重命名并拆分职责。

---

## 4. Prism 8.x 实现问题归因
| 问题 | 影响 | 原因 | 解决方向 |
|------|------|------|----------|
| 事件/Args 重复定义 | 编译错误、事件风格不统一、订阅失效 | 多轮迭代叠加文件，缺少统一迁移策略 | 建立一套唯一事件定义（建议 `UnifiedEvents`），阶段性迁移老接口并清理冗余文件。
| ViewModel 初始化缺失 | 登录视图不出现，导航未启动 | `MainWindowViewModel.InitializeViewModel()` 未调用；注册方式绕开构造逻辑 | 将初始化动作放入构造或 `OnLoaded`，或调整 `ViewModelLocator` 注册为工厂。
| NavigationService 重包装 | 自定义 Workflow 处理与 Prism Region 功能冲突 | 期望统一工作流而重复造轮子 | 评估 Prism `IRegionNavigationJournal` 和 CompositeCommand 替代；保留必要的业务调度。
| DialogService 过载 | 对话框扩散多套接口 | 迁移未完成，旧代码仍被引用 | 收敛为 Prism `IDialogService` + 专用封装，移除硬编码初始化。
| Consultation 与 Workbench 职责不清 | “看诊”界面 monopoli 化 | 角色需求与实现偏差，命名不一致 | 重新划分模块：诊疗流程 vs 后台管理；调整区域布局与导航。

---

## 5. 重构路线图
### 5.1 分阶段计划
1. **Phase A – 编译修复与事件统一**
   - 目标：清除编译错误，让 Desktop 端可正常构建。
   - 行动：
     - 将事件定义整合到 `UnifiedEvents.cs`（或拆分为按域的单一文件），删除/过时标记 `ConsultationEvents.cs`、`ConsultationEventArgs.cs`、`UnifiedEventArchitecture.cs` 中冗余类。
     - 引入 `LYBT.Desktop.Core.Models.Consultation.WorkflowStep` 所需命名空间或迁移枚举。
     - 统一 `StatusMessageType/MessageType`，保留单一枚举；调整所有使用点。
     - 校正 XAML（`UnifiedDesignSystem.xaml`）中 Converter assembly 引用问题（检查 `LYBT.Desktop.Core` 是否输出 Converters；若已移动至其他程序集需修正 `xmlns`）。

2. **Phase B – Prism 基础对齐**
   - 完成 `MainWindowViewModel` 初始化重构，确保登录流程触发。
   - 清理自定义导航/对话服务，迁移到 Prism 原生模式；保留必要业务逻辑（如模块优先级）。
   - 对 Service 层依赖注入进行梳理，避免 Shell 与模块重复注册。

3. **Phase C – Consultation 域调整**
   - 与需求对齐：将“Consultation 工作台”拆分为角色工作台 + 诊疗流程页面；使用更贴近业务的命名（如“诊疗工作台”、“诊断流程”）。
   - `WorkflowStep` 重新定义：区分“看诊流程 Step”与“后台管理 Step”。
   - 重构 UI：保留患者列表、病历等组件，但明确模块边界（患者管理模块 vs 诊疗流程模块）。

4. **Phase D – 测试与覆盖率恢复**
   - 参考 `server-test-analysis-2025-09-23.md` 的恢复路线，将 Desktop/Shared 对应测试恢复。
   - 优先覆盖 Service 层（SessionManager、NavigationService）、事件集成与 ViewModel 关键逻辑。
   - 建立桌面端 UI 行为测试计划（可考虑 Prism 内部命令单测、集成 UI 自动化框架后期引入）。

### 5.2 关键里程碑
| 里程碑 | 验收标准 |
|--------|-----------|
| M1 | `dotnet build LYBT.Desktop.sln` 成功；Desktop Shell 登录界面正常显示；事件冲突清除。
| M2 | 登录 -> 角色导航 -> 对应工作台流程可手动操作，无运行时异常；自定义服务与 Prism 机制统一。
| M3 | Consultation 功能命名/角色流程符合最新需求文档；界面文案更新；存在流程文档。
| M4 | Desktop 关键服务单元测试通过，覆盖率较当前基线显著提升（目标 ≥30% 关键模块覆盖）。

---

## 6. 桌面端测试与保障现状
- 当前 `tests/` 目录主要覆盖 Server/Shared；桌面端几乎无自动化测试。
- 报告显示 Server 侧测试存在大面积失效，Desktop 端尚未纳入现有测试策略。
- 建议：
  - Phase D 开始前，明确桌面端测试策略（单元测试为主，必要时加入集成测试/快照测试）。
  - 与 `tests/TestCoverageStrategy.md` 对齐，规划 Desktop Service/ViewModel 的测试用例；可参考 Prism 官方示例中对 EventAggregator/Navigation 的单测方式。
  - 建立日志与诊断机制，减少 UI 手动验证成本。

---

## 7. 需求与术语校准
| 需求点 | 当前实现 | 差距/措施 |
|---------|-----------|-----------|
| 登录后按角色展示不同主界面 | `WorkbenchRouter` 根据角色返回 `System` or `Consultation` Workbench | 需确保角色映射与最新角色定义一致（`sysadmin` 兼容 `admin`）。
| 医生主界面包含诊疗/管理/历史查询 | `ConsultationWorkbenchMainView` 聚合流程，但管理功能依赖其他模块页面 | 需要在 Workbench 中提供导航菜单或区域切换，符合“无需回到主界面也可切换”需求。
| “看诊”术语调整 | 代码/界面混用“Consultation”、“工作台”、“看诊” | 梳理命名，将“Consultation”严格指代诊疗流程环节；界面文案更新。
| 角色字段与默认值 | User DTO/Session 默认角色 `Doctor` | 确认数据库/登录接口返回的角色值与 `WorkbenchRouter` 配置一致（`sysadmin` -> 管理员；普通用户 -> 医生）。
| 需求记录 | `docs/requirements/ui-workflow-spec.md` 已记录部分流程 | 可新增需求文档条目，补充角色导航、术语调整、优化点（见下述摘要）。

### 7.1 下一个优化点摘要
1. **事件体系统一化** —— 解决当前编译错误并简化发布订阅模型。
2. **工作台与诊疗流程解耦** —— 让 Consultation 名称回归“诊疗环节”，将工作台作为容器区域。
3. **对话框与导航服务整合** —— 减少自定义包装，提升 Prism 一致性。
4. **测试欠缺补齐** —— 制定桌面端测试策略，与 Server 侧恢复工作同步推进。

---

## 8. 下一步行动建议
1. **短期（1-2 天）**
   - 整理 `Core/Events` 文件夹，删除重复事件类，保证编译通过；修复 XAML Converter 引用。
   - 在 `docs/requirements/ui-workflow-spec.md` 补充角色导航与术语更新需求，确保团队共识。

2. **中期（本周内）**
   - 重构 `MainWindowViewModel` 初始化流程；统一 Session/Navigation 服务依赖注入方式。
   - 评估 Consultation 工作台 UI 的拆分方案，规划模块路由调整。

3. **中长期**
   - 建立桌面端单元测试基线；与 Server 测试恢复计划协同。
   - 逐步实现 Phase B/C/D 目标，形成阶段性 PR 与文档更新。

> **提示**：若在后续执行中发现仍有“ConsultationWorkbench”命名与“诊断”场景不符的情况，请在模块重构时同步更名，以免引导需求误解。
