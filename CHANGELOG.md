# Changelog

All notable changes to LYBTZYZS project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

#### Master-Detail布局重构 (OpenSpec: refactor-master-detail-layout) - 2025-12-18 [已归档]

**状态**: ✅ 100%完成，已归档

**Phase 1-1.5: 基础架构控件**
- 创建MasterDetailLayout通用控件（左右分割布局、GridSplitter可调节）
- 创建SearchBox、DetailToolbar、EmptyState、LoadingOverlay、DataGridToolbar控件
- 创建IMasterDetailViewModel接口和MasterDetailViewModelBase基类

**Phase 2-5: 模块重构**
- Patients: PatientMasterDetailView/ViewModel
- Users: UserMasterDetailView/ViewModel
- Herbs: HerbMasterDetailView/ViewModel
- Formula: FormulaMasterDetailView/ViewModel

**Phase 6: 清理**
- 删除15+个废弃Management组件（View+ViewModel+Tests）
- 更新模块注册，MasterDetail视图作为默认

**技术成果**:
- 统一基础数据管理为Master-Detail模式
- 减少代码冗余，提升用户体验

---

#### 实体数据流优化 (OpenSpec: optimize-entity-data-flow) - 2025-12-18

**Phase 1: MasterDetail完整性验证 (已完成)**
- 验证5个模块MasterDetail视图功能完整性：Formula, Herb, Patient, User, MedicalCase
- 确认所有CRUD操作正常：列表加载、新增、编辑、删除、搜索筛选
- 确认AdminHome已指向MasterDetail视图而非Management视图

**Phase 2: Management组件标记过时 (已完成)**
- 标记10个Management组件为[Obsolete]：
  - FormulaManagementViewModel/View
  - HerbManagementViewModel/View
  - PatientManagementViewModel/View
  - UserManagementViewModel/View
  - MedicalCaseManagementViewModel/View
- 编译验证通过 (0 错误)

**Phase 3: User模块DTO迁移 (已完成)**
- Server端:
  - IUserService添加GetPagedListAsync方法返回UserListDto
  - UserService实现GetPagedListAsync方法
  - UsersController添加GET /api/v1/users/list端点
- Client端:
  - IUserApi添加GetUsersListAsync (Refit接口)
  - IUserRepository/UserRepository添加GetPagedListAsync方法
  - UserCommandHandler添加GetPagedListAsync方法
  - UserMasterDetailViewModel泛型参数从UserDto迁移到UserListDto
- 采用增量API策略，原有方法保持不变确保向后兼容
- 编译验证通过 (0 错误)

**HttpClient层评估结论:**
- 当前架构已规范化：Refit + Repository模式
- 无需预先重构，采用增量扩展策略支持ListDto

**技术决策:**
- 遵循Pre-Release Stabilization原则，使用[Obsolete]保持向后兼容
- Phase 3.1完成：User模块列表视图使用轻量级UserListDto
- 保持程序可随时运行，渐进式迁移

#### DTO简化重构 (OpenSpec: refactor-dto-simplification) - 2025-12-18

**重构目标:**
- 消除DTO继承链，采用扁平化设计
- 统一四种核心DTO类型：ListDto, DetailDto, InputDto, Statistics
- InputDto设计原则：排除Status/系统字段/展示字段
- Desktop本地Model不使用Dto后缀(消除命名歧义)

**Phase 3完成项:**
- Prescription模块: 新扁平化DTO就位，旧继承链类标记[Obsolete]
- Formula模块: 移除FormulaInputDto的IRemarkable接口继承
- Query/Search DTO: 标记6个DTO为[Obsolete]（Prescription/Formula/Herb各2个）
- Patient模块: 修复Desktop层9处PatientInputDto.Status引用
- Consultation模块: 创建ConsultationListDto，ConsultationInputDto移除展示字段
- User模块: 创建UserListDto/UserDetailDtoNew/UserStatistics，保留UserInputDto.Status(安全例外)

**Phase 4完成项:**
- Desktop层命名消歧: PrescriptionPrintDto → PrescriptionPrintModel
- Desktop层命名消歧: PrescriptionItemPrintDto → PrescriptionItemPrintModel

**标记[Obsolete]的类:**
- PrescriptionInputBaseDto, PrescriptionCreateDto, PrescriptionEditDto
- PrescriptionQueryDto, PrescriptionSearchDto
- FormulaQueryDto, FormulaSearchDto
- HerbQueryDto, HerbSearchDto
- UserDto, UserQueryDto, UserSearchDto

