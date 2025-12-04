# Implementation Tasks: refactor-logging-system

## Overview

本任务清单按Phase划分,覆盖日志系统重构和错误处理机制重构两大部分。
- Phase 1: Server端日志基础设施
- Phase 2: Server端错误处理机制
- Phase 3: Client端日志与错误处理
- Phase 4: 测试与验证

---

## Phase 1: Server端日志基础设施重构

### Task 1.1: 实现Serilog两阶段初始化
**Priority**: High
**Effort**: 2h
**Files**:
- `src/Server/Services/LYBT.WebAPI/Program.cs`

**Description**:
重构Program.cs,实现Bootstrap Logger + Final Logger模式,确保启动阶段异常能够被记录。

**Acceptance Criteria**:
- [ ] Bootstrap Logger在try块外初始化
- [ ] Final Logger通过UseSerilog配置
- [ ] 启动异常能够写入bootstrap日志文件
- [ ] 应用正常启动后切换到Final Logger

---

### Task 1.2: 实现CorrelationId中间件
**Priority**: High
**Effort**: 3h
**Files**:
- `src/Server/Services/LYBT.WebAPI/Middleware/CorrelationIdMiddleware.cs` (新建)
- `src/Server/Services/LYBT.WebAPI/Program.cs`

**Description**:
创建CorrelationId中间件,从请求头读取或生成CorrelationId,并通过LogContext传递。

**Acceptance Criteria**:
- [ ] 中间件读取X-Correlation-ID请求头
- [ ] 无请求头时自动生成GUID
- [ ] CorrelationId写入响应头
- [ ] LogContext.PushProperty正确注入
- [ ] 中间件在管道早期注册

---

### Task 1.3: 创建CorrelationId日志富集器
**Priority**: Medium
**Effort**: 1h
**Files**:
- `src/Server/Core/LYBT.Infrastructure/Logging/CorrelationIdEnricher.cs` (新建)

**Description**:
创建Serilog Enricher,从AsyncLocal或HttpContext读取CorrelationId。

**Acceptance Criteria**:
- [ ] 实现ILogEventEnricher接口
- [ ] 支持从HttpContext.Items获取CorrelationId
- [ ] 无CorrelationId时使用默认值"N/A"

---

### Task 1.4: 整合敏感数据脱敏组件
**Priority**: Medium
**Effort**: 2h
**Files**:
- `src/Server/Core/LYBT.Infrastructure/Logging/SensitiveDataMasker.cs`
- `src/Server/Core/LYBT.Infrastructure/Utilities/LogSanitizer.cs`

**Description**:
将LogSanitizer的正则脱敏功能整合到SensitiveDataMasker,统一敏感数据处理入口。

**Acceptance Criteria**:
- [ ] SensitiveDataMasker支持正则模式脱敏
- [ ] 连接字符串、密码等文本模式脱敏正常工作
- [ ] LogSanitizer标记为Obsolete或删除
- [ ] 现有测试通过

---

### Task 1.5: 增强GlobalExceptionHandler日志
**Priority**: Medium
**Effort**: 1h
**Files**:
- `src/Server/Services/LYBT.WebAPI/Middleware/GlobalExceptionHandler.cs`

**Description**:
增强异常处理日志,包含CorrelationId和结构化异常详情。

**Acceptance Criteria**:
- [ ] 日志包含CorrelationId
- [ ] 使用结构化日志记录异常详情
- [ ] 包含请求路径、方法、用户ID等上下文

---

### Task 1.6: 更新Serilog配置
**Priority**: High
**Effort**: 1h
**Files**:
- `src/Server/Services/LYBT.WebAPI/appsettings.json`
- `src/Server/Services/LYBT.WebAPI/appsettings.Development.json`

**Description**:
更新Serilog配置,添加Enrichers和优化输出模板。

**Acceptance Criteria**:
- [ ] 配置包含CorrelationId输出模板
- [ ] 添加MachineName、ThreadId Enrichers
- [ ] Development环境Console输出简化

---

### Task 1.7: 添加Serilog.Sinks.MSSqlServer依赖
**Priority**: High
**Effort**: 0.5h
**Files**:
- `Directory.Packages.props`
- `src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj`

**Description**:
添加Serilog数据库Sink依赖,用于将Warning+级别日志持久化到数据库。

**Acceptance Criteria**:
- [ ] Directory.Packages.props添加Serilog.Sinks.MSSqlServer版本
- [ ] WebAPI项目引用PackageReference
- [ ] dotnet restore成功

