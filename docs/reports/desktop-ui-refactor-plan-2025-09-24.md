# 桌面端界面逻辑重构计划（2025-09-24）

## 1. 背景
- 基于 prism-8x-desktop-refactor-plan-2025-09-24.md 的服务层分析，确认桌面端存在事件定义重复、导航职责混乱、术语不统一等问题。
- 近期对 README 的校准表明项目仍处于“桌面端重构”阶段，需要针对界面逻辑制定明确计划，以配合事件统一与测试恢复。

## 2. 现状问题
1. **事件系统碎片化**：Core/Events 下存在多套事件（UnifiedEvents、ConsultationEvents、UnifiedEventArchitecture、Models/Events 等），导致编译错误与订阅混乱。
2. **工作台职责模糊**：MedicalWorkbenchMainView 集成患者列表、四诊录入、诊断、处方、管理入口，和需求中“诊疗流程 + 导航”目标不符。
3. **术语错位**：UI 与文档仍使用“看诊”“MedicalWorkbench”等旧称，与 docs/requirements/desktop-role-workflow-notes.md 不一致。
4. **导航链路复杂**：MainWindowViewModel、WorkbenchRouter、自定义 NavigationService 互相耦合，角色切换与模块加载逻辑重复、难以维护。
5. **会话状态不统一**：SessionManager 同时通过 EventAggregator 与 CLR 事件发布状态，订阅方逻辑重复。
6. **对话框实现过重**：WpfDialogService 内部硬编码 ViewModel 初始化，与 Prism IDialogService 重叠。

## 3. 重构目标
- 归一事件定义，恢复桌面端编译；所有状态事件集中在 UnifiedEvents.cs。
- 建立“登录 → 角色 → 工作台 → 子模块”的清晰导航链路，界面/文案与需求一致。
- 分离诊疗流程与管理模块，重构工作台布局与导航菜单。
- 精简对话框、Session、导航服务，保证 UI 状态来自单一入口。
- 为后续测试与文档更新奠定基础。

## 4. 分阶段计划
| 阶段 | 目标 | 时间窗口 | 验收结果 |
| --- | --- | --- | --- |
| Phase 0 – 解阻编译 | 统一事件文件、修复 Converter 引用 | 1-2 天 | ✅ dotnet build LYBT.Desktop.sln 成功 |
| Phase 1 – UI 架构调整 | 工作台职责拆分、导航链路重构、术语统一 | 3-5 天 | ✅ 新的诊疗工作台布局、角色导航图示 |
| Phase 2 – 功能收敛 & 测试 | 对话框精简、Session 统一、补齐测试与文档 | 5-7 天 | ✅ 关键 ViewModel 单测、对话框重构验收报告 |

## 5. 关键工作包
1. **事件统一与命名修复**
   - 保留 UnifiedEvents.cs，迁移所需 EventArgs，删除其它重复事件文件。
   - 统一 StatusMessageType 枚举，更新 SessionManager、UnifiedEventHandler、订阅方。
   - 为 WorkflowStep 引用正确命名空间或迁移至统一模型。

2. **工作台与导航重构**
   - MainWindowViewModel：拆分初始化、登录成功、角色导航逻辑，调用统一 WorkbenchRouter。
   - WorkbenchRouter：根据 docs/requirements/ui-workflow-spec.md 配置角色→工作台→默认视图；缓存结果供角色切换使用。
   - 重构 MedicalWorkbenchMainView：将诊疗流程（患者选择→四诊录入→诊断→处方）与管理模块解耦，可采用 Tab 或 Region 切换。
   - 更新 ModuleLoadingCoordinator 的优先级，确保按角色加载所需模块。

3. **Session 与状态管理**
   - 在 SessionManager 中采用单一的 EventAggregator 通道，提供 Login/Logout/Consultation 状态事件。
   - SessionAwareViewModel 改为统一订阅/释放逻辑，减少重复事件挂接。

4. **对话框与交互**
   - 精简 WpfDialogService：仅负责解析视图与 DataContext；初始化逻辑回归各 ViewModel（实现 ICustomDialogAware）。
   - 合并 CommonDialogService 与 Prism IDialogService，避免重复封装。

5. **术语与文档同步**
   - 替换 UI 与代码中的“看诊”“MedicalWorkbench”等旧称，使用“诊疗工作台”“诊疗流程”。
   - 更新 README、docs/requirements/desktop-role-workflow-notes.md、新增“桌面端导航指南”。

6. **测试与验收**
   - 新增单测：SessionManager 状态切换、UnifiedEventHandler 事件发布、WorkbenchRouter 角色导航。
   - 恢复服务器测试并补充与 UI 相关的契约验证。
   - 通过 docs/tasks/completed/ 输出阶段总结。

## 6. 验收标准
- dotnet build LYBT.Desktop.sln -c Release 通过，桌面端可启动基本导航。
- 登录 → 角色 → 工作台 → 子模块流程与需求文档一致。
- SessionManager 仅通过统一事件发布状态，订阅者更新顺滑。
- Dialog 与导航服务使用 Prism 官方接口，无多余包装。
- README、导航指南、任务列表更新到位。

## 7. 风险与依赖
- 事件归一涉及大量文件移动，需分步骤提交并做好回滚方案。
- 工作台拆分可能影响现有数据绑定和命令，需先设计草图再编码。
- 对话框重构涉及多个模块协作，需要提前对齐需求。
- 测试基线缺失，补充用例需额外时间。

## 8. 配套文档与任务
- 任务发布：docs/tasks/pending/2025-09-24-readme-校准任务.md（已创建），后续新增事件统一、工作台重构、对话框精简、测试基线任务。
- 文档更新：
  - README（已初步校准）
  - docs/requirements/desktop-role-workflow-notes.md
  - 新增 docs/development/desktop-navigation-guide.md（重构完成后补充）

---

> 本计划由 Thinker 持续维护，编码执行交由 Coder 完成。每个阶段结束需在 docs/tasks/completed/ 输出总结并同步 README。
