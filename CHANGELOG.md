# Changelog

All notable changes to LYBTZYZS project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

#### 登录认证架构重构 (OpenSpec: refactor-login-authentication) - 2025-12-21

**背景**: 登录认证模块存在安全隐患（明文密码存储）、状态管理混乱和事件通信不一致问题。

**Phase 1 - 核心安全修复**:
- TokenManager: 实现ITokenManager接口，Token严格内存存储，支持有效性和即将过期检查
- CredentialVault: 实现ICredentialVault接口，使用DPAPI+HMAC加密，支持旧格式凭据迁移
- AutoLoginToken: 自动登录使用AutoLoginToken而非明文密码，修改LoginViewModel使用AutoLogin
- Token刷新失败处理: 实现分级处理策略（网络错误重试、Token过期导航登录）

**Phase 2 - 状态管理重构**:
- LoginStateMachine: 实现ILoginStateMachine接口，使用状态转换表管理8种登录状态
- LoginCoordinator重构: 简化职责，集成LoginStateMachine
- 可靠Logout: 实现ILogoutService接口，本地登出立即生效，服务端登出可重试

**Phase 3 - 事件体系**:
- AuthEvents定义: 9个Prism PubSubEvent事件类和9个Payload记录
- 事件发布集成: LoginStateMachine/TokenRefreshHandler/LogoutService发布对应事件
- 向后兼容: 保留原有EventHandler事件，新旧事件系统共存

**测试覆盖**: 18+22+29+20+49 = 138个单元测试通过

#### DDD聚合边界优化 (OpenSpec: refactor-server-ddd-aggregates) - 2025-12-21

**背景**: Server端实体存在双向导航属性，违反DDD跨聚合引用原则，可能导致循环引用。

**重构工作**:
- 删除Consultation.MedicalCase和Prescription.MedicalCase反向导航属性
- EF Core配置改用HasOne<T>()泛型语法（无导航属性参数）
- ConsultationRepository使用子查询替代Include跨聚合加载
- 新增GetMedicalCaseInfoAsync辅助方法用于只读跨聚合查询

**设计原则**:
- 聚合内实体: 只有聚合根到子实体的单向导航
- 跨聚合引用: 仅使用ID，禁止导航属性
- 共享主键: Consultation.Id == MedicalCase.Id

**附带修复**:
- 移除tests/Directory.Build.props重复的coverlet.collector定义(NU1504警告)

#### Desktop层Core净化重构 (OpenSpec: optimize-desktop-core) - 2025-12-21

**背景**: Desktop层Core项目(LYBT.Desktop.Models)包含大量业务代码，违反三层对齐架构原则。

**迁移工作**:
- ConsultationItem -> LYBT.Desktop.Consultation/Models/Items/
- FormulaHerbItem, FormulaItem -> LYBT.Desktop.Formula/Models/Items/
- HerbItem, HerbItemViewModelBase -> LYBT.Desktop.Herbs/Models/Items/, ViewModels/Base/
- MedicalCaseItem -> LYBT.Desktop.MedicalCase/Models/Items/
- PatientItem -> LYBT.Desktop.Patients/Models/Items/
- PrescriptionHerbItem, PrescriptionTemplate -> LYBT.Desktop.Prescriptions/Models/
- UserItem -> LYBT.Desktop.Users/Models/Items/

**Core层净化结果**:
- 仅保留Http/ProblemDetails.cs (RFC 7807标准)
- 仅保留ViewModels/Base/核心ViewModel基类

**测试项目优化**:
- 统一coverlet.collector到tests/Directory.Build.props
- 清理12个测试项目的重复PackageReference

### Added

#### 测试覆盖优化项目 (OpenSpec: optimize-integration-tests) - 2025-12-21

**背景**: 测试存在覆盖缺口、遗留代码和过度设计问题。

**优化工作**:
- 新增FormulasControllerIntegrationTests: 覆盖15个API端点, 30+测试方法
- 新增HerbsControllerIntegrationTests: 覆盖18个API端点, 30+测试方法
- 新增HealthCheckIntegrationTests: 覆盖3个健康检查端点, 12个测试方法
- 清理~2800行冗余/过度设计代码

**清理工作**:
- 删除5个模块中重复的BaseServiceTest (~645行)
- 删除5个模块中重复的InMemoryConfiguration (~1275行)
- 删除未使用测试基类和辅助类 (~615行)
- 删除3个.bak备份文件

#### 统一前后端实体类型与命名 (OpenSpec: unify-frontend-backend-types) - 2025-12-21

**背景**: Desktop UI Model与Shared DTO之间存在类型不一致(枚举转字符串、bool替代枚举)和命名不一致问题。

**类型统一**:
- PatientItem.Gender: string -> Gender enum
- MedicalCaseItem.PatientGender: string -> Gender enum
- HerbItem.IsActive: bool -> Status: CommonStatus enum
- FormulaItem.IsActive: bool -> Status: CommonStatus enum
- FormulaItem.CreatedBy: string? -> Guid?

**命名统一**:
- UserItem: CreateTime->CreatedAt, UpdateTime->UpdatedAt
- PatientItem: IdCard->IdNumber, LastVisitDate->LastVisitTime
- HerbItem: Pinyin->PinYinCode, DosageUnit->Unit, UnitPrice->Price
- FormulaItem: Indication->Indications, Contraindication->Contraindications
- MedicalCaseItem: Status->CaseStatus
- FormulaHerbItem: Sequence->SortOrder

**结构优化**:
- 创建LYBT.Desktop.Models项目集中管理所有Item类
- Item定义从各模块迁移到统一位置
- 合并PrescriptionItemViewModel为PrescriptionHerbItem

**影响模块**: 全部Desktop模块

#### 消除Service层catch-return反模式 (OpenSpec: eliminate-service-catch-return) - 2025-12-21

**背景**: Service层存在97个手动catch-return反模式,违反SVC-003规范,导致双重异常处理和代码冗余。

