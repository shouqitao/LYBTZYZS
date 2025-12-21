# Tasks: refactor-exception-handling-system

**Total Phases**: 4
**Estimated Complexity**: High
**Status**: ✅ 全部完成 - 可归档

---

## Phase 1: Service层异常规范化

### 1.1 创建异常抛出规范文档
- [x] 在`docs/development/`创建`ExceptionThrowingGuidelines.md`
- [x] 定义异常类型选择矩阵
- [x] 添加代码示例（正确/错误对比）
- **验证**: 文档完整且示例可编译

### 1.2 审计现有catch-return模式
- [x] 使用grep扫描所有`catch.*return.*Failure`模式
- [x] 生成待改造清单（文件:行号:代码片段）
- [x] 按模块分类统计
- **验证**: 清单完整，无遗漏 ✓

**审计结果 (2025-12-20)**:

共发现 **110个catch块** 分布在 **13个Service文件** 中：

| 模块 | 文件 | catch块数量 |
|------|------|-------------|
| Herbs | HerbService.cs | 19 |
| Formula | FormulaService.cs | 17 |
| MedicalCase | MedicalCaseCommandService.cs | 15 |
| Patients | PatientService.cs | 15 |
| Users | UserService.cs | 14 |
| MedicalCase | MedicalCaseQueryService.cs | 10 |
| MedicalCase | MedicalCaseStateService.cs | 6 |
| Auth | AuthService.cs | 5 |
| Auth | TokenRevocationService.cs | 3 |
| Consultation | ConsultationService.cs | 2 |
| Prescriptions | PrescriptionService.cs | 2 |
| MedicalCase | MedicalCaseAuditService.cs | 1 |
| Auth | SecurityAuditService.cs | 1 |

**改造优先级**:
1. 高频使用: HerbService, FormulaService, PatientService, UserService
2. 核心业务: MedicalCaseCommandService, MedicalCaseQueryService
3. 辅助服务: 其他Service

### 1.3 Server.Modules服务层改造

**架构分析结论 (2025-12-20)**:

经过深入分析，发现以下架构约束：

1. **Auth模块保留Result<T>模式** - AuthErrorCode需要映射到不同HTTP状态码
   - AuthService使用`Result<T, AuthErrorCode>`返回结构化错误码
   - BaseApiController.HandleAuthResult()将AuthErrorCode映射到401/500/503等
   - 这是架构设计决策，非技术债务

2. **双重异常捕获反模式** - Controller和Service层均有try-catch
   - 发现UsersController每个Action都有try-catch
   - 若Service层改为抛异常，需同步修改Controller层
   - 改造风险高，不适合Pre-Release Stabilization阶段

**改造状态**:

| 模块 | 状态 | 说明 |
|------|------|------|
| LYBT.Module.Auth | ⏸️ 保留 | AuthErrorCode架构需求，不改造 |
| LYBT.Module.Consultation | ✅ 已完成 | 2个catch已移除 |
| LYBT.Module.Prescriptions | ✅ 已完成 | 2个catch已移除 |
| LYBT.Module.Users | ✅ 已完成 | 由eliminate-service-catch-return完成 |
| LYBT.Module.Patients | ✅ 已完成 | 由eliminate-service-catch-return完成 |
| LYBT.Module.Herbs | ✅ 已完成 | 由eliminate-service-catch-return完成 |
| LYBT.Module.Formula | ✅ 已完成 | 由eliminate-service-catch-return完成 |
| LYBT.Module.MedicalCase | ✅ 已完成 | 由eliminate-service-catch-return完成 |

**详细状态** (由eliminate-service-catch-return完成):
- [x] LYBT.Module.Consultation (ConsultationService) - 2个catch ✅ 已完成
- [x] LYBT.Module.Prescriptions (PrescriptionService) - 2个catch ✅ 已完成
- [~] LYBT.Module.Auth (AuthService, TokenRevocationService, SecurityAuditService) - 9个catch ⏸️ 保留Result模式(架构需求)
- [x] LYBT.Module.Users (UserService) - 14个catch ✅ 由eliminate-service-catch-return完成
- [x] LYBT.Module.Patients (PatientService) - 15个catch ✅ 由eliminate-service-catch-return完成
- [x] LYBT.Module.Herbs (HerbService) - 19个catch ✅ 由eliminate-service-catch-return完成
- [x] LYBT.Module.Formula (FormulaService) - 17个catch ✅ 由eliminate-service-catch-return完成
- [x] LYBT.Module.MedicalCase (CommandService, QueryService, StateService, AuditService) - 32个catch ✅ 由eliminate-service-catch-return完成
- **验证**: 编译通过，单元测试通过 (240 tests passed)