---

### Task 1.8: 创建SystemLogs数据库表迁移
**Priority**: High
**Effort**: 1h
**Files**:
- `src/Server/Data/LYBT.Persistence/Migrations/AddSystemLogsTable.cs` (新建)

**Description**:
创建EF Core迁移,生成SystemLogs表用于存储Warning+级别日志。

**Acceptance Criteria**:
- [ ] SystemLogs表包含所有设计字段(Id, Timestamp, Level, Message, Exception等)
- [ ] 创建Timestamp、Level、CorrelationId、UserId索引
- [ ] dotnet ef migrations add成功
- [ ] dotnet ef database update成功

---

### Task 1.9: 配置MSSqlServer Sink
**Priority**: High
**Effort**: 2h
**Files**:
- `src/Server/Services/LYBT.WebAPI/Program.cs`
- `src/Server/Services/LYBT.WebAPI/Configuration/SerilogMSSqlServerConfiguration.cs` (新建)
- `src/Server/Services/LYBT.WebAPI/appsettings.json`

**Description**:
配置Serilog MSSqlServer Sink,将Warning+级别日志写入SystemLogs表。

**Acceptance Criteria**:
- [ ] 配置restrictedToMinimumLevel为Warning
- [ ] 配置ColumnOptions映射自定义列
- [ ] 支持从appsettings.json读取连接字符串
- [ ] 异步批量写入避免性能影响
- [ ] 验证日志正确写入数据库

---

### Task 1.10: 实现LoggingLevelSwitch动态调试
**Priority**: Medium
**Effort**: 2h
**Files**:
- `src/Server/Services/LYBT.WebAPI/Controllers/Admin/LoggingAdminController.cs` (新建)
- `src/Server/Services/LYBT.WebAPI/Program.cs`

**Description**:
实现动态日志级别切换API,允许管理员在生产环境临时开启Debug日志。

**Acceptance Criteria**:
- [ ] LoggingLevelSwitch注册为Singleton
- [ ] GET api/admin/logging/level返回当前日志级别
- [ ] POST api/admin/logging/level支持设置日志级别
- [ ] 仅Admin角色可访问
- [ ] 级别变更记录Warning级别日志

---

### Task 1.11: 实现日志保留策略清理作业
**Priority**: Low
**Effort**: 2h
**Files**:
- `src/Server/Services/LYBT.WebAPI/BackgroundServices/LogCleanupService.cs` (新建)

**Description**:
创建后台服务定期清理过期日志(Warning 90天, Error永久保留)。

**Acceptance Criteria**:
- [ ] 继承BackgroundService实现定时任务
- [ ] 每日凌晨执行清理
- [ ] Warning级别日志保留90天
- [ ] Error/Fatal级别日志永久保留
- [ ] 清理操作记录日志

---

## Phase 2: Server端错误处理机制重构

### Task 2.1: 实现RFC 7807 Problem Details配置
**Priority**: High
**Effort**: 2h
**Files**:
- `src/Server/Services/LYBT.WebAPI/Program.cs`
- `src/Server/Services/LYBT.WebAPI/Configuration/ProblemDetailsConfiguration.cs` (新建)

**Description**:
配置ASP.NET Core内置的Problem Details支持,实现RFC 7807标准化错误响应。

**Acceptance Criteria**:
- [ ] 添加AddProblemDetails()服务配置
- [ ] CustomizeProblemDetails注入CorrelationId和timestamp
- [ ] 配置UseExceptionHandler()和UseStatusCodePages()
- [ ] 错误响应包含type、title、status、detail、instance字段

---

### Task 2.2: 创建ErrorCode枚举体系
**Priority**: High
**Effort**: 1.5h
**Files**:
- `src/Shared/LYBT.Shared.Models/Errors/ErrorCode.cs` (新建)
- `src/Shared/LYBT.Shared.Models/Errors/ErrorCategory.cs` (新建)

**Description**:
创建分层错误码枚举,按模块划分(0xxxx-7xxxx),便于前端显示和问题定位。

**Acceptance Criteria**:
- [ ] ErrorCode枚举包含通用错误(0xxxx)
- [ ] 各业务模块错误码分区(Users/Patients/MedicalCase等)
- [ ] ErrorCategory枚举定义错误类别
- [ ] ErrorCode包含XML文档注释

---