**重构工作**:
- 移除Auth/Users/Patients/Herbs/Formula/MedicalCase共97个try-catch-rethrow反模式
- 统一由IExceptionHandler处理异常,简化Controller层代码
- 保留合理的catch场景: fire-and-forget(审计日志)、重试逻辑、批处理item-level隔离

**测试更新**:
- 更新11个单元测试从`_ShouldReturnFailure`改为`_ShouldThrowException`
- 测试全部通过: Auth(81) + Users(31) + Patients(54) + Herbs(33) + MedicalCase(41) = 240通过

**安全改进**: 修复TokenRevocationService中`IsTokenRevokedAsync`查询异常返回false(未撤销)的安全隐患

**影响模块**: Auth, Users, Patients, Herbs, Formula, MedicalCase

#### 采用Activity API统一分布式追踪 (OpenSpec: adopt-activity-api-tracing) - 2025-12-21

**背景**: 自定义CorrelationId实现分散、非标准格式、不兼容OpenTelemetry。

**已实现内容**:
- TraceContext.cs: 提供统一的Activity API访问（CurrentTraceId, TraceIdOrNew, StartActivity）
- ActivityCorrelationIdProvider: 基于Activity的ICorrelationIdProvider实现
- ViewModelBase/ClientErrorMessageMapper: 已迁移使用TraceContext.TraceIdOrNew
- 旧代码删除: CorrelationIdContext.cs, CorrelationIdDelegatingHandler.cs

**技术优势**: .NET原生AsyncLocal传播、W3C traceparent自动添加、OpenTelemetry兼容

**影响模块**: LYBT.Shared.Logging, LYBT.Desktop.Models, LYBT.Desktop.Infrastructure

#### 重构异常处理系统 (OpenSpec: refactor-exception-handling-system) - 2025-12-21

**背景**: 异常处理体系存在异常吞没、消息泄露、无HTTP韧性等问题,需建立端到端一致的处理机制。

**Phase 1: Service层异常规范化**:
- 移除101个catch-return反模式(本提案4个 + eliminate-service-catch-return 97个)
- Auth模块保留Result<T,AuthErrorCode>模式(架构需求,9个catch块)
- 创建docs/development/ExceptionThrowingGuidelines.md规范文档

**Phase 2: ViewModel层异常处理基类**:
- ViewModelBase新增SafeExecuteAsync<T>方法
- 新增HandleApiExceptionAsync/HandleUnauthorizedAsync/HandleConflictAsync
- 28个ViewModel已使用GetSafeOperationFailureMessage

**Phase 3: HTTP韧性层(Polly集成)**:
- 添加RetryPolicyExtensions策略工厂
- 配置重试策略(3次,指数退避)、熔断器(5次失败→30秒熔断)、超时(30秒)
- 新增11个集成测试(RetryPolicyIntegrationTests.cs)

**Phase 4: 异常消息安全化**:
- 扩展ClientErrorMessageMapper(70+ ErrorCode映射)
- 新增SensitiveInfoFilter过滤敏感信息
- 100%用户可见消息替换为安全消息
- CorrelationId追踪码机制(8位短追踪码)

**统计**: 改造进度100%(101/101),259单元测试+11集成测试通过

**影响模块**: Desktop.Foundation, Desktop.Infrastructure, 全部Server模块

#### WPF转换器统一架构 (OpenSpec: consolidate-wpf-converters) - 2025-12-21

**背景**: Desktop层的WPF转换器分散在多个项目中,存在大量重复定义,需要统一管理。

**新增Infrastructure/Converters/Converters.xaml**:
- 创建统一转换器资源字典,集中管理所有IValueConverter
- 通过App.xaml -> UnifiedComponents.xaml -> Converters.xaml链提供全局访问

**转换器清单(15个)**:
- 布尔转换: BooleanToVisibilityConverter, InverseBooleanToVisibilityConverter, InverseBooleanConverter
- 空值转换: NullToVisibilityConverter, InverseNullToVisibilityConverter
- 枚举转换: EnumDescriptionConverter
- API状态: ApiHealthStatusToColorConverter, ApiHealthStatusToTextConverter
- 字符处理: FirstCharConverter
- 数值转换: BoolToDoubleConverter
- 专用转换: DecocteMethodToVisibilityConverter, ImagePathConverter, MedicalCaseStatusToColorConverter, MedicalCaseStatusToTextConverter, PrescriptionStatusToColorConverter

**清理工作**:
- 删除Shell层重复转换器(3个): ApiHealthStatusToColorConverter, ApiHealthStatusToTextConverter, FirstCharConverter
- 删除MedicalCase模块重复转换器(1个): InvertedBoolConverter
- 清理45+个XAML文件的本地转换器定义

**影响模块**: Shell, Users, Patients, MedicalCase, Herbs, Formula, Infrastructure

#### Desktop工具类统一项目 (OpenSpec: consolidate-shared-utilities) - 2025-12-21

**背景**: Desktop层的工具类分散在多个项目中，需要统一管理并解决ValidationConstants循环依赖问题。

**新增LYBT.Desktop.Utilities项目**:

工具类迁移(7个):
- `ConfigurationExtensions` - IConfiguration扩展方法
- `SystemConstants` - 系统常量定义
- `ExcelHelper` - NPOI Excel操作封装
- `ProblemDetailsResponse` - RFC 7807错误响应模型
- `RetryPolicyExtensions` - Polly重试/熔断策略
- `ClientErrorMessageMapper` - 客户端错误消息映射
- `DesktopSerilogConfiguration` - Desktop日志配置
- `SensitiveInfoFilter` - 敏感信息过滤器

**ValidationConstants移至Primitives**:
- 解决Models→Validators循环依赖
- 新增常量: CodeMaxLength, UsageMaxLength, DiagnosisMaxLength, FourDiagnosisMaxLength
- 命名空间: `LYBT.Shared.Primitives.Validation`

**清理未使用代码**:
- 删除SimpleMapper.cs (无引用)
- 删除重复的ValidationConstants文件

#### 统一日志系统项目 (OpenSpec: unify-logging-system) - 2025-12-20