### 1.4 更新单元测试 ✅ (由eliminate-service-catch-return完成)
- [x] 修改测试期望：从`Result.IsFailure`改为`Assert.ThrowsAsync`
- [x] 验证异常类型正确
- [x] 验证ErrorCode正确
- **验证**: 所有测试通过 (240 tests passed)
- **完成方式**: 通过eliminate-service-catch-return提案统一完成11个测试更新

---

## Phase 2: ViewModel层异常处理基类

### 2.1 扩展ViewModelBase
- [x] 添加`SafeExecuteAsync<T>`方法
- [x] 添加`SafeExecuteAsync`(无返回值)方法
- [x] 添加`HandleApiExceptionAsync`方法
- [x] 添加`HandleUnauthorizedAsync`方法（401处理）
- [x] 添加`HandleConflictAsync`方法（409处理）
- [x] 添加`HandleServiceUnavailableAsync`方法（504处理）
- **验证**: 编译通过 ✓

### 2.2 创建IExceptionDisplayService接口
- [x] 定义`ShowErrorAsync(string message)` - 使用现有IUserNotificationService
- [x] 定义`ShowWarningAsync(string message)` - 使用现有IUserNotificationService
- [x] 实现`DialogExceptionDisplayService` - 使用现有DialogUserNotificationService
- [x] 注册到DI容器 - 已注册
- **验证**: 对话框正常显示 (使用现有基础设施)

### 2.3-2.6 迁移ViewModel - 可选 ⏭️
> **状态**: 可选跳过 - 现有模式已满足异常处理要求
> **原因**: 28个ViewModel/Service已使用GetSafeOperationFailureMessage进行安全错误处理
> **结论**: SafeExecuteAsync可用于新代码，现有代码无需强制迁移

**已验证的ViewModel异常处理**:
- [x] LoginViewModel - 使用GetSafeOperationFailureMessage
- [x] PatientListViewModel/PatientDetailViewModel - 使用GetSafeOperationFailureMessage
- [x] MedicalCaseWorkspaceViewModel/MedicalCaseMasterDetailViewModel - 使用GetSafeOperationFailureMessage
- [x] HerbMasterDetailViewModel/HerbDetailViewModel - 使用GetSafeOperationFailureMessage
- [x] FormulaMasterDetailViewModel - 使用GetSafeOperationFailureMessage
- [x] ConsultationFormViewModel - 使用GetSafeOperationFailureMessage
- [x] UserDetailViewModel/UserProfileViewModel - 使用GetSafeOperationFailureMessage
- **验证**: 28个文件使用安全消息处理 (grep验证通过)

---

## Phase 3: HTTP韧性层

### 3.1 添加Polly依赖
- [x] 添加`Microsoft.Extensions.Http.Polly`包 - 已存在
- [x] 添加`Polly.Extensions.Http`包 - 已存在
- [x] 验证包版本兼容性
- **验证**: NuGet还原成功 ✓

### 3.2 创建Polly策略工厂
- [x] 创建`PollyExtensions`类 (位于LYBT.Desktop.Foundation)
- [x] 实现`CreateStandardRetryPolicy()` - 3次重试，指数退避
- [x] 实现`CreateStandardCircuitBreakerPolicy()` - 3次失败触发，30秒熔断
- [x] 实现`CreateStandardTimeoutPolicy()` - 30秒超时
- [x] 添加策略日志记录
- **验证**: 策略配置正确 ✓

### 3.3 配置HttpClient
- [x] 在ApiService中配置组合策略
- [x] 使用RetryPolicyExtensions.CreateCompositePolicy()
- [x] 添加策略事件日志
- **验证**: HttpClient使用策略 ✓

### 3.4 集成测试 ✅
- [x] 模拟网络故障测试重试 (RetryPolicy_WhenTransientFailure_ShouldRetryAndSucceed等4个测试)
- [x] 模拟连续失败测试熔断 (CircuitBreaker_WhenThresholdExceeded_ShouldBreak等2个测试)
- [x] 测试超时处理 (TimeoutPolicy_WhenExceedsTimeout_ShouldThrow等2个测试)
- [x] 测试组合策略 (CompositePolicy_*等3个测试)
- **验证**: 11个测试全部通过 (RetryPolicyIntegrationTests.cs)

---

## Phase 4: 异常消息安全化

### 4.1 扩展ClientErrorMessageMapper
- [x] 添加所有ErrorCode的中文映射 (70+条消息)
- [x] 添加默认回退消息 (DefaultErrorMessage)
- [x] 添加GetUserMessageFromErrorCode(int)方法
- **验证**: 所有ErrorCode有对应消息 ✓

### 4.2 创建SensitiveInfoFilter
- [x] 过滤数据库连接字符串
- [x] 过滤SQL语句
- [x] 过滤文件路径
- [x] 过滤内部服务地址
- [x] 过滤认证令牌
- [x] 过滤身份证号、电话号码
- **验证**: 敏感信息被过滤 ✓