**新增文件:**
- ConsultationListDto.cs - 诊疗列表视图DTO
- UserListDto.cs - 用户列表视图DTO
- UserDetailDtoNew.cs - 用户详情DTO(扁平化)
- UserStatistics.cs - 用户统计DTO(record)

**重命名文件:**
- PrescriptionPrintDto.cs → PrescriptionPrintModel.cs

**技术决策:**
- 遵循Pre-Release Stabilization原则，使用[Obsolete]保持向后兼容
- InputDto排除展示字段(PatientName/DoctorName)，由服务层填充
- Desktop本地Model不使用Dto后缀，避免与Shared层DTO混淆
- UserInputDto.Status为安全例外（用户账户启用/禁用功能需要前端可控）

#### 统一枚举定义到Shared层 (OpenSpec: unify-enums-to-shared) - 2025-12-17

**重构内容:**
- 合并重复的ErrorCategory/ErrorSeverity枚举定义到ErrorEnums.cs
- 迁移分散枚举：MedicalCaseUpdateMode、BusinessOperation、PasswordStrength
- 清理所有枚举的冗余[JsonConverter]属性（已全局配置JsonStringEnumConverter）
- 移除ToChinese()扩展方法，统一使用GetDescription()

**新增文件:**
- ErrorEnums.cs - ErrorCategory和ErrorSeverity枚举
- SecurityEnums.cs - PasswordStrength枚举
- ValidationEnums.cs - BusinessOperation枚举

**删除文件:**
- Contracts/Common/ErrorCategory.cs
- Contracts/Common/ErrorSeverity.cs
- Errors/ErrorCategory.cs

**技术决策:**
- 中文显示统一使用[Description]属性
- JSON序列化通过全局配置，无需单独标注
- 完整重构而非别名兼容模式

#### 侧边栏组件化与返回主页功能 (OpenSpec: refactor-role-navigation) - 2025-12-16

**功能实现:**
- SidebarControl组件化：从MainWindow提取约130行侧边栏UI代码
- 返回主页按钮：在侧边栏菜单区顶部添加，支持角色感知导航
- 角色导航映射：Admin/SuperAdmin→AdminHomeView，Doctor→ClinicalHomeView

**技术要点:**
- DependencyProperty实现控件数据绑定
- 复用UnifiedViewModelBase.NavigateToHomeCommand避免重复代码
- ApiHealthStatusToTextConverter从Shell下沉到Infrastructure

**新增文件:**
- SidebarControl.xaml/xaml.cs - 侧边栏控件
- BoolToDoubleConverter.cs - 侧边栏宽度转换器
- ApiHealthStatusToTextConverter.cs - API状态文本转换器

#### 处方打印功能增强 (OpenSpec: print-prescription-slip, enhance-prescription-print) - 2025-12-15

**功能实现:**
- XAML模板实现A5处方笺布局(PrescriptionPrintTemplate.xaml)
- FixedDocument实现WYSIWYG所见即所得打印
- 打印预览窗口左右分栏布局(设置面板+DocumentViewer)
- 支持A5/A4纸张尺寸动态切换
- 打印机选择、份数设置功能
- 所有字段下划线两端对齐(Grid布局)
- 签名行(医师签字/审核/调配)留空供手写

**技术要点:**
- UserControl转FixedPage技术(Measure/Arrange/UpdateLayout)
- IAddChild接口添加页面到FixedDocument
- 动态纸张尺寸切换重建文档
- ClinicSettingsService管理诊所配置

**新增文件:**
- PrescriptionPrintTemplate.xaml/xaml.cs - XAML打印模板
- ClinicSettings.cs - 诊所配置模型
- IClinicSettingsService/ClinicSettingsService - 诊所配置服务

#### 重复药材提醒逐个确认 (OpenSpec: enhance-duplicate-herb-dialog) - 2025-12-14

**功能改进:**
- 处方导入/历史复制时重复药材提醒从批量对话框改为逐个确认
- 每个重复药材单独弹窗显示"[药材名] 重复"，医生逐个确认
- 剂量合并策略可配置化(appsettings.json Prescription节点)
  - 支持5种策略: Max(默认)/Min/Sum/Import/Keep