**背景**: Server和Desktop层各自实现了日志组件，存在代码重复和HttpContext耦合问题。

**新增LYBT.Shared.Logging项目**:

核心抽象:
- `ICorrelationIdProvider` - CorrelationId提供者接口，解耦HttpContext依赖
- `AsyncLocalCorrelationIdProvider` - 基于AsyncLocal的Desktop实现

敏感数据脱敏:
- `SensitiveDataMasker` - 敏感数据脱敏工具类
- `SensitiveDataDestructuringPolicy` - Serilog解构策略
- `SensitiveDataAttribute/SensitiveDataType/MaskingMode` - 脱敏标记

日志管理:
- `LoggingLevelManager` - 动态日志级别管理
- `DebugModeInfo` - 调试模式信息

Enrichers:
- `CorrelationIdEnricher` - 统一的CorrelationId日志富集器

扩展方法:
- `LoggerConfigurationExtensions` - Serilog配置扩展
- `ServiceCollectionExtensions` - DI注册扩展

**适配器**:
- `HttpContextCorrelationIdProvider` (Server端) - 桥接HttpContext
- `FoundationCorrelationIdProvider` (Desktop端) - 桥接Foundation层

**向后兼容**: 旧组件标记为[Obsolete]，未直接删除

#### 覆盖率基础设施 (OpenSpec: coverage-driven-cleanup) - 2025-12-20

**背景**: 建立代码覆盖率收集基础设施，为质量提升提供数据支撑。

**配置变更**:
- 配置coverlet.collector到全部36个测试项目
- 优化tests/.runsettings排除规则(排除生成代码/迁移/测试项目)
- 创建docs/testing/coverage-baseline.md基准文档

**覆盖率基准**:
- 行覆盖率: 15.4%
- 分支覆盖率: 12.9%
- 方法覆盖率: 11.5%
- 覆盖行数: 2,992 / 19,321

**死代码检查**: 通过Roslyn分析器(IDE0051/IDE0052)扫描，确认无未使用的私有成员

### Changed

#### Desktop核心层清理 (OpenSpec: optimize-desktop-core) - 2025-12-20

**背景**: 清理Desktop核心层未使用的死代码文件，减少代码库复杂度。

**删除的组件**:
- Foundation层: CacheService, ConfigurationService, PollyExtensions, ServiceExceptionExtensions, ServiceHandlerExtensions, VisibilityHelper, SecureTokenStorage
- Infrastructure层: VirtualizedDataGrid, VirtualizedListView, ZeroToVisibilityConverter, RepositoryContainerRegistryExtensions, 多个未使用Event类
- Services: FeatureToggleService, KeyboardShortcutService, StandardErrorHandler, UserExperienceService

**净效果**: 删除30+文件，减少1253行代码

#### 清理过时日志组件 (OpenSpec: cleanup-obsolete-code) - 2025-12-20

**背景**: 统一日志系统(unify-logging-system)完成后，旧的[Obsolete]组件需要清理。

**删除的Server端组件**:
- `LYBT.Infrastructure/Logging/CorrelationIdEnricher.cs` - 已迁移到Shared.Logging
- `LYBT.Infrastructure/Logging/LoggingLevelManager.cs` - 已迁移到Shared.Logging
- `LYBT.Infrastructure/Logging/SensitiveDataMasker.cs` - 已迁移到Shared.Logging
- `LYBT.Infrastructure/Logging/SensitiveDataDestructuringPolicy.cs` - 已迁移到Shared.Logging

**删除的Desktop端组件**:
- `LYBT.Desktop.Infrastructure/Logging/CorrelationIdEnricher.cs` - 已迁移到Shared.Logging

**删除的异常处理组件**:
- `LYBT.WebAPI/Middleware/GlobalExceptionHandler.cs` - 已由Shared.ExceptionHandling替代

**更新的引用**:
- `BaseApiController.cs` → 使用 `LYBT.Shared.Logging.Masking`
- `SensitiveDataJsonConverterFactory.cs` → 使用命名空间别名解决枚举冲突
- `Program.cs` → 使用 `LYBT.Shared.Logging.Management/Extensions`
- `DiagnosticsController.cs` → 使用 `LYBT.Shared.Logging.Management`
- `DiagnosticsControllerTests.cs` → 使用共享组件

**代码统计**: -859行冗余代码

#### Desktop Core层无用代码清理 (OpenSpec: cleanup-desktop-core-unused) - 2025-12-20

**背景**: Desktop Core层(Foundation/Infrastructure/Models/Presentation)累积了大量从未被使用的代码。

**删除的文件** (12个):

Foundation项目 (5个):
- Commands/CommandFactory.cs - 命令工厂，从未被实例化
- Diagnostics/DiagnosticService.cs - 诊断服务，静态方法从未被调用
- Security/SecurityService.cs - 安全服务，从未被DI注册
- Api/Managers/IUnifiedApiClientManager.cs - 接口无实现
- Repositories/BaseApiRepository.cs - 仓库基类从未被继承

Infrastructure项目 (4个):
- Components/CommandHandlerBase.cs - 命令处理器基类从未被继承
- Components/ComponentValidatorBase.cs - 验证器基类从未被继承
- Services/Navigation/EnhancedNavigationService.cs - 导航服务从未使用
- Logging/CorrelationIdContext.cs - Foundation版本的冗余包装器

Presentation项目 (2个):
- Theming/ThemeService.cs - 主题服务，注册但从未注入
- Navigation/INavigationService.cs - 导航接口，定义但无消费者

**代码统一**:
- CorrelationIdContext引用统一使用Foundation.Logging版本
- 清理DI注册中对已删除服务的引用

**净效果**: 净删除约900行无用代码

#### Desktop Core层架构优化 (OpenSpec: optimize-desktop-core) - 2025-12-20

**背景**: Desktop Core层(5个项目)存在代码重复、职责混乱、组织问题，需要统一架构。

