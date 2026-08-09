# A-31-C1 日志集中定义报告（Shared.Logging 升级独立完整日志项目）

> 任务书：`docs/compose/specs/task-a31-c1-logging-centralization-2026-08-08.md`｜派发：Mimo Code｜版本：v1.0
> 完成日期：2026-08-09｜基线：C-0 完成（`f4957b7e7`）｜技术引入治理：蓝图 §0.5 + P05b 豁免清单（文档先行）
> 硬性门禁：`dotnet build LYBTZYZS.sln --no-incremental` **0 错误 0 警告** + `dotnet test tests/LYBT.Tests.Architecture/` 全绿

---

## 1. 迁移清单执行结果（M1-M10）

### M1 ✅ DesktopSerilogConfiguration → Bootstrap/LybtLoggingOptions + LoggingBootstrap

- 源 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Logging/DesktopSerilogConfiguration.cs` **删除**
- 目标：`Bootstrap/LybtLoggingOptions.cs`（文件路径/应用名/最低级别/MSSQL 开关，合并 Desktop + MSSQL 配置项）+ `Bootstrap/LoggingBootstrap.cs`
- `LoggingBootstrap` 接管静态入口：`Initialize(Action<LybtLoggingOptions>?)`（文件+控制台 sink + UseSharedLogging + 脱敏 + `Application=LYBT.Desktop`）、`CloseAndFlush()`、`CorrelationIdProvider`/`LoggingLevelManager` 全局单例
- `UseSharedLogging` **保留**（`Extensions/LoggerConfigurationExtensions.cs`），由 Bootstrap 内部使用

### M2 ✅ SerilogMSSqlServerExtensions → Sinks/MssqlSinkConfiguration（options 化，代码优先）

- 源 `WebAPI/Extensions/SerilogMSSqlServerExtensions.cs` **删除**
- 目标：`Sinks/MssqlSinkConfiguration.cs`（`WriteToMssqlSink(connectionString, Action<MssqlSinkOptions>?)` + `MssqlSinkOptions`：TableName/SchemaName/**AutoCreateSqlTable 默认 false**/BatchPostingLimit/BatchPeriod/RestrictedToMinimumLevel）
- **JSON/代码冲突解决**：`appsettings.Production.json:73` 的 `autoCreateSqlTable:true` MSSqlServer WriteTo 条目**整体移除**（仅保留 Console+File），MSSQL sink 完全代码驱动（默认 false，由 EF 迁移管理表结构）——**代码优先**

### M3 ✅ LoggingRegistrationExtensions → Bootstrap/ServiceCollectionExtensions（RegisterLogging 合并）

- 源 `Shell/Extensions/LoggingRegistrationExtensions.cs` **删除**
- 目标：`Bootstrap/ServiceCollectionExtensions.cs` → `AddLybtLogging(this IServiceCollection)`：
  - `AddSingleton<ICorrelationIdProvider>`（经 `LoggingBootstrap.CorrelationIdProvider`，**解决 S1 发现的零注册问题**）
  - `AddSingleton(LoggingBootstrap.LoggingLevelManager)`（与 Final Logger 的 LevelSwitch 同实例，运行时调级生效）
  - `TryAddSingleton<ILoggerFactory>` + `TryAddSingleton(typeof(ILogger<>), typeof(Logger<>))`（宿主已注册时不覆盖，保持宿主日志管道；原 RegisterLogging 的 ILoggerFactory+ILogger<> 逻辑合并于此）
- Desktop Prism 容器适配：Shell `ServiceCollectionExtensions.cs` 新增 `RegisterLoggingServices(IContainerRegistry)`（3 行，全部委托 LoggingBootstrap——Provider 单例 + CreateLoggerFactory；Prism 无 IServiceCollection 桥接，Shared.Logging 不引入 Prism 依赖，与 PrismConfigurationExtensions 先例一致）

### M4 ✅ LoggingHttpHandler → Http/LoggingHttpHandler（Provider 化）

- 源 `Desktop.Foundation/Http/LoggingHttpHandler.cs` **删除**
- 目标：`Http/LoggingHttpHandler.cs`，构造改为 `(ILogger<LoggingHttpHandler>, ICorrelationIdProvider)`
- **CorrelationId 直读 `activity.Id`（:28）替换为 `_correlationIdProvider.GetCorrelationId() ?? Guid.NewGuid()`**；traceparent header 保持 Activity 机制（W3C 传播语义不变）
- 消费方 `Shell/Extensions/UnifiedApiClientExtensions.cs:85-89` 改调（`container.Resolve<ICorrelationIdProvider>()`）

### M5 ✅ CorrelationIdMiddleware → Http/CorrelationIdMiddleware + UseLybtCorrelationId

- 源 `WebAPI/Middleware/CorrelationIdMiddleware.cs` **删除**
- 目标：`Http/CorrelationIdMiddleware.cs`（逻辑逐行保留：traceparent→X-Correlation-ID→短 GUID→TraceIdentifier/LogContext）
- 扩展更名 `UseCorrelationId` → **`UseLybtCorrelationId`**（单点注册）；共享辅助 `GetCorrelationId(this HttpContext)` 随迁（ProblemDetailsConfiguration 消费方同步改 using）
- `HttpHeaderConstants`（Infrastructure）常量内联为中间件私有常量（P05d 禁止 Shared→Infrastructure）
- 消费方 `Extensions/UnifiedMiddlewareConfiguration.cs:98` 改调 `UseLybtCorrelationId()`

### M6 ✅ ApiLoggingFilter → Http/ApiLoggingFilter + AddLybtApiLoggingFilter

- 源 `WebAPI/Filters/ApiLoggingFilter.cs` **删除**
- 目标：`Http/ApiLoggingFilter.cs`，CorrelationId 由 `context.HttpContext.TraceIdentifier` 直读（:25）改为**共享扩展 `GetCorrelationId(context.HttpContext)`**（TraceIdentifier 直读收敛）
- 新增 `AddLybtApiLoggingFilter()`（`Configure<MvcOptions>` 全局 Filter 单点注册）；消费方 `WebAPI/Extensions/ServiceCollectionExtensions.cs:151` 从 AddControllers 内联移除，改调扩展

### M7 ✅ WebAPI/Program.cs:75-145 内联 Serilog → LoggingBootstrap 统一入口

- Phase 1（Bootstrap Logger，:72-98）→ `LoggingBootstrap.CreateBootstrapLogger(isTestEnvironment)`（含 Test 环境特殊分支）
- Phase 2（UseSerilog 内联构建，:126-145）→ `builder.Host.AddLybtLogging(options => { ApplicationName="LYBT.WebAPI"; UseMssqlSink=true; })`；Test 环境 MSSQL 跳过逻辑保留在 Bootstrap 内（`!IsEnvironment("Test")`）
- `builder.Services.AddSingleton(LoggingLevelManager)`（:148）→ `builder.Services.AddLybtLogging()`（Provider + LevelManager 注册）
- 静态 `LoggingLevelManager` 字段（:35）删除——Bootstrap 全局单例接管

### M8 ✅ App.xaml.cs + Shell ServiceCollectionExtensions → LoggingBootstrap

- `App.xaml.cs:48` `DesktopSerilogConfiguration.Initialize()` → `LoggingBootstrap.Initialize()`
- `App.xaml.cs:75` `DesktopSerilogConfiguration.CloseAndFlush()` → `LoggingBootstrap.CloseAndFlush()`
- `Shell/Extensions/ServiceCollectionExtensions.cs:49` `containerRegistry.RegisterLogging()` → `RegisterLoggingServices(containerRegistry)`（见 M3）

### M9 ✅ SensitiveDataJsonConverterFactory 去重（复用 SensitiveDataMasker）

- `Infrastructure/Serialization/SensitiveDataJsonConverterFactory.cs`：属性敏感检查由 `property.GetCustomAttribute<SensitiveDataAttribute>()` 直读改为 **`SensitiveDataMasker.GetSensitiveDataAttribute(property)`**（单点共享；`HasSensitiveProperties` 类型级检查同步委托）
- 脱敏值计算本就经 `SensitiveDataMasker.Mask`（:141，C-0 基线已委托）——核验确认，无重复实现残留

### M10 ✅ Serilog 包从 4 项目移除 → 仅 Shared.Logging 持有

| 项目 | 移除包 |
|------|--------|
| `LYBT.Infrastructure.csproj` | `Serilog` |
| `LYBT.WebAPI.csproj` | Serilog.AspNetCore / Sinks.File / Sinks.Console / Sinks.MSSqlServer / Enrichers.Environment / Enrichers.Thread |
| `LYBT.Desktop.Infrastructure.csproj` | Serilog / Serilog.Extensions.Logging / Sinks.File / Sinks.Console / Enrichers.Environment / Enrichers.Thread |
| `LYBT.Shared.Logging.csproj` | **持有全部 8 包**（新增 Serilog.AspNetCore + Serilog.Sinks.MSSqlServer） |

- 传递引用保证消费方编译与运行不受影响（WebAPI 的 `UseSerilog`/`UseSerilogRequestLogging` 经 Shared.Logging → Serilog.AspNetCore 传递解析）

## 2. 前置（技术引入治理，文档先行）

1. ✅ 蓝图 `14-structure-design-blueprint.md` §0.5.1 技术栈表标注 Serilog 收敛至 Shared.Logging + §1 表 Shared.Logging 职责扩展（15 文件）+ §0.5 治理记录（ASP.NET Core 依赖 + Shared.Configuration 引用获批，P05b 豁免）
2. ✅ 架构测试 `P05b_SharedUtilities_Should_Not_Depend_On_AspNetCore` 豁免清单同步：范围由 `LYBT.Shared.Models.Utilities` 扩展为 `LYBT.Shared` 排除 `LYBT.Shared.Logging`（唯一 AspNetCore 依赖例外），测试注释 + `ARCHITECTURE-RULES.md` 同步
3. ✅ `Directory.Packages.props` 新增 `Microsoft.AspNetCore.Mvc.Abstractions` / `Mvc.Core`（2.3.9，与 Http.Abstractions 对齐）

## 3. 验证结果（真实输出）

| 验证 | 命令 | 结果 |
|------|------|------|
| 全量构建 | `dotnet build LYBTZYZS.sln --no-incremental` | ✅ **0 错误 0 警告**（首次失败 1 项：测试 using 旧命名空间，修复后通过） |
| 架构测试 | `dotnet test tests/LYBT.Tests.Architecture/ --no-build` | ✅ **88/88 通过**（P05b 豁免更新后） |
| Server 日志单测 | `dotnet test tests/LYBT.Tests.Server/ --filter ~CorrelationIdMiddleware\|~SensitiveDataMasker\|~LoggingLevelManager` | ✅ **39/39**（含迁移后的 CorrelationIdMiddleware 行为测试） |
| Desktop 日志单测 | `dotnet test tests/LYBT.Tests.Desktop/ --filter ~SensitiveDataMasker\|~LoggingLevelManager` | ✅ **28/28** |

> 备注：Server/Desktop 全量集成测试（真实 SQL Server/LocalDB）未运行——本批次为日志基础设施迁移（不涉业务逻辑/数据），已用全量构建 + 架构测试 + 日志专项单测覆盖；迁移为同构搬移（中间件/Filter/Handler 逻辑逐行保留，仅改命名空间与 CorrelationId 取值来源）。

## 4. 残留检查

| 检查项 | 结果 |
|--------|------|
| 旧类型引用残留（DesktopSerilogConfiguration / AddMSSqlServerSinkWithColumnOptions / RegisterLogging / UseCorrelationId / Shared.Logging.Abstractions / .Enrichers） | ✅ 0 处（仅 LybtLoggingOptions 文档注释提及迁移来源，非代码） |
| Serilog 包引用 | ✅ 仅 `LYBT.Shared.Logging.csproj`（8 包） |
| appsettings.*.json MSSqlServer/autoCreateSqlTable | ✅ 0 处（冲突已消除，代码优先） |
| `TraceIdentifier` 直接读取残留 | ⏸ 14 处——均为**有意保留**：① 异常处理器 7 处（`BusinessExceptionHandler`/`SystemExceptionHandler`）属 **C-2 异常批次**（任务书明确不做）；② `BaseApiController.cs:58`/`ControllerBaseExtensions.cs:13` RequestId 辅助与 `ProblemDetailsConfiguration.cs:31` traceId 字段、`UnifiedMiddlewareConfiguration.cs:58,71,72`、`WebAPI ServiceCollectionExtensions.cs:200` 为 RequestId/TraceId 语义字段——中间件已将 `TraceIdentifier` 赋值为 correlationId，取值一致；③ 中间件本身 2 处为赋值源。**收敛路径**：C-2 批次上移 `GetCorrelationId(HttpContext)` 共享辅助后统一替换 |
| `UseSharedLogging` / `WithSensitiveDataMasking` | ✅ 保留（Bootstrap 内部使用），未删 |
| 业务模块日志调用（ILogger 消费方） | ✅ 未改动（Surgical） |
| `MaskObject`/`SanitizeException`（D 级死方法） | ⏸ 保留——属 C-6 死代码清理批次（S1 报告 §6，删除需技术总监审批 + 联动单测） |
| 双控制器树（权限/端点） | ✅ 本批次无权限/端点变更，无需双树同步 |

## 5. 交付物清单

- **新增**（Shared.Logging 10 文件）：`Bootstrap/`×3、`Correlation/`×3（迁移）、`Http/`×3、`Sinks/`×1
- **删除**：6 个迁移源文件 + Shared.Logging 旧目录 3 文件（Abstractions×2/Enrichers×1 → Correlation）
- **修改**：WebAPI Program.cs / UnifiedMiddlewareConfiguration.cs / ServiceCollectionExtensions.cs / ProblemDetailsConfiguration.cs / appsettings.Production.json；Shell App.xaml.cs / ServiceCollectionExtensions.cs / UnifiedApiClientExtensions.cs；SensitiveDataJsonConverterFactory.cs；csproj ×4；Directory.Packages.props；P05b + ARCHITECTURE-RULES.md；蓝图 §0.5；总账 A-31；测试 using 修复（CorrelationIdMiddlewareTests）

**未 commit**：由本任务统一单 commit + push（`refactor(logging): A-31-C1 ...`）。