### Task 2.3: 扩展AppException异常体系
**Priority**: High
**Effort**: 1.5h
**Files**:
- `src/Server/Core/LYBT.Infrastructure/Exceptions/AppException.cs`
- `src/Server/Core/LYBT.Infrastructure/Exceptions/ConflictException.cs` (新建)
- `src/Server/Core/LYBT.Infrastructure/Exceptions/UnauthorizedException.cs` (新建)

**Description**:
扩展现有AppException体系,添加ErrorCode属性和新的异常类型。

**Acceptance Criteria**:
- [ ] AppException新增ErrorCode属性
- [ ] 新增ConflictException(并发冲突,HTTP 409)
- [ ] 新增UnauthorizedException(授权失败,HTTP 401)
- [ ] 保持向后兼容

---

### Task 2.4: 实现IExceptionHandler处理器链
**Priority**: High
**Effort**: 3h
**Files**:
- `src/Server/Services/LYBT.WebAPI/ExceptionHandlers/BusinessExceptionHandler.cs` (新建)
- `src/Server/Services/LYBT.WebAPI/ExceptionHandlers/SystemExceptionHandler.cs` (新建)
- `src/Server/Services/LYBT.WebAPI/Program.cs`

**Description**:
使用ASP.NET Core 8的IExceptionHandler接口实现异常处理器链模式。

**Acceptance Criteria**:
- [ ] BusinessExceptionHandler处理AppException及子类
- [ ] SystemExceptionHandler处理系统异常(兜底)
- [ ] 异常处理器按优先级注册
- [ ] 日志包含CorrelationId和结构化异常信息
- [ ] 返回标准ProblemDetails格式

---

### Task 2.5: 创建ConfigurableErrorMessageMapper
**Priority**: Medium
**Effort**: 1.5h
**Files**:
- `src/Server/Core/LYBT.Infrastructure/Errors/ConfigurableErrorMessageMapper.cs` (新建)
- `src/Server/Services/LYBT.WebAPI/appsettings.json`

**Description**:
创建基于配置的错误消息映射器,支持在配置文件中定义错误消息。

**Acceptance Criteria**:
- [ ] 实现IErrorMessageMapper接口
- [ ] 支持从IConfiguration读取ErrorMessages节
- [ ] 支持ErrorCode到友好消息的映射
- [ ] 提供默认消息回退

---

### Task 2.6: 重构GlobalExceptionHandler
**Priority**: Medium
**Effort**: 1h
**Files**:
- `src/Server/Services/LYBT.WebAPI/Middleware/GlobalExceptionHandler.cs`

**Description**:
将现有GlobalExceptionHandler迁移到IExceptionHandler模式,或标记为备用。

**Acceptance Criteria**:
- [ ] GlobalExceptionHandler标记为[Obsolete]或删除
- [ ] 新IExceptionHandler链完全接管异常处理
- [ ] 验证所有异常类型都能正确处理

---

## Phase 3: Client端日志与错误处理集成

### Task 3.1: 添加Serilog NuGet依赖
**Priority**: High
**Effort**: 0.5h
**Files**:
- `Directory.Packages.props`
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj`
- `src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`

**Description**:
在Directory.Packages.props中添加客户端所需的Serilog包版本。

**Acceptance Criteria**:
- [ ] 添加Serilog.Extensions.Logging版本
- [ ] 项目引用PackageReference
- [ ] dotnet restore成功

---

### Task 3.2: 实现客户端Serilog配置
**Priority**: High
**Effort**: 2h
**Files**:
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Logging/DesktopSerilogConfiguration.cs` (新建)

**Description**:
创建客户端Serilog配置类,配置文件日志输出。

**Acceptance Criteria**:
- [ ] 日志输出到%LOCALAPPDATA%/LYBTZYZS/logs
- [ ] 按天Rolling,保留30天
- [ ] 输出模板与Server端一致
- [ ] 添加Application属性标识客户端

---