**P0级代码重复消除**:
- 删除UnifiedErrorHandlingService，统一使用IDesktopExceptionHandler
- 删除ITokenManager，Token由ITokenStorageService管理
- 删除MappingService/IMappingService，统一使用SimpleMapper
- 删除冗余ErrorHandling目录(ErrorContext/ErrorHandlingService/IExceptionHandler)

**P1级职责澄清**:
- 移除ISessionManager的Token属性，职责分离更清晰
- 删除未使用的IUserSessionManager接口
- 明确会话管理层级：Foundation(认证API) → Infrastructure(内存状态)

**P2级组织优化**:
- 删除未使用的Controls子目录(Auth/Authentication/ErrorHandling/FormulaTemplates)
- Item模型命名一致性验证通过

**净效果**: 净删除约1,700行冗余代码，创建desktop-core-cleanup规范(8个需求项)

**项目结构整合 (Phase 2)** - 2025-12-20:
- 删除LYBT.Desktop.Presentation项目，内容完全合并到Infrastructure
- 迁移UI组件: HerbCardControl, HerbListEditor, HerbListView → Infrastructure.Controls
- 迁移服务: NotificationService, UserExperienceService → Infrastructure.Services
- 迁移接口: 17个接口从Infrastructure.Interfaces → Contracts.Services
- 创建IHerbItemEditable接口(Shared.Components)解决控件与ViewModel循环依赖
- 删除过时的PatientSelector测试项目
- 项目数从5个减少到4个: Contracts, Foundation, Infrastructure, Models

#### 异常处理架构统一 (OpenSpec: consolidate-exception-handling) - 2025-12-20

**背景**: 异常处理分散在多个项目(Foundation/Exceptions、Models/Errors、Infrastructure/Errors等)，缺乏统一架构。

**核心变更**:
- 创建三层架构: Primitives(零依赖) → Models → ExceptionHandling
- 实现ErrorCode 5位数分区设计(0xxxx-7xxxx按模块)
- 引入EC别名模式解决属性/枚举名称冲突
- 实现RFC 7807 ProblemDetails工厂
- 统一Server端异常处理器到共享库

**新增项目**:
- LYBT.Shared.Primitives: 零依赖基础层(ErrorCode枚举)
- LYBT.Shared.ExceptionHandling: 统一异常处理层
- LYBT.Shared.ExceptionHandling.Tests: 100个单元测试

**删除冗余代码**(12+文件):
- Foundation/Exceptions目录(5个文件)
- Models/Errors目录(2个文件)
- Infrastructure/Errors目录(2个文件)
- WebAPI/ExceptionHandlers目录(2个文件)
- Desktop.Models/Exceptions目录(1个文件)

**净效果**: 净删除约847行代码，统一异常处理架构

#### 代码质量标准建立 (OpenSpec: consolidate-code-quality) - 2025-12-20

**背景**: 基于Visual Studio Code Metrics分析，识别7个高圈复杂度方法(CC>20)及EF迁移目录混乱问题。

**Phase 1 - 高复杂度代码重构**:
- PatientImportExecutor.ImportWorker_RunWorkerCompleted (CC 30→15): 消除反射模式，创建ImportResult记录类型
- MedicalCaseRepository.UpdateAsync (CC 28→12): 提取4个辅助方法处理处方实体状态
- ExcelHelper (CC 25+22→10): SetCellValue用switch表达式，ConvertValue提取5个辅助方法
- MedicalCaseCommandService.SaveAsync (CC 23→10): 提取10+辅助方法分离创建/更新/处方逻辑

**Phase 2 - EF迁移目录整合**:
- 统一迁移目录: Data/Migrations/ → Migrations/
- 移动5个迁移文件并更新命名空间
- 删除冗余的Data/Migrations/目录

**新增规范** (openspec/specs/code-quality/spec.md):
- CQ-001: 圈复杂度标准 (CC < 20)
- CQ-002: EF迁移目录规范 (单一目录)
- CQ-003: 代码度量报告 (定期执行)

**额外修复**: 添加缺失的DecocteMethodToVisibilityConverter转换器

**净效果**: 重构4个核心方法，CC均降至15以下，统一迁移目录

### Added

#### 集成测试覆盖补充 (OpenSpec: optimize-integration-tests) - 2025-12-20

**背景**: WebAPI集成测试存在覆盖缺口，FormulasController(15端点)、HerbsController(18端点)、HealthController(3端点)缺少测试。

**新增测试**:
- FormulasControllerIntegrationTests: 覆盖15个API端点，30+测试方法
- HerbsControllerIntegrationTests: 覆盖18个API端点，30+测试方法
- HealthCheckIntegrationTests: 覆盖3个健康检查端点，12个测试方法
- UsersControllerIntegrationTests: 使用当前框架重写

**基础设施优化**:
- 集成测试数据库从InMemory改为真实SQL Server(LYBTDB)
- 统一使用IntegrationTestBase基类
- 修复编译警告(DatabaseLoggingTests、PendingMedicalCaseTests)

**净效果**: 新增72+测试方法，覆盖36个API端点

### Removed

#### 单元测试过度设计清理 (OpenSpec: optimize-integration-tests) - 2025-12-20

**背景**: 单元测试存在大量重复基类和未使用的测试基础设施。

**删除内容**:
- BaseServiceTest.cs: 5个模块中重复的基类(~645行)
- InMemoryConfiguration.cs: 5个模块中重复的配置(~1275行)
- 未使用测试基类: BaseControllerTest、BaseRepositoryTest、BaseSqliteRepositoryTest(~260行)
- 未使用辅助类: TestHelper.cs、TestDataFactory.cs(~355行)
- _archived目录: 遗留的旧测试存档(~300行)
- CrossModuleQueryServiceTests: 6个测试未实现方法的无效测试(~120行)
- .bak备份文件: 3个过时的备份文件

**净效果**: 清理约2800行冗余/过度设计代码

### Removed

#### Desktop未使用代码清理 (OpenSpec: cleanup-desktop-unused-code) - 2025-12-19

**背景**: Desktop层经过多次迭代开发，积累了未使用的代码，增加维护成本。

