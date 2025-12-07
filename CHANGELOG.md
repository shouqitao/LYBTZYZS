# Changelog

All notable changes to LYBTZYZS project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

#### Shell层架构整合 (OpenSpec: consolidate-shell-architecture) - 2025-12-07

**健康检查服务提取:**
- 新增`IHealthCheckCoordinator`接口和`HealthCheckCoordinator`实现
- 从`MainWindowViewModel`提取健康检查逻辑，降低ViewModel复杂度
- 健康检查通过事件驱动通知UI状态变更

**启动架构规范化:**
- 清理`ApplicationBootstrapper`废弃方法，仅保留`LoadModulesForRoleAsync`
- 确认`StartupPipeline`为唯一启动入口
- 更新`IApplicationBootstrapper`接口，移除死代码

**规范文档更新:**
- `login-ui`规范添加Purpose描述
- `shell-layout`规范添加Purpose描述和Shell层架构概述
- `Shell/README.md`更新Services目录结构

#### DetailView UI风格统一 (OpenSpec: unify-detail-view-style) - 2025-12-07

**操作模式统一:**
- 编辑按钮从列表页移至详情页右上角
- 所有5个ManagementView移除编辑按钮
- 统一操作流程：查看 -> 详情页 -> 编辑

**样式规范扩展:**
- 新增 `ui-style-conventions` 规范要求:
  - UI-010: Detail View Layout Convention (三行布局)
  - UI-011: Detail View Shared Styles (共享样式)
  - UI-012: Form Layout Flexibility (表单布局)
  - UI-013: Detail View Style Prohibition (禁止重复样式)

### Added

#### DetailView容器化重构 (OpenSpec: refactor-detail-view-container) - 2025-12-07

**新增容器组件:**
- `BaseDetailContainer` - 详情页容器控件，支持查看/编辑模式独立内容定义
- `InfoCard` - 信息卡片控件，用于查看模式下的信息分组展示

**容器化迁移:**
- `HerbDetailView` - 药材详情页
- `PatientDetailView` - 患者详情页
- `UserDetailView` - 用户详情页
- `FormulaDetailView` - 验方详情页
- `MedicalCaseDetailView` - 医案详情页

**过渡动画:**
- 页面加载淡入动画 (0.3s CubicEase)
- 查看/编辑模式切换动画 (0.25s 淡入+滑动)
- Footer 底部滑入动画

**新增OpenSpec规范:**
- `desktop-detail-views`: 详情页容器组件规范

#### Shell启动流程重构 (OpenSpec: refactor-shell-startup-flow) - 2025-12-05

**架构改进:**
- 引入`IApplicationLifecycle`状态机管理启动阶段（Initializing→Authenticating→Ready→Running）
- 使用`StartupPipeline`管道模式统一初始化流程
- 新增`LoginCoordinator`编排完整登录流程（认证→保存Token→启动会话→加载模块→导航）
- 新增`SessionLifecycleManager`管理会话状态和Token生命周期

**新增组件:**
- `IStartupStep`接口和5个实现步骤：ErrorHandling、ModuleCoordinator、CoreServices、ApiHealthCheck、Warmup
- `StartupPipeline`管道执行器
- `StartupDiagnostics`启动诊断日志

**精简优化:**
- `MainWindowViewModel`从18个依赖减少到15个，移除3个死方法
- `ApplicationBootstrapper`标记废弃方法，保留角色模块加载

**测试覆盖:**
- 新增91个Shell单元测试
- 覆盖Lifecycle、Login、Session、Startup、Diagnostics组件

#### 登录界面优化 (OpenSpec: remove-titlebar-add-close-button) - 2025-12-05

**无边框全屏界面:**
- 移除Windows标题栏 (WindowStyle="None")
- 添加登录界面关闭按钮(X)和Alt+F4拦截逻辑
- 已登录用户必须先退出登录才能关闭程序

**登录界面布局优化:**
- 左右分屏居中对称设计
- 诊所标题在左半边中心，登录框在右半边中心
- 登录框尺寸优化：460px宽，自适应高度
- 增大字体：主标题72px，副标题54px

**新增OpenSpec规范:**
- `login-ui`: 登录界面设计规范

### Removed

#### 废弃代码清理 (OpenSpec: cleanup-obsolete-code) - 2025-12-04

**Phase 1: 删除废弃API端点**
- 删除 `CacheHealthController.cs` 整个文件（运维功能，无Client调用）
- 删除 `HerbsController.BatchDeleteHerbs` 方法
- 删除 `FormulasController.BatchDeleteFormulas` 方法
- 删除 `MedicalCaseController.CompleteMedicalCase` 方法（已有PUT /{id}/status替代）
- 删除 `UsersController.BatchDeleteUsers` 方法
- 删除 `UsersController.ToggleStatus` 方法