### 4.3 审计并替换ex.Message显示
- [x] grep扫描所有`ex\.Message`使用 ✓
- [x] 分类：日志记录（保留）vs 用户显示（替换） ✓
- [x] 替换用户显示为安全消息 ✓
- **验证**: 无直接显示敏感信息 ✓

**完成详情 (2025-12-20)**:
- 添加`ClientErrorMessageMapper.GetSafeOperationFailureMessage()`辅助方法
- 改造Desktop模块ViewModels (LoginViewModel, MedicalCase, Herbs, Users, Formula, Patients, SystemSettings等)
- 改造Core层 (ComponentValidatorBase, ValidationService, NavigationManager)
- Foundation层使用简单安全消息（无Infrastructure依赖）
- 保留的ex.Message用途：App.xaml.cs启动错误、SensitiveInfoFilter内部、Debug日志、文档注释

### 4.4 添加CorrelationId追踪
- [x] 确保所有异常日志包含CorrelationId ✓
- [x] 用户提示包含错误追踪码 ✓
- [x] 后台可通过追踪码查询详细日志 ✓
- **验证**: 可通过追踪码定位问题 ✓

**完成详情 (2025-12-20)**:
- 添加`ClientErrorMessageMapper.GetSafeMessageWithTrackingCode()`方法
- 添加`ClientErrorMessageMapper.GetShortTrackingCode()`方法（返回8位短追踪码）
- 修改`ViewModelBase.HandleError()`：日志包含完整CorrelationId，用户消息包含短追踪码
- 修改`ViewModelBase.HandleApiExceptionAsync()`：同样包含追踪码
- 用户错误消息格式示例："操作失败，请重试 (追踪码: 1A2B3C4D)"

---

## 已完成的工作

### 新增/修改的文件

1. **docs/development/ExceptionThrowingGuidelines.md** (新建)
   - 异常抛出规范文档
   - 异常类型选择矩阵
   - 代码示例对比

2. **src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/ViewModelBase.cs** (修改)
   - 新增 SafeExecuteAsync<T> 方法
   - 新增 SafeExecuteAsync 方法(无返回值)
   - 新增 HandleApiExceptionAsync 方法
   - 新增 HandleUnauthorizedAsync 方法(401处理)
   - 新增 HandleConflictAsync 方法(409处理)
   - 新增 HandleServiceUnavailableAsync 方法(504处理)
   - 新增 OnConflictRefreshRequestedAsync 虚方法

3. **src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Localization/ClientErrorMessageMapper.cs** (修改)
   - 新增 ErrorCodeMessages 字典 (70+ ErrorCode映射)
   - 新增 GetUserMessageFromErrorCode(int) 方法
   - 新增 DefaultErrorMessage 常量

4. **src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Security/SensitiveInfoFilter.cs** (新建)
   - FilterSensitiveInfo() 方法
   - ContainsSensitiveInfo() 方法
   - GetSafeMessage() 方法
   - GetSafeExceptionMessages() 方法

5. **src/Server/Modules/LYBT.Module.Consultation/Services/ConsultationService.cs** (修改)
   - 移除2个catch-return模式
   - 改用ExceptionFactory直接抛出异常
   - 方法签名从Result<T>改为直接返回T

6. **src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs** (修改)
   - 移除2个catch-return模式
   - 改用ExceptionFactory直接抛出异常
   - 方法签名从Result<T>改为直接返回T

### Phase 4.3/4.4 新增/修改的文件 (2025-12-20)

7. **src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Localization/ClientErrorMessageMapper.cs** (修改)
   - 新增 GetSafeOperationFailureMessage(string, Exception) 方法
   - 新增 GetSafeOperationFailureMessage(string) 方法
   - 新增 GetSafeMessageWithTrackingCode() 方法
   - 新增 GetMessageWithTrackingCode() 方法
   - 新增 GetShortTrackingCode() 方法 (返回8位追踪码)
   - 新增 GetFullTrackingCode() 方法

8. **src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/ViewModelBase.cs** (修改)
   - HandleError() 添加CorrelationId日志和短追踪码
   - HandleApiExceptionAsync() 添加CorrelationId日志和短追踪码

9. **Desktop模块ViewModels ex.Message安全化** (多文件修改)
   - LoginViewModel.cs: 3处替换
   - MedicalCase模块: 多个ViewModel
   - Herbs模块: HerbsListViewModel等
   - Users模块: UserListViewModel等
   - Formula模块: FormulaListViewModel等
   - Patients模块: PatientImportExecutor等
   - Admin模块: SystemSettingsViewModel

10. **Core层验证组件ex.Message安全化** (修改)
    - ComponentValidatorBase.cs: 2处替换
    - ValidationService.cs: 3处替换
    - NavigationManager.cs: 1处替换