**删除内容**:
- Shell Dialogs: ErrorDetailsDialog, InformationDialog (未注册未使用)
- MedicalCase Services: MedicalCaseStatusPresenter, MedicalCaseEventCoordinator (未注册)
- Users组件: IUserDataManager, UserDataManager, UserValidator (未注册无引用)
- 对应测试文件: UserDataManagerTests, UserValidatorTests

**评估保留**:
- UnfinishedCaseDialog: WPF Window模式功能正常，避免Pre-Release重构风险
- IDataProvider: 被PrescriptionPanelViewModel和ConsultationPanelViewModel广泛使用

**净效果**: 删除13个文件，减少1575行代码

**Spec更新**: desktop-structure-cleanup (ADDED: Unused Code Cleanup Policy)

### Fixed

#### 药材单位自动加载修复 (OpenSpec: fix-herb-unit-auto-load) - 2025-12-19

**问题**: 创建经验方/处方时，空白药材行的单位硬编码为"g"，与药材库定义的单位（如"克"、"条"、"枚"）不一致。

**修复内容**:
- DTO默认值: FormulaHerbItemInputDto/FormulaHerbImportItemDto Unit改为string.Empty
- Desktop Formula: 3个ViewModel的空行创建逻辑修复
- Desktop MedicalCase: HerbSelectionManager 8处空行创建修复
- Desktop Prescriptions: PrescriptionPrintModel打印模板同步修复
- Server Formula: FormulaService导入时不再fallback到"g"

**行为变化**:
- 空白药材行: 单位字段为空（不再显示"g"）
- 选择药材后: 单位自动从药材库加载
- 打印时: 显示实际存储的单位值

**Spec更新**: herb-card-control (ADDED: Herb Unit Auto-Load requirement)

### Changed

#### Desktop层代码复用优化 (OpenSpec: optimize-desktop-code-reuse) - 2025-12-20

**背景**: Desktop层存在组件分类混乱、代码重复、基类体系不完善等问题。

**优化内容**:

**Phase 1 - 组件存放位置统一**:
- Formula模块: FormulaCommandHandler/DataManager/Validator从ViewModels/Components/迁移至Services/
- 建立分类标准: Services/放业务逻辑组件，ViewModels/Components/仅放UI辅助组件

**Phase 2 - 基类体系完善**:
- 新增CommandHandlerBase: 统一命令注册/执行/异常处理框架
- 新增ComponentValidatorBase: 统一验证模板方法与错误处理
- 位置: Infrastructure/Components/

**Phase 3 - 模块边界文档化**:
- Prescriptions模块: 明确定位为"服务库模式"(Service Library)
- Consultation模块: 明确定位为"组件模式"(MedicalCase Step 2)

**净效果**: 新增2个基类，规范化组件目录结构

**Spec更新**: desktop-code-patterns (NEW)

#### 统一前后端实体类型 (OpenSpec: unify-frontend-backend-types) - 2025-12-19

**状态**: ✅ 全部完成 (Phase 0-8)

**问题背景**:
- Desktop UI Model与Shared DTO之间存在类型不一致
- Gender属性使用string而非枚举，导致FromDto/ToDto需要转换
- Status属性使用bool IsActive而非CommonStatus枚举
- FormulaItem.CreatedBy使用string?而非Guid?
- FormulaHerbItem.Sequence命名与DTO的SortOrder不一致
- UI Model属性命名与DTO不一致（如IdCard vs IdNumber）
- Item类分散在各模块Models目录中，缺乏统一管理

**实施内容**:

**Phase 0: DTO层PatientGender类型统一**
- MedicalCaseDetailDto.PatientGender: string? → Gender enum
- MedicalCaseListDto.PatientGender: string? → Gender enum

**Phase 1: PatientItem Gender类型统一**
- Gender属性从string改为Gender枚举
- 添加GenderDisplay计算属性用于UI显示
- 更新PatientViewControl/PatientMasterDetailView/PatientSelectionView XAML绑定

**Phase 2: MedicalCaseItem PatientGender类型统一**
- PatientGender属性从string改为Gender枚举
- 添加PatientGenderDisplay计算属性

**Phase 3: HerbItem Status类型统一**
- IsActive: bool → Status: CommonStatus
- 添加IsActive计算属性（向后兼容）
- StatusText/StatusColor改为switch表达式

**Phase 4: FormulaItem Status/CreatedBy类型统一**
- IsActive: bool → Status: CommonStatus
- CreatedBy: string? → Guid?
- 添加IsActive计算属性（向后兼容）

**Phase 6: UI Model命名统一**
- UserItem: CreateTime→CreatedAt, UpdateTime→UpdatedAt
- PatientItem: IdCard→IdNumber, LastVisitDate→LastVisitTime
- HerbItem: Pinyin→PinYinCode, DosageUnit→Unit, UnitPrice→Price, Specification→Spec
- FormulaItem: Indication→Indications, Contraindication→Contraindications, Note→Remark
- MedicalCaseItem: Status→CaseStatus
- 所有Item的FromDto/ToDto/UpdateFromDto方法已更新，直接映射无命名转换

**Phase 7: FormulaHerbItem命名统一**
- Sequence → SortOrder（与DTO一致）

**Phase 8: 前端Item定义集中化**
- 在LYBT.Desktop.Models创建Items/集中目录
- 迁移7个Item类到统一位置:
  - FormulaItem → Items/Formulas/ (LYBT.Desktop.Models.Items.Formulas)
  - FormulaHerbItem → Items/Formulas/ (从FormulaItem.cs拆分为独立文件)
  - PatientItem → Items/Patients/ (LYBT.Desktop.Models.Items.Patients)
  - HerbItem → Items/Herbs/ (LYBT.Desktop.Models.Items.Herbs)
  - UserItem → Items/Users/ (LYBT.Desktop.Models.Items.Users)
  - MedicalCaseItem → Items/MedicalCases/ (LYBT.Desktop.Models.Items.MedicalCases)
  - ConsultationItem → Items/Consultations/ (LYBT.Desktop.Models.Items.Consultations)