**Phase 2: 删除未使用DTO类**
- 删除 `FormulaAnalysisDtos.cs` 整个文件（6个未使用DTO）
- 从 `MedicalCaseDtos.cs` 删除: CompleteMedicalCaseDto, SuspendMedicalCaseDto, ArchiveMedicalCaseDto, DoctorMedicalCaseStatisticsDto
- 从 `PatientOperationDtos.cs` 删除: PatientVisitHistoryDto, VisitRecordDto, PatientProfileManagementDto
- 从 `HerbOperationDtos.cs` 删除: HerbSpecialPriceDto, CompatibilitySuggestionDto

**清理统计:**
- 删除文件数: 2
- 删除API方法数: 6
- 删除DTO类数: 15
- 预计清理代码行: ~570行

### Changed

#### 项目README文档体系重构 (OpenSpec: document-project-architecture)

**文档精简:**
- 重写27个模块README，统一使用表格替代代码示例
- 文档总行数从21143行精简至3645行（减少83%）
- 标准化结构：项目定位→目录结构→核心组件(表格)→依赖关系→更新记录

**覆盖范围:**
- Server层: Entities, Infrastructure, 8个Module README
- Shared层: Models, Components, Utilities, Validators README
- Client Core: Presentation, Models, Infrastructure, Foundation, Contracts README
- Client Modules: Auth, Consultation, Formula, Herbs, MedicalCase, Patients, Prescriptions, Users README

**新增OpenSpec规范:**
- `project-architecture`: 整体项目架构规范
- `server-layer-architecture`: Server层架构规范
- `shared-layer-architecture`: Shared层架构规范
- `client-layer-architecture`: Client层架构规范
- `readme-documentation`: README文档规范(DOC-001至DOC-007)

#### UI层清理重构 (OpenSpec: cleanup-ui-layer)

**Phase 1: ViewModel重构**
- PrescriptionPanelViewModel拆分为7个Components (Calculator/Validator/ItemHandler/SaveHandler/ImportHandler/DataLoader)
- PatientSelectionViewModel引入MedicalCaseStartCoordinator处理医案启动流程
- 大型ViewModel保留1300+行但已最大化委托，剩余为核心ViewModel职责

**Phase 2: 样式统一**
- 建立全局样式系统 (`Shell/Styles/Colors.xaml`, `Typography.xaml`, `Controls.xaml`)
- 所有模块硬编码颜色迁移到全局Brush
- 新增状态色: SuccessLightBrush, WarningLightBrush, ErrorLightBrush

**Phase 3: 基础设施整理**
- 删除重复Shell服务 (`INavigationService`, `ThemeService`)
- 确认通知服务分层设计合理 (IUserNotificationService vs INotificationService)

**Phase 4: 交互模式标准化**
- 创建 `dialog-patterns` spec规范对话框使用模式
- 创建 `ui-style-conventions` spec规范样式约定
- 更新 `viewmodel-conventions` spec添加导航服务指南

**Phase 5: 验证和文档**
- Desktop UI测试: 147/147通过
- 更新 `viewmodel-development-guide.md` 添加样式/对话框/导航示例

#### WebAPI层重构 (OpenSpec: refactor-webapi-layer)

**Phase 1: Dead Endpoints清理**
- 标记废弃端点 `[Obsolete]` + `[ApiExplorerSettings(IgnoreApi = true)]`
- UsersController: `BatchDeleteUsers`, `ToggleStatus` 已废弃
- HerbsController: `BatchDeleteHerbs` 已废弃
- FormulasController: `BatchDeleteFormulas` 已废弃
- MedicalCaseController: `CompleteMedicalCase` 已废弃
- CacheHealthController: 整个Controller标记废弃待评估

**决策记录:**
- 批量删除模式统一为Client端循环模式
- 保留有设计意图的端点: `GetCurrentUser`, `CheckReference`, `BatchCheckReference`, `GetAllForExport`, `Search`

#### Service层重构 (OpenSpec: refactor-service-layer)

- 统一返回值类型：废弃`ServiceResult<T>`，统一使用`Result<T>`
- 引入Service基类：创建`BaseService`提供统一错误处理和`ExecuteAsync`方法
- MedicalCaseService拆分（消除God Class）：
  - `IMedicalCaseCommandService` - 创建/更新/删除操作
  - `IMedicalCaseQueryService` - 查询操作
  - `IMedicalCaseStateService` - 状态转换操作