**技术要点:**
- 使用TaskCompletionSource实现异步等待用户确认
- 新增IPrescriptionSettingsService接口和实现
- 静态访问器模式供POCO类(DuplicateHerbInfo)访问配置
- 同时适用于验方导入和历史处方复制

#### 历史医案复制对话框UI重设计 (OpenSpec: redesign-history-copy-ui) - 2025-12-13

**UI布局重构:**
- 对话框采用左右双栏布局 (400:*)
- 左栏: 搜索区 + 医案列表(显示所有医生的医案)
- 右栏: 复用MedicalCaseViewControl显示医案详情预览

**功能修复:**
- 修复"查看全部患者"功能0条记录问题
- 修复处方药材组合绑定路径(使用Prescription.导航属性)
- 新增GetPagedIncludeAllDoctorsAsync API支持跨医生查询

**技术要点:**
- WPF XAML数据绑定导航属性模式
- MedicalCaseDetailDto.Prescription嵌套绑定

#### MedicalCase UI架构统一 (OpenSpec: unify-medicalcase-view-edit-pattern) - 2025-12-13

**架构重构:**
- 统一BaseDetailContainer ViewContent/EditContent模式
- 15个任务全部完成 (Phase 0: 3, Phase 1: 6, Phase 2: 6)

**技术规范:**
- 使用DependencyProperty接收数据对象
- Prism MVVM模式
- Master-Detail对话框布局

#### 验方导入对话框UI重设计 (OpenSpec: redesign-formula-import-ui) - 2025-12-13

**UI布局重构:**
- 对话框尺寸调整为 1100x680
- 左右双栏布局 (320:*)
- 左栏: 搜索区 + 分类筛选 + 验方卡片列表
- 右栏: 复用FormulaViewControl显示验方详情

**功能增强:**
- 分类筛选下拉框 (全部 + 各分类)
- 搜索支持名称、适应症、功效字段
- 选中验方异步加载详情
- 空状态提示

#### DetailView控件提取重构 (OpenSpec: extract-detail-controls) - 2025-12-13

**新增独立预览/编辑控件:**
- FormulaViewControl + FormulaEditControl (验方模块)
- HerbViewControl + HerbEditControl (药材模块)
- PatientViewControl + PatientEditControl (患者模块)
- UserViewControl + UserEditControl (用户模块)
- MedicalCaseViewControl (医案模块，无标准编辑模式)

**重构收益:**
- 控件与ViewModel解耦，支持多场景复用
- FormulaImportDialog右侧面板复用FormulaViewControl
- 各DetailView统一使用BaseDetailContainer布局
- 使用DependencyProperty接收数据对象

**技术规范:**
- 控件位于各模块Controls目录
- 通过DependencyProperty绑定数据
- 新增17个文件，代码复用率提升

#### 处方模块整合与死代码清理 (OpenSpec: refactor-prescription-module-consolidation) - 2025-12-10

**循环依赖消除:**
- 确立正确依赖方向: MedicalCase -> Prescriptions (无反向依赖)
- MedicalCase通过IPrescriptionEditorService接口依赖处方功能(依赖倒置原则)
- Prescriptions模块不再引用MedicalCase模块

**死代码删除:**
- 删除FormulaTemplateDialog及其ViewModel (无调用入口)
- 删除SelectFormulaDialog及其ViewModel (无调用入口)
- 删除PrescriptionEditorDialog及其代码隐藏文件 (无调用入口)
- 共删除8个文件，约1605行代码

**模块精简:**
- PrescriptionsModule仅注册2个核心服务:
  - IPrescriptionPrintService (打印服务)
  - IPrescriptionEditorService (编辑器服务)
- 处方UI功能已完全迁移至MedicalCase模块

#### 处方模块冗余代码清理 (OpenSpec: cleanup-prescription-redundancy) - 2025-12-10

**删除冗余文件 (共9个):**
- ViewModels/Components/PrescriptionCalculator.cs (与MedicalCase重复)
- ViewModels/Components/PrescriptionValidator.cs (与MedicalCase重复)
- ViewModels/Components/PrescriptionEventCoordinator.cs (无外部引用)
- ViewModels/PrescriptionItemViewModel.cs (与MedicalCase重复)
- ViewModels/PrescriptionItemRow.cs (无外部引用)
- Components/BasicValidator.cs (无外部引用)
- Components/PriceCalculator.cs (无外部引用)
- Constants/PrescriptionConstants.cs (无外部引用)
- Models/PrescriptionItem.cs (无外部引用)