### Task 3.3: 实现CorrelationId上下文
**Priority**: High
**Effort**: 1.5h
**Files**:
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Logging/CorrelationIdContext.cs` (新建)

**Description**:
使用AsyncLocal实现客户端CorrelationId上下文管理。

**Acceptance Criteria**:
- [ ] AsyncLocal存储当前CorrelationId
- [ ] 支持创建新的CorrelationId作用域
- [ ] 线程安全

---

### Task 3.4: 实现HTTP请求CorrelationId注入
**Priority**: High
**Effort**: 2h
**Files**:
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Logging/CorrelationIdDelegatingHandler.cs` (新建)
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ApiClient/` (相关配置)

**Description**:
创建DelegatingHandler,在HTTP请求中注入X-Correlation-ID头。

**Acceptance Criteria**:
- [ ] 从CorrelationIdContext获取当前ID
- [ ] 无ID时自动生成
- [ ] 注入X-Correlation-ID请求头
- [ ] HttpClient配置中注册Handler

---

### Task 3.5: 实现ProblemDetailsResponse解析
**Priority**: High
**Effort**: 2h
**Files**:
- `src/Shared/LYBT.Shared.Models/Errors/ProblemDetailsResponse.cs` (新建)
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ApiClient/ProblemDetailsParser.cs` (新建)

**Description**:
创建客户端ProblemDetails响应解析器,处理服务端返回的RFC 7807错误响应。

**Acceptance Criteria**:
- [ ] ProblemDetailsResponse模型包含所有标准字段
- [ ] 支持Extensions字典(errorCode, correlationId等)
- [ ] 解析器能从HttpResponseMessage提取ProblemDetails
- [ ] 处理非ProblemDetails格式的错误响应

---

### Task 3.6: 重构ErrorHandlingService
**Priority**: High
**Effort**: 2h
**Files**:
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ErrorHandling/ErrorHandlingService.cs`
- `src/Client/Desktop/Shell/Extensions/ErrorHandlingServiceExtensions.cs`

**Description**:
重构错误处理服务,集成Serilog和ProblemDetails处理。

**Acceptance Criteria**:
- [ ] 使用Serilog作为日志提供程序
- [ ] 错误日志包含CorrelationId
- [ ] 支持解析ProblemDetails获取ErrorCode
- [ ] 根据ErrorCode显示友好错误消息

---

### Task 3.7: 增强StandardExceptionHandler日志
**Priority**: Medium
**Effort**: 1h
**Files**:
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Exceptions/StandardExceptionHandler.cs`

**Description**:
增强异常处理日志,使用结构化日志记录。

**Acceptance Criteria**:
- [ ] 使用结构化日志格式
- [ ] 包含异常类型、消息、堆栈
- [ ] 包含操作上下文信息

---

### Task 3.8: 客户端错误消息本地化
**Priority**: Medium
**Effort**: 1.5h
**Files**:
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Errors/ClientErrorMessageMapper.cs` (新建)
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Resources/ErrorMessages.resx` (新建)

**Description**:
创建客户端错误消息映射器,支持ErrorCode到友好消息的本地化映射。

**Acceptance Criteria**:
- [ ] 实现IClientErrorMessageMapper接口
- [ ] ErrorCode到中文友好消息映射
- [ ] 资源文件支持未来多语言扩展
- [ ] 未知ErrorCode返回默认消息

---

## Phase 4: 测试与验证

### Task 4.1: Server端日志组件单元测试
**Priority**: High
**Effort**: 2h
**Files**:
- `tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/Logging/CorrelationIdEnricherTests.cs` (新建)
- `tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/Logging/SensitiveDataMaskerTests.cs` (更新)

**Description**:
为新增的日志组件编写单元测试。

**Acceptance Criteria**:
- [ ] CorrelationIdEnricher测试覆盖
- [ ] SensitiveDataMasker整合功能测试
- [ ] 测试通过率100%

---

### Task 4.2: CorrelationId中间件集成测试
**Priority**: Medium
**Effort**: 1.5h
**Files**:
- `tests/IntegrationTests/WebAPI.IntegrationTests/Middleware/CorrelationIdMiddlewareTests.cs` (新建)

**Description**:
验证CorrelationId中间件在完整请求管道中的行为。

**Acceptance Criteria**:
- [ ] 测试请求头传递场景
- [ ] 测试自动生成场景
- [ ] 测试响应头返回

---

### Task 4.3: IExceptionHandler单元测试
**Priority**: High
**Effort**: 2h
**Files**:
- `tests/UnitTests/Server/Services/LYBT.WebAPI.Tests/ExceptionHandlers/BusinessExceptionHandlerTests.cs` (新建)
- `tests/UnitTests/Server/Services/LYBT.WebAPI.Tests/ExceptionHandlers/SystemExceptionHandlerTests.cs` (新建)

**Description**:
为异常处理器链编写单元测试。