- 更新所有引用和命名空间
- 删除6个旧Item文件

**Phase 8.4: 处方Item标准化**
- 合并PrescriptionItemViewModel和PrescriptionHerbItemViewModel为统一的PrescriptionHerbItem
- 迁移到LYBT.Desktop.Models/Items/Prescriptions/PrescriptionHerbItem.cs
- 命名空间: LYBT.Desktop.Models.Items.Prescriptions
- 添加向后兼容方法: SetLoadedUnitPrice(), ItemAmount属性(ItemTotal别名)
- 更新所有引用文件（7个Components + 2个ViewModels）
- 删除旧ViewModel文件
- 更新MedicalCaseModule.cs移除废弃的DI注册

**Phase 9: 最终验证**
- 编译验证: 0错误, 0警告
- 单元测试: 228/228 MedicalCase测试全部通过
- 测试文件更新: 修复PrescriptionHerbItem类名引用

**验证结果**:
- 编译通过: 0错误, 0警告
- 类型转换代码已消除，FromDto/ToDto直接赋值
- 属性命名与DTO完全一致，消除命名转换代码
- Item类集中管理，便于维护和查找
- 处方Item类型统一，消除PrescriptionItemViewModel/PrescriptionHerbItemViewModel二义性

---

#### Post-Release Cleanup: DTO架构统一与API优化 - 2025-12-19 [已归档]

**状态**: ✅ 全部完成

**问题背景**:
- 多个OpenSpec提案的DEFERRED项目需要统一清理
- DTO命名不一致（混用XxxDto和XxxDetailDto）
- API端点存在冗余（MedicalCase有重复的GET /和GET /list）
- 测试需要同步更新

**实施内容**:

**Phase 1-2: 过期组件和DTO清理**
- 删除已标记[Obsolete]的Management组件
- 统一DTO命名：XxxDto → XxxDetailDto（Patient/User/Formula/Herb/MedicalCase/Consultation）
- 删除冗余的DtoExtensions扩展方法
- 拆分大型DTO文件为独立文件

**Phase 3: 服务层迁移到ListDto**
- Patient模块: IPatientService.GetPagedAsync返回PatientListDto
- User模块: IUserService.GetPagedAsync返回UserListDto
- Formula模块: IFormulaService.GetPagedAsync返回FormulaListDto
- Herb模块: IHerbService.GetPagedAsync返回HerbListDto
- 所有Controller端点同步更新

**Phase 4: MedicalCase API端点优化**
- 合并重复端点: GET /和GET /list → 统一返回MedicalCaseListDto
- 删除Client端GetMedicalCasesListAsync方法
- Repository层保持向后兼容（使用SearchMedicalCasesAsync维持DetailDto契约）

**Phase 5: 测试验证**
- 更新PatientsControllerTests验证GetPagedAsync调用
- 全量测试通过（0错误0警告）

**验证结果**:
- 编译通过: 0错误, 0警告
- 关键模块测试全部通过（单独运行）

---

#### 简化MedicalCase API端点 (OpenSpec: simplify-medicalcase-api) - 2025-12-19 [已归档]

**状态**: ✅ 核心变更完成，已归档 (DEFERRED项目待Post-Release)

**问题背景**:
- MedicalCase API端点过多(28+)，命名不一致
- PUT /aggregate路由不符合RESTful规范
- 存在Ghost APIs (Client定义但Server未实现)
- 保存功能HTTP 400错误

**实施内容**:

**核心变更**:
1. 路由简化: PUT `/api/v1/medicalcases/{id}/aggregate` → PUT `/api/v1/medicalcases/{id}`
2. 方法重命名: `SaveAggregate` → `Save`
3. Ghost APIs删除: `ClearPrescriptionAsync`, `ImportFormulaIntoPrescriptionAsync`
4. Bug修复: MedicalCaseInputDto添加PatientId和UserId，修复HTTP 400

**验证结果**:
- 编译通过 (0错误0警告)
- 测试通过: Server(41) + Client(228) = 269 passed
- 功能验证: 医案列表/详情加载、保存(HTTP 200)

**DEFERRED (Post-Release)**:
- 查询端点合并 (include/filter参数)
- 状态端点统一 (PATCH /{id}/status)
- 独立Prescription/Consultation端点删除

---

#### 优化实体数据流 (OpenSpec: optimize-entity-data-flow) - 2025-12-19 [已归档]

**状态**: ✅ Phase 1-3完成，已归档 (Phase 4-5 DEFERRED to Post-Release)

**问题背景**:
- MasterDetail布局需要验证所有模块功能完整性
- Management组件需要标记为过期以便后续清理
- DTO需要迁移到ListDto/DetailDto分层模式消除N+1查询

**实施内容**:

**Phase 1: 验证MasterDetail完整性**
- Formula/Herb/Patient/User/MedicalCase模块MasterDetail功能验证通过

**Phase 2: 标记Management为[Obsolete]**
- 所有模块ManagementViewModel/View添加[Obsolete]标记

**Phase 3: 迁移MasterDetail中的过期DTO**
- User模块: UserMasterDetailViewModel迁移到UserListDto
- Formula模块: FormulaMasterDetailViewModel迁移到FormulaListDto
- Patient模块: PatientMasterDetailViewModel迁移到PatientListDto
- Herb模块: HerbMasterDetailViewModel迁移到HerbListDto
- MedicalCase模块: MedicalCaseMasterDetailViewModel迁移到MedicalCaseListDto
- 所有模块Server端添加/list API端点

**DEFERRED (Post-Release)**:
- Phase 4: 服务层DTO迁移
- Phase 5: 过期代码移除

---

#### 重构医案管理模块 (OpenSpec: refactor-medicalcase-management) - 2025-12-19 [已归档]

**状态**: ✅ Phase 1-4完成，已归档 (旧代码清理DEFERRED to Post-Release)