**保留文件:**
- Models/PrescriptionPrintDto.cs (Print服务使用)
- Services/PrescriptionEditorService.cs (核心服务)
- Services/PrescriptionPrintService.cs (核心服务)

**代码减少:** 约2200行

#### 医案聚合根CRUD重构 (OpenSpec: refactor-medicalcase-aggregate-crud) - 2025-12-10

**统一保存端点:**
- 新增`PUT /api/medicalcase/{id}/aggregate`聚合根保存API
- 创建`MedicalCaseAggregateInputDto`统一Consultation+Prescription数据
- 事务保证诊断和处方原子性写入

**ISaveable到IDataProvider迁移:**
- 移除ISaveable接口依赖，使用IDataProvider模式
- ConsultationPanelViewModel实现IDataProvider<ConsultationInputDto>
- PrescriptionPanelViewModel实现IDataProvider<List<PrescriptionAggregateDto>>

**工作区协调器优化:**
- MedicalCaseWorkspaceCoordinator统一收集子面板数据
- 移除独立的Consultation/Prescription保存API调用
- 保存API调用从2-3次减少到1次

**ConsultationModule禁用:**
- 从Shell层移除ConsultationModule注册（功能已迁移至MedicalCase模块）
- 保留项目目录供参考

#### 患者选择组件重构 (OpenSpec: refactor-patient-selection) - 2025-12-08

**搜索性能优化:**
- 新增`PatientSearchCache`服务，LRU缓存策略(最大100条，5分钟过期)
- 搜索输入防抖优化(300ms延迟)
- 缓存命中时跳过API调用，提升响应速度

**用户体验改进:**
- 支持Enter键触发搜索(DataGrid KeyBinding)
- 搜索状态指示器(IsBusy绑定显示加载状态)
- 统一PatientSelectionView UI风格(与ManagementView一致)

**架构精简:**
- 提取`PatientSearchManager`服务封装搜索和分页逻辑
- 删除废弃`PatientSelectorControl`组件(约350行)
- `PatientSelectionViewModel`职责更清晰

**延迟任务:**
- Task 1.4: 轻量级DTO优化(需后端配合)
- Task 2.3: 关键字高亮(需UI框架评估)

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

#### Desktop层空目录清理与接口整理 (OpenSpec: cleanup-desktop-empty-directories) - 2025-12-11

**空目录删除:**
- 删除 `LYBT.Desktop.Admin` 空模块目录
- 删除 `LYBT.Desktop.Services` 空Core目录
- 删除 `LYBT.Desktop.Infrastructure/Enums` 空目录

**接口文件整理:**
- Prescriptions模块: `IPrescriptionPrintService.cs` 移至 `Interfaces/`
- Auth模块: `IConnectionSettingsService.cs` 移至 `Interfaces/`
- Patients模块: `IPatientSearchCache.cs` 移至 `Interfaces/`

**解决方案清理:**
- 移除LYBT.Desktop.sln中不存在的项目引用(AdminWorkstation, ClinicalWorkstation)

#### 模块目录结构标准化 (OpenSpec: standardize-module-structure) - 2025-12-11

**Components文件夹重命名:**
- 将所有Desktop模块中的`Components/`文件夹重命名为`Services/`
- 统一命名符合.NET命名约定和职责描述
- 涉及模块: Auth, MedicalCase, Patients, Users

**命名空间同步更新:**
- 更新所有相关文件的命名空间从`.Components`到`.Services`
- 更新引用这些组件的文件的using语句
- 保持向后兼容的模块内部结构

**测试文件适配:**
- 更新单元测试文件中的using语句以匹配新命名空间
- 修复因重命名导致的测试编译问题

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

#### 验方复制为我的验方功能 (OpenSpec: implement-formula-copy-flow) - 2025-12-08

**新增功能:**
- 验方详情页添加"复制为我的验方"按钮
- 用户可复制他人共享验方或自己的验方，保存为新副本
- 复制后自动进入编辑模式，可调整后保存

**技术实现:**
- Server: `FormulaService.CreateAsync` 添加 `creatorId` 参数设置所有权
- Server: `FormulasController` 获取当前用户ID传递给服务
- Client: `FormulaDetailViewModel` 实现 `CopyAsMyFormulaCommand`

**修复:**
- 复制验方保存后无法在列表显示（UserId未设置导致过滤排除）

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

**最后更新**: 2025-12-18