- FluentValidation验证统一化，移除手工验证代码
- 创建`service-conventions` spec规范化Service设计模式

#### Repository层重构 (OpenSpec: refactor-repository-layer)

- 将`IRepository`/`IReadRepository`接口从Shared层移至Infrastructure层
- 统一所有Repository构造函数签名为`(AppDbContext context, ILogger logger)`
- 引入模板方法模式消除`GetPagedAsync`代码重复
  - `ApplyKeywordFilter` - 子类覆盖实现关键字过滤
  - `ApplyDefaultOrdering` - 子类覆盖实现默认排序
- 修复`UnifiedListViewModelBase`基类`commonDialogService`参数传递问题
- 创建`repository-patterns` spec规范化Repository设计模式

## [1.0.0] - 2025-11-09

### Added

#### 文档系统整合 (Issue #1933)

**Phase 2: Skills文档整合到docs/体系**
- 新增`docs/how-to/development/`目录，整合13个开发工具Skills文档
- 新增`docs/how-to/quality/`目录，整合6个质量保障Skills文档
- 新增`docs/how-to/testing/`目录，整合测试工具Skills文档
- 新增`docs/how-to/documentation/`目录，整合文档工具Skills文档
- 新增`docs/explanation/skills-overview.md` - Skills系统概述
- 新增`docs/explanation/skills-collaboration.md` - Skills协同模式指南
- 新增`docs/explanation/automation-system.md` - 自动化工作流系统说明
- 更新`docs/index.md`添加Skills文档索引（21个Skills操作指南）

**Phase 3: spec-workflow归档与steering/文档迁移**
- 新增`docs/explanation/product-vision.md` - 产品愿景与战略目标（从.spec-workflow/steering/迁移）
- 新增`docs/explanation/project-structure.md` - 项目结构与组织指南（从.spec-workflow/steering/迁移）
- 新增`docs/archive/`目录 - 文档归档中心
- 新增`docs/archive/README.md` - 归档索引和归档原则说明
- 新增`docs/archive/spec-workflow-legacy-2025-11-09/` - .spec-workflow完整归档
- 新增`docs/archive/spec-workflow-legacy-2025-11-09/MIGRATION.md` - 详细迁移映射说明
- 更新`docs/index.md`添加"项目愿景与结构"小节

**Phase 5: 文档验证与质量改进**
- 新增`docs/reports/documentation-consolidation-phase1-analysis-2025-11-09.md` - Phase 1分析报告
- 新增`docs/reports/documentation-consolidation-final-report-2025-11-09.md` - 最终整合报告
- 新增`CHANGELOG.md` - 项目变更日志（本文件）

### Changed

#### 文档系统整合 (Issue #1933)

- 更新`docs/index.md` - 修正无效文档链接，验证114个链接全部有效
- 更新`.claude/skills/`中24个Skills文档的内部引用，指向新的docs/路径

### Deprecated

#### 文档系统整合 (Issue #1933)

- `.spec-workflow/` 目录已归档到`docs/archive/spec-workflow-legacy-2025-11-09/`
  - `steering/product.md` → 已迁移至`docs/explanation/product-vision.md`
  - `steering/structure.md` → 已迁移至`docs/explanation/project-structure.md`
  - `steering/constitution.md` → 内容已整合至`docs/explanation/architecture/principles.md`
  - `steering/tech.md` → 内容已整合至`docs/explanation/architecture/principles.md`和ADR文档
  - `specs/` → 已废弃，改用GitHub Issues + 标准文档流程
  - `approvals/` → 已废弃，改用GitHub PR Review机制

### Removed

#### 文档系统整合 (Issue #1933)

- 删除`docs/index.md`中2个无效文档链接：
  - `explanation/architecture/server/interfaces-layer-design.md`（文档不存在）
  - `reports/medicalcase-consultation-prescription-current-status-analysis-2025-10-24.md`（文档已删除或重命名）

### Fixed

#### 文档系统整合 (Issue #1933)

- 修正多套文档体系并存问题（.spec-workflow/, docs/, .claude/skills/）
- 修正GitHub同步缺失问题（.claude/skills/文档未同步到GitHub）
- 修正文档定位不清问题（steering/文档与docs/explanation/高度重复）
- 修正Spec工作流未使用问题（specs/和approvals/目录从未实际使用）

---

## 变更分类说明

- **Added**: 新增功能、文件或文档
- **Changed**: 现有功能或文档的变更
- **Deprecated**: 即将废弃的功能或文档
- **Removed**: 已删除的功能或文档
- **Fixed**: Bug修复或问题解决
- **Security**: 安全相关的修复或改进

---

**最后更新**: 2025-11-09