**问题背景**:
- 医案管理模块使用旧的Management布局，与其他模块MasterDetail布局不一致
- 需要更新诊断字段以匹配refactor-diagnosis-fields变更
- 新建医案应仅通过看诊入口创建，管理模块不提供新建功能

**实施内容**:

**Phase 1: 医案管理Master-Detail布局**
- 创建MedicalCaseMasterDetailView/ViewModel
- 左侧Master: 工具栏(仅刷新)+搜索+DataGrid+分页
- 右侧Detail: 医案详情表单+诊断/处方摘要
- 工具栏不包含AddCommand（无新建功能）

**Phase 2: 看诊工作区诊断字段更新**
- ConsultationPanelView保留4个核心字段(PresentIllness, TongueDiagnosis, PulseDiagnosis, TCMDiagnosis)

**Phase 3: 分离共用组件**
- 管理视图使用只读显示，看诊工作区使用编辑控件
- 两者不共享可编辑控件

**Phase 4: 清理与验证**
- 旧代码已标记[Obsolete]，Post-Release删除
- 228测试全部通过

**变更文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseMasterDetailView.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/MedicalCaseDetailModel.cs`

---

#### 统一药材列表编辑控件 (OpenSpec: unify-herb-list-controls) - 2025-12-19 [已归档]

**状态**: ✅ Phase 1-3完成，已归档

**问题背景**:
- 药材列表编辑控件分散在多个模块，实现不一致
- HerbItemViewModelBase._unit默认值问题
- 医案管理模块缺少处方编辑功能

**实施内容**:

**Phase 1: Bug修复与新控件**
- 修复HerbItemViewModelBase._unit默认值为空字符串
- 创建HerbListView.xaml只读预览控件

**Phase 2: 控件统一**
- EditFormulaDialog.xaml使用HerbListEditor
- MedicalCaseWorkspaceView.xaml药材预览使用HerbListView

**Phase 2.5: 医案管理模块处方编辑**
- MedicalCaseMasterDetailViewModel添加处方编辑功能
- 使用SaveAsync一次性保存诊断和处方

**Phase 3: 验证**
- 编译验证通过
- 处方/经验方编辑功能验证通过

**变更文件**:
- `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/HerbItemViewModelBase.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Presentation/Components/HerbListView.xaml(.cs)`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/EditFormulaDialog.xaml`

---

#### HerbCardControl UI优化与煎法字段添加 (OpenSpec: modify-herbcard-decoction) - 2025-12-19 [已归档]

**状态**: ✅ Phase 1-5完成，已归档

**问题背景**:
- HerbCardControl UI上显示的"单位"字段对用户无实际意义（单位从药材库自动获取）
- 缺少煎法标注功能，中医处方常需标注先煎、后下等特殊煎法
- Bug: 输入完整正确药材名称后按回车，焦点不移动
- Bug: 输入不存在的药材名称后，系统接受了无效输入

**实施内容**:

**Phase 1: 数据模型变更**
- 创建DecocteMethod枚举(Default/PreDecoct/PostAdd/MeltIn/TakeWithWater/WrapDecoct/SeparateDecoct)
- PrescriptionItem实体添加DecocteMethod字段
- EF Core迁移添加数据库列

**Phase 2: ViewModel层变更**
- HerbItemViewModelBase添加DecocteMethod属性
- PrescriptionItemViewModel添加DecocteMethod属性和AvailableDecocteMethods列表

**Phase 3: UI层变更与Bug修复**
- HerbCardControl移除单位显示，添加煎法ComboBox
- UI优化：删除按钮改为右键菜单（节省空间+防误删）
- Bug修复：完整药材名称回车焦点跳转
- Bug修复：无效药材名称校验

**Phase 4: 打印功能适配**
- PrescriptionPrintDto添加DecocteMethod属性
- 打印模板格式："药材名剂量单位(煎法)" - 仅非默认煎法显示括号标注

**Phase 5: 验证与测试**
- 编译验证通过
- 功能验证：经验方和处方煎法修改功能正常