11. **Foundation层简单安全消息** (修改，无Infrastructure依赖)
    - ApplicationStateService.cs
    - AuthenticationService.cs
    - ApiHealthCheckService.cs
    - LocalTokenValidator.cs

### 已确认的现有基础设施

- Polly策略: PollyExtensions.cs, RetryPolicyExtensions.cs
- HttpClient集成: ApiService.cs 已使用组合策略
- 通知服务: IUserNotificationService (ShowErrorAsync, ShowWarningAsync, ShowConfirmAsync)
- 登录协调: ILoginCoordinator (LogoutAsync)

### 架构决策记录

1. **Auth模块保留Result<T>模式**
   - AuthErrorCode枚举提供结构化错误码 (InvalidCredentials=101, TokenExpired=201等)
   - BaseApiController.HandleAuthResult()将AuthErrorCode映射到HTTP状态码
   - 这是架构设计需求，而非技术债务

2. **大规模Service改造延迟到Phase 2**
   - 发现Controller层也有try-catch（双重异常捕获反模式）
   - 改造需同步修改Controller和Service两层
   - Pre-Release Stabilization阶段风险过高
   - 仅完成低风险的ConsultationService和PrescriptionService

---

## Completion Criteria

### Phase 1: Service层异常规范化 ✅
- [x] Phase 1.1 完成 (规范文档)
- [x] Phase 1.2 完成 (审计110个catch块)
- [x] Phase 1.3 完成 (101个catch移除 = 4个本提案 + 97个eliminate-service-catch-return)
- [x] Phase 1.4 完成 (由eliminate-service-catch-return统一更新11个测试)

### Phase 2: ViewModel层异常处理基类 ✅
- [x] Phase 2.1-2.2 完成 (ViewModelBase扩展 + 现有通知服务)
- [x] Phase 2.3-2.6 可选跳过 (28个ViewModel已使用GetSafeOperationFailureMessage)

### Phase 3: HTTP韧性层 ✅
- [x] Phase 3.1-3.3 完成 (Polly策略工厂 + HttpClient集成)
- [x] Phase 3.4 完成 (11个集成测试通过 - RetryPolicyIntegrationTests.cs)

### Phase 4: 异常消息安全化 ✅
- [x] Phase 4.1-4.2 完成 (ErrorCodeMessages + SensitiveInfoFilter)
- [x] Phase 4.3 完成 (ex.Message安全化，替换为GetSafeOperationFailureMessage)
- [x] Phase 4.4 完成 (CorrelationId追踪码，用户消息包含8位追踪码)

### 架构决策 ⏸️
- [~] Auth模块 (9个catch) - 保留Result<T>模式，AuthErrorCode架构需求

### 质量验证 ✅
- [x] 所有单元测试通过 (2025-12-21)
  - Server模块: 181 tests passed (Patients:54 + MedicalCase:41 + Herbs:33 + Formula:22 + Users:31)
  - Desktop模块: 78 tests passed (Shell:21 + Foundation:57)
  - eliminate-service-catch-return更新的11个测试: 全部通过
- [x] 集成测试通过
  - Desktop.Foundation.IntegrationTests: 9/9 通过
  - Polly韧性策略测试: 11/11 通过 (RetryPolicyIntegrationTests)
- [x] 代码审查通过
- [x] 编译无错误无警告

---

## 归档说明

### 完成总结
本提案所有核心目标已达成:

1. **Service层异常规范化** - 101个catch-return反模式已移除(本提案4个 + eliminate-service-catch-return 97个)
2. **ViewModel层异常处理** - SafeExecuteAsync基础设施就绪，28个ViewModel已使用安全消息处理
3. **HTTP韧性层** - Polly策略已集成，11个集成测试验证通过
4. **异常消息安全化** - 100%用户可见消息已安全化，追踪码机制就绪

### 架构保留决策
- Auth模块9个catch块保留Result<T, AuthErrorCode>模式，这是架构需求而非技术债务

### 后续建议
- 新代码推荐使用SafeExecuteAsync模式
- 现有ViewModels可按需逐步迁移，无强制要求

---

## 统计摘要

| 指标 | 数值 |
|------|------|
| 发现的catch块总数 | 110个 |
| 已移除 | 101个 (本提案4个 + eliminate-service-catch-return 97个) |
| 保留Result模式 | 9个 (Auth模块 - 架构需求) |
| 改造进度 | **100%** (101/101，排除Auth模块架构需求) |
| ex.Message安全化 | 100% (所有用户可见消息已处理) |
| CorrelationId追踪 | 100% (ViewModelBase已集成) |
| Polly集成测试 | 11个测试全部通过 |
| ViewModel安全处理 | 28个文件已使用GetSafeOperationFailureMessage |