**Acceptance Criteria**:
- [ ] BusinessExceptionHandler正确处理AppException及子类
- [ ] SystemExceptionHandler作为兜底处理器工作正常
- [ ] ProblemDetails响应格式符合RFC 7807
- [ ] 测试覆盖率>80%

---

### Task 4.4: Problem Details集成测试
**Priority**: High
**Effort**: 2h
**Files**:
- `tests/IntegrationTests/WebAPI.IntegrationTests/ErrorHandling/ProblemDetailsIntegrationTests.cs` (新建)

**Description**:
验证完整的错误处理流程和ProblemDetails响应。

**Acceptance Criteria**:
- [ ] 验证ValidationException返回400 + ProblemDetails
- [ ] 验证NotFoundException返回404 + ProblemDetails
- [ ] 验证ConflictException返回409 + ProblemDetails
- [ ] 验证未处理异常返回500 + ProblemDetails
- [ ] 响应包含CorrelationId和ErrorCode

---

### Task 4.5: 端到端日志追踪验证
**Priority**: High
**Effort**: 1h
**Files**: N/A (手动测试)

**Description**:
手动验证客户端请求到服务端的完整CorrelationId追踪。

**Acceptance Criteria**:
- [ ] 客户端日志包含CorrelationId
- [ ] 服务端日志包含相同CorrelationId
- [ ] 可通过CorrelationId关联两端日志
- [ ] 错误响应包含CorrelationId

---

### Task 4.6: 日志文件输出验证
**Priority**: Medium
**Effort**: 0.5h
**Files**: N/A (手动测试)

**Description**:
验证日志文件Rolling和保留策略。

**Acceptance Criteria**:
- [ ] Server端日志按天Rolling
- [ ] Client端日志按天Rolling
- [ ] 超过30天的日志自动删除

---

### Task 4.7: 错误消息显示验证
**Priority**: Medium
**Effort**: 0.5h
**Files**: N/A (手动测试)

**Description**:
验证客户端错误消息显示是否友好、准确。

**Acceptance Criteria**:
- [ ] ValidationException显示具体验证错误
- [ ] NotFoundException显示"未找到XXX"类消息
- [ ] ConflictException显示并发冲突提示
- [ ] 系统错误显示通用友好消息(不暴露技术细节)

---

### Task 4.8: 数据库日志功能验证
**Priority**: High
**Effort**: 1.5h
**Files**:
- `tests/IntegrationTests/WebAPI.IntegrationTests/Logging/DatabaseLoggingTests.cs` (新建)

**Description**:
验证数据库日志存储功能正确工作。

**Acceptance Criteria**:
- [ ] Warning级别日志正确写入SystemLogs表
- [ ] Error级别日志正确写入SystemLogs表
- [ ] Information级别日志不写入数据库
- [ ] CorrelationId正确记录到数据库
- [ ] 验证日志列数据完整性

---

### Task 4.9: LoggingAdminController测试
**Priority**: Medium
**Effort**: 1h
**Files**:
- `tests/UnitTests/Server/Services/LYBT.WebAPI.Tests/Controllers/Admin/LoggingAdminControllerTests.cs` (新建)

**Description**:
验证日志级别动态切换API。

**Acceptance Criteria**:
- [ ] GET /api/admin/logging/level返回当前级别
- [ ] POST成功切换日志级别
- [ ] 无效级别返回BadRequest
- [ ] 非Admin角色返回403

---

## Summary

| Phase | Tasks | Total Effort |
|-------|-------|--------------|
| Phase 1: Server端日志 | 11 | ~17.5h |
| Phase 2: Server端错误处理 | 6 | ~10.5h |
| Phase 3: Client端日志与错误处理 | 8 | ~12.5h |
| Phase 4: 测试验证 | 9 | ~12h |
| **Total** | **34** | **~52.5h** |

## Dependencies

- Phase 2 依赖 Phase 1 完成(CorrelationId中间件)
- Phase 3 依赖 Phase 1 和 Phase 2 完成(日志基础设施和错误格式)
- Phase 4 依赖 Phase 1、Phase 2、Phase 3 完成

## Risks

1. **Client端DI配置复杂度**: Prism框架的DI容器与Serilog集成需要验证
2. **日志文件权限**: %LOCALAPPDATA%路径在某些环境可能有权限问题
3. **IExceptionHandler兼容性**: 确保与现有GlobalExceptionHandler平滑过渡
4. **ErrorCode枚举扩展**: 需要建立ErrorCode分配规范避免冲突