**变更文件**:
- `src/Shared/LYBT.Shared.Models/Enums/DecocteMethod.cs`
- `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionItem.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Presentation/Components/HerbCardControl.xaml(.cs)`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionItemViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`

---

#### 简化MedicalCase数据流 (OpenSpec: simplify-medicalcase-dataflow) - 2025-12-19 [已归档]

**状态**: ✅ Phase 0-5完成，已归档

**问题背景**:
- MedicalCase实体存在冗余字段(ConsultationDate与CreatedAt重复)
- DoctorId命名不统一(应为UserId)
- Prescription实体的Indication/FormulaSource字段与Consultation重复
- MedicalCaseAggregateInputDto和PrescriptionAggregateInputDto增加维护复杂度
- 权限判断逻辑分散在Entity和Service中

**实施内容**:

**Phase 0: 实体字段优化 + 权限逻辑统一**
- MedicalCase: 删除ConsultationDate(用CreatedAt)，DoctorId→UserId，新增CaseNumber/CompletedAt
- MedicalCase: 删除CanEdit()方法，新增IsActive/IsCompleted计算属性，更新IsLocked逻辑
- Prescription: 删除Indication/FormulaSource，新增Usage字段
- 权限逻辑统一到MedicalCasePermissionService

**Phase 1: DTO重构**
- 扩展MedicalCaseInputDto添加Consultation/Prescription/EditReason字段
- 删除MedicalCaseAggregateInputDto和PrescriptionAggregateInputDto
- 删除对应的Validator

**Phase 2: Server端业务逻辑重构**
- 统一SaveAggregateAsync → SaveAsync
- POST端点支持创建时包含Consultation/Prescription

**Phase 3: Client端适配**
- IMedicalCaseApi/Repository/ViewModels方法重命名
- 去除Aggregate后缀

**Phase 4: 测试验证**
- Server模块: 324测试全部通过 (Auth 81, Herbs 33, MedicalCase 41, Patients 54, Users 31, Prescriptions 34, WebAPI 50)
- Desktop模块: 449测试全部通过 (MedicalCase 228, Consultation 8, Foundation 57, Shell 156)

**Phase 5: 清理与文档**
- 编译验证: 0错误0警告
- 更新CHANGELOG，归档提案

**变更文件**:
- `src/Server/Domain/LYBT.Entities/MedicalCases/MedicalCase.cs`
- `src/Server/Domain/LYBT.Entities/Prescriptions/Prescription.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/*.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/*.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/*.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/**/*.cs`

---

#### 统一MedicalCase InputDTO (OpenSpec: unify-medicalcase-input-dto) - 2025-12-19 [已归档]

**状态**: ✅ Phase 1-6完成，已归档 (commit: 0215518ab)

**问题背景**:
- MedicalCaseInputDto包含14个诊断字段，但Server端CreateMedicalCaseRequest仅使用PatientId+VisitDate
- Client-Server API契约不一致，存在冗余字段
- 诊断字段应由ConsultationInputDto管理（DDD聚合设计）

**实施内容**:
- **Phase 1**: 分析DTO使用情况，发现诊断字段全部未被使用
- **Phase 2**: 简化MedicalCaseInputDto为5字段(Id, PatientId, DoctorId, VisitDate, Remark)
- **Phase 3**: 删除CreateMedicalCaseRequest，Controller统一使用MedicalCaseInputDto
- **Phase 4**: Client端代码已正确使用简化后的DTO
- **Phase 5**: 更新映射配置(PatientMappingProfile)，修复重复映射问题
- **Phase 6**: 测试验证通过 - MedicalCase(228) + Patients(54) + Users(31) + Auth(81) = 477+ tests passed

**关键决策**:
- IsMedicalCaseChanged()变更检测仅跟踪: CaseNumber, PatientId, DoctorId, CaseStatus, Remark
- AutoMapper映射需忽略Status/CreatedBy审计字段

**变更文件**:
- `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseInputDto.cs`
- `src/Shared/LYBT.Shared.Validators/MedicalCase/MedicalCaseInputDtoValidator.cs`
- `src/Server/Modules/LYBT.Module.Patients/Mapping/PatientMappingProfile.cs`
- `tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests/` (多个测试文件)

---

#### 整合医案查询到聚合根模式 (OpenSpec: consolidate-medicalcase-queries) - 2025-12-19 [已归档]

**状态**: ✅ Phase 1-6完成 + Phase 7.A死代码清理完成，已归档

**Phase 1-2: 查询能力扩展**
- MedicalCaseQueryService添加SearchMedicalCasesAsync和GetPatientRecentMedicalCasesAsync
- MedicalCaseController添加/search和/patient/{id}/recent端点
- IMedicalCaseApi添加对应Refit方法

**Phase 3: WebApi清理**
- 删除ConsultationController.cs和PrescriptionsController.cs
- 删除IConsultationApi.cs和IPrescriptionApi.cs
- 移除DI容器中的相关注册

**Phase 4-6: Service层与DTO清理**
- PrescriptionService删除跨医案查询方法
- 删除MedicalCaseBasicDto.cs
- ICrossModuleQueryService删除MedicalCase相关方法

**Phase 7.A: Client API死代码清理**
- 删除CreateMedicalCaseWithDetailsAsync、SoftDeleteMedicalCaseAsync方法
- 删除MedicalCaseCreateInputDto.cs文件
- 清理Repository层相关方法

**架构影响**:
- 强化DDD聚合根模式：Consultation和Prescription操作必须通过MedicalCase
- 符合CQRS原则：读操作集中到MedicalCaseQueryService
- 减少API表面积：删除2个独立Controller

**延迟事项**: Phase 7.B DTO统一重构（需单独OpenSpec提案）

---

#### DTO设计简化重构 (OpenSpec: refactor-dto-simplification) - 2025-12-18 [已归档]

**状态**: ✅ 100%完成，已归档

**Phase 1-3: DTO规范建立与模块重构**
- 建立DTO设计规范：每模块最多4个核心类型(ListDto, DetailDto, InputDto, Statistics)
- 重构7个模块DTO：Prescription, Formula, Herb, Patient, MedicalCase, User, Consultation
- Statistics DTO改为record定义，移除继承关系

**Phase 4: 清理遗留代码**
- 扁平化5个DTO：ConsultationDetailDto, FormulaDetailDto, MedicalCaseDetailDto, FormulaHerbItemDto, PrescriptionItemDto
- 删除11个未使用DTO文件（PatientTagDto, HerbExpiryWarningDto等）
- 从DtoBase.cs移除3个未使用基类（CreateDtoBase, UpdateDtoBase, ExtendedQueryDto）
- Desktop层命名消歧：PrescriptionPrintDto → PrescriptionPrintModel

**技术决策**:
- 保留ICreatorTrackable接口用于所有权检查
- 保留BaseDto/TimestampDto/StatusDto基类（仍有模块使用）
- UserInputDto.Status保留（用户账户状态有特殊安全需求）

---

#### 批量操作优化与DTO命名规范化 (OpenSpec: optimize-batch-operations) - 2025-12-18 [已归档]

**状态**: ✅ 100%完成，已归档

**Phase 1: DTO命名规范化**
- 重命名15个批量相关DTO (Request→Input, 添加Item后缀)
- 统一ImportFailureDto命名规范
- BatchImportResultDto继承规范化（消除重复字段）

**Phase 2: 批量操作API优化**
- Server端: 新增batch-delete/enable/disable端点 (Users/Patients/Herbs/Formulas/MedicalCases)
- Service层: 使用EF Core ExecuteUpdate实现数据库级批量操作
- Desktop层: ViewModel批量操作从N+1模式优化为单次API调用
- 测试: BatchOperationsTests.cs集成测试 + BatchOperationsBenchmark.cs性能测试

**性能提升**: 批量模式比N+1模式快约8-9倍

**技术决策**:
- 使用ExecuteUpdate替代逐条更新，解决N+1性能问题
- 批量端点统一使用BatchDeleteInputDto和BatchOperationResultDto

---

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
