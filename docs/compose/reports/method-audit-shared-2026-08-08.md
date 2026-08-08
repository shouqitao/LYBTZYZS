# Shared 层方法级深审报告（Mimo 独立分析）

> 任务：A-30-S1｜派发：Mimo Code（只读审查）｜版本：v1.0
> 产出日期：2026-08-09｜基线 commit：`7edf020f5`（S0）｜任务书：`docs/compose/specs/task-a30-s1-shared-audit-2026-08-08.md`
> 性质：**只读**，未修改任何 src/tests 代码，未 commit

---

## 0. 方法说明（工具/基线/时间点）

| 项 | 说明 |
|----|------|
| 输入 | S0 基线（`method-audit-baseline-2026-08-08.md` 方法总账）+ 重复聚类（693 组）+ 死方法候选（1095 项） |
| 复核工具 | 全仓 src/（排除 bin/obj）PowerShell Select-String 符号级调用计数；框架反射模式（`ILogEventEnricher.Enrich`/`IDestructuringPolicy.TryDestructure`/`IExceptionHandler.TryHandleAsync`/DI 构造/`AddValidatorsFromAssembly` 反射实例化）单独识别，不误判为死 |
| 复核范围 | Shared 5 项目全部方法（Entities 39 / Configuration 9 / ExceptionHandling 61 / Logging 38 / Models 65 = **212 方法**）+ 跨层消费方（Server/Desktop 调用点）只读核实 |
| 时间点 | S0 生成 2026-08-09 02:10；本报告符号级复核 2026-08-09（同基线代码树，无代码变更） |
| 已知盲区 | ① 反射/DI 构造（`new()` target-typed、`Configure<T>` Options 绑定）grep 不可见，已逐项人工排除；② 子类 `: base(...)` 链调用基类构造 grep 不可见，AppException 构造经此排除"死"判定 |

---

## 1. 5 项目方法分级总表（A/B/C/D/E 计数）

| 项目 | 类型数 | 方法数 | A 依据充分 | B 支撑性 | C 重复分散 | D 孤儿/死 | E 可集中 |
|------|-------|-------|-----------|---------|-----------|----------|---------|
| LYBT.Entities | 18 | 39 | 39 | 0 | 0 | 0 | 0 |
| LYBT.Shared.Configuration | 33 | 9 | 7 | 1 | 0 | 1 | 0 |
| LYBT.Shared.ExceptionHandling | 7 | 61 | 19 | 0 | 0 | 42 | 0 |
| LYBT.Shared.Logging | 10 | 38 | 19 | 12 | 0 | 7 | 0 |
| LYBT.Shared.Models | 142 | 65 | 55 | 4 | 0 | 6 | 0 |
| **合计** | **210** | **212** | **139** | **17** | **0（跨项目 C 级 11 组见 §5）** | **56** | **6 文件 + 5 机制见 §5** |

> 注：① C/E 级主要落在**跨项目**维度（Shared 方法被 Server/Desktop 重复实现/散落实现），Shared 项目内部方法本身无 C 级；② D 级 56 项中含 **4 个整类死类**（ConflictException 8 / ApiException 11 / UnauthorizedException 11 / ValidationException 7 = 37 方法，见 §6）；③ E 级 6 文件为散落在 4 个消费项目的日志/异常机制文件（§2/§3 详述）。

### 1.1 LYBT.Entities（A 级 39/39）

全部为 DDD 领域工厂/行为方法，逐项核实调用链完整（示例证据）：

| 方法 | 调用链证据（文件:行号） |
|------|----------------------|
| `Formula.SoftDelete/Restore/ChangeStatus` | `LYBT.Module.Formula/Application/Commands/DeleteFormulaCommandHandler.cs:32`、`RestoreFormulaCommandHandler.cs:36`、`BatchEnableFormulasCommandHandler.cs:35` |
| `Herb.SoftDelete/Restore/ChangeStatus` | `DeleteHerbCommandHandler.cs:28`、`RestoreHerbCommandHandler.cs:36`、`BatchDisableHerbsCommandHandler.cs:35` |
| `AuthSession.Create/Logout/Revoke/IsValid/IsExpired` | `LogoutCommandHandler.cs:36,38`、`RefreshTokenCommandHandler.cs:38,94`、`AuthSessionRepository.cs:55`、`ValidateTokenQueryHandler.cs:53` |
| `FormulaHerbItem.BindHerb` | `ValidateFormulaHerbCommandHandler.cs:37` |
| `Formula.AddHerb` | `BatchImportFormulasCommandHandler.cs:90` |
| `Patient/Formula/Herb/ApplicationUser.UpdateProfile` | `UpdatePatientCommandHandler.cs`、`UpdateHerbCommandHandler.cs:35`、`UpdateFormulaCommandHandler.cs:35`、`BatchImportPatientsCommandHandler.cs:75` |
| `Registration.StartVisit/Complete/Cancel/RevertToWaiting/AssignMedicalCase` | `StartVisitCommandHandler.cs:41,56`、`RegistrationCrossModuleService.cs:26,39,56` |
| `MedicalCase.Complete/Suspend/UpdateConsultation/SoftDelete` | `MedicalCaseStateService.cs:150,206,211` |

> 同名方法（`Create`×6 / `UpdateProfile`×4 / `ChangeStatus`×5 / `SoftDelete`×5 / `Restore`×5）为**逐实体 DDD 惯用法**，各自有真实调用链，**不判 C 级**（同名不同实体，非重复实现）。

### 1.2 LYBT.Shared.Configuration（A 7 / B 1 / D 1）

| 方法 | 级 | 证据 |
|------|----|------|
| `ConnectionStringResolver.GetEffectiveConnectionString` | A | 7 个 Server 模块调用：`AuthModule.cs:30`、`FormulaModule.cs:42`、`HerbsModule.cs:33`、`MedicalCaseModule.cs:35`、`PatientsModule.cs:32`、`RegistrationModule.cs:33`、`UsersModule.cs:33` |
| `ConnectionStringResolver.FirstNonEmpty` | B | 私有辅助 |
| `ServerConfigurationExtensions.AddLybtServerConfiguration` | A | `WebAPI/Program.cs:160` |
| `ClientConfigurationExtensions.AddLybtClientConfiguration` | A→**D** | **生产零调用**（仅测试 `ConfigurationLoadingTests.cs:86`）；Desktop 实际走 Shell 同名 `PrismConfigurationExtensions.cs:21`（IContainerRegistry 重载，`ServiceCollectionExtensions.cs:74`），两版注册集合不一致（Prism 版多注册 DefaultPasswordOptions/LocalJwtOptions，`PrismConfigurationExtensions.cs:44,47`）→ **Shared 版死代码 + 命名混淆** |
| `LoginRateLimitOptions` 构造 | A | `SecurityOptions.cs:36` `= new()` 初始化（target-typed，S0 误报候选已排除） |
| `DatabaseOptionsValidator/JwtOptionsValidator/LocalJwtOptionsValidator/SecurityOptionsValidator.Validate` ×4 | A | 注册：`ServerConfigurationExtensions.cs:24-26`、`ClientConfigurationExtensions.cs:23`（ValidateOnStart 反射调用） |

### 1.3 LYBT.Shared.ExceptionHandling（A 19 / D 42）

- **A 级（19）**：`AppException` 全 8 方法（构造经子类 `: base(...)` 链实际生效——`BusinessException.cs:26` 等；`GetHttpStatusCode:35` 被 `BusinessExceptionHandler.cs:49` 虚方法派发）；`BusinessException` 全 6（src 16 处构造，全在 MedicalCase 模块：`MedicalCaseStateService.cs:62,78,...`、`MedicalCaseServiceHelper.cs:78,94,102`、`MedicalCaseCommandService.cs:109`、`MedicalCaseCommandService.Deletion.cs:42`）；`NotFoundException` 构造 4 + `GetHttpStatusCode`（src 3 处：`MedicalCaseServiceHelper.cs:71,82`、`MedicalCaseRepository.Update.cs:195`）。
- **D 级（42）**：见 §6 —— 含 **4 个整类死类**（ConflictException / ApiException / UnauthorizedException / ValidationException = 37 方法）与 `NotFoundException` 5 个静态工厂。
  - ⚠️ 符号级确认：`ValidationBehavior.cs:35` 的 `throw new ValidationException(failures)` 为 **FluentValidation.ValidationException**（文件 `using FluentValidation`，参数 `List<ValidationFailure>`），共享 `ValidationException`（构造参数为 string/Dictionary）src 内 **0 次直接构造** → 整类死。

### 1.4 LYBT.Shared.Logging（A 19 / B 12 / D 7）

- **A 级（19）**：`GetCorrelationId`（`CorrelationIdEnricher.cs:56` 消费）、`Enrich`（ILogEventEnricher 框架接口）、`TryDestructure`（IDestructuringPolicy 框架接口）、`WithCorrelationId`（`LoggerConfigurationExtensions.cs:44`）、`UseSharedLogging`（`DesktopSerilogConfiguration.cs:57`）、`WithSensitiveDataMasking`（`WebAPI/Program.cs:135`）、`LoggingLevelManager` 5 方法（`Program.cs:35,148`、双端 `DiagnosticsController`、`LogLevelControlViewModel.cs:43-102`）、`Mask/MaskUri/SanitizeText/IsSensitiveFieldName/SerializeWithSanitization/GetSensitiveDataAttribute`（`LoggingHttpHandler.cs:38,71`、`ApiLoggingFilter.cs:85`、`BaseApiController.cs:43`、`SensitiveDataJsonConverterFactory.cs:141`、`SensitiveDataDestructuringPolicy.cs:54`）。
- **B 级（12）**：私有正则 ×4、`MaskPartial/MaskHash/MaskDefault`、`LoggingLevelManager.Dispose×2`、`SanitizingJsonConverter.CanConvert/Read/Write`（内部经 `SerializeWithSanitization` 使用）。
- **D 级（7）**：`SetCorrelationId`（实现+接口，0 调用）、`GetCorrelationIdOrNew`（0）、`WriteToConsoleWithTemplate/WriteToFileWithTemplate`（0）、`MaskObject`（仅测试）、`SanitizeException`（仅测试）。

### 1.5 LYBT.Shared.Models（A 55 / B 4 / D 6）

- **A 级（55）**：`ApiResponse.CreateSuccess`（28 处）/`CreateFail`（35 处）；`PagedResult` 构造（32 处 `new PagedResult<`）；`Result` 族 19（`FromException` 被 `SystemConfigurationService.cs:89,116` 用）；`ErrorCodeExtensions.ToHttpStatusCode`（`ControllerBaseExtensions.cs:95,131`、`AuthController.cs:56,75`、`AppException.cs:36`）/`ToCategory`（`AppException.cs:42`）/`ToFormattedString`（38 处）；`ErrorMessages.GetUserMessage`（`ClientErrorMessageMapper.cs:111`）；`CacheExtensions.RemoveByPrefix`（`DesktopCacheManager.cs:37,49`、`CacheInvalidationService.cs:35`）；`DtoConversionExtensions.ToInputDto`（12 处 Desktop 调用）；`MedicalCaseBusinessRules` 4 方法（`MedicalCaseServiceHelper.cs:87-97`、`MedicalCaseStateService.cs:74`）；`PinYinHelper.GetPinYinCode`（18 处）；`PasswordHelper.GenerateSecurePassword`×4（`ResetPasswordCommandHandler.cs:32`）；`SensitiveDataAttribute`（9 处）；8 个 DTO 验证器构造（DI 反射实例化）。
- **B 级（4）**：`GetAllCacheKeys`、`GetRandomInt/Shuffle`、`GetPinYinCodeFallback`。
- **D 级（6）**：`Result.ValidationFailure`、`ErrorCodeExtensions.GetModuleName`、`PasswordHelper.GenerateTemporaryPassword/GenerateSalt/ValidatePassword/SecureEquals`（后 4 者仅测试引用，见 §6）。

---

## 2. 日志集中定义专项：现状分布图 + 整合方案 + 迁移清单

### 2.1 现状分布图（已符号级核实）

```
                          ┌───────────────────── 日志机制散落 4 项目 ─────────────────────┐
                          │                                                                 │
   LYBT.Shared.Logging(8文件)     Desktop.Infrastructure        WebAPI                     Desktop.Shell / Foundation
   ├ Abstractions/                  ├ Logging/DesktopSerilogConfiguration.cs ── 路径/Lazy Provider/Initialize(88行)
   │   ActivityCorrelationIdProvider  └ 被 App.xaml.cs:48 调用, :75 CloseAndFlush            └ Shell/Extensions/LoggingRegistrationExtensions.cs
   │   (Activity.Current.TraceId 机制)                                                          RegisterLogging(ILoggerFactory) 被 ServiceCollectionExtensions.cs:49 调用
   ├ Enrichers/CorrelationIdEnricher   Serilog 包×6                 WebAPI/Extensions/SerilogMSSqlServerExtensions.cs
   ├ Extensions/LoggerConfigurationExtensions                       (MSSQL sink, 表 SystemLogs, Program.cs:142)      Desktop.Foundation/Http/LoggingHttpHandler.cs
   │   UseSharedLogging(仅Desktop用)      Serilog 包×1               WebAPI/Middleware/CorrelationIdMiddleware.cs     (DelegatingHandler, UnifiedApiClientExtensions.cs:85-89 手写链)
   │   WithSensitiveDataMasking(仅Server用)                          WebAPI/Filters/ApiLoggingFilter.cs               (全局 Filter, ServiceCollectionExtensions.cs:151)
   ├ Management/LoggingLevelManager       Serilog 包×6               WebAPI 侧 Serilog 包×6（含 Sinks.MSSqlServer）
   └ Masking/SensitiveDataMasker*
```

**关键事实（证据）**：

| # | 事实 | 证据 |
|---|------|------|
| 1 | **ICorrelationIdProvider 全仓无任何 DI 注册**（双端均不注册）；Server 靠中间件 `LogContext.PushProperty`，Desktop 靠静态 `Lazy` 单例 + Activity | `CorrelationIdMiddleware.cs:72`、`DesktopSerilogConfiguration.cs:33-39` |
| 2 | **CorrelationId 双机制分叉**：Server=`HttpContext.TraceIdentifier`（中间件 + ~9 处直接读）；Desktop=`Activity.Current.TraceId`（Provider:20 + LoggingHttpHandler:28） | `CorrelationIdMiddleware.cs:59`、`ActivityCorrelationIdProvider.cs:20`、`LoggingHttpHandler.cs:28`；直接读 TraceIdentifier 者：`BusinessExceptionHandler.cs:58,79,80`、`SystemExceptionHandler.cs:57,67,74`、`BaseApiController.cs:58`、`ControllerBaseExtensions.cs:13`、`ProblemDetailsConfiguration.cs:31`、`ServiceCollectionExtensions.cs:203`、`UnifiedMiddlewareConfiguration.cs:57,70,71`、`ApiLoggingFilter.cs:25` |
| 3 | **Serilog 初始化双入口无共用**：WebAPI `Program.cs:75-89/126-145` 内联构建（ReadFrom.Configuration + WithSensitiveDataMasking + MSSQL sink）；Desktop `App.xaml.cs:48` 走 DesktopSerilogConfiguration → `UseSharedLogging`（`DesktopSerilogConfiguration.cs:57`） | `Program.cs:129,135,142` |
| 4 | **Serilog 包被 4 项目引用**（版本经 Directory.Packages.props 统一，但职责未统一） | `Shared.Logging.csproj:15-20`、`Infrastructure.csproj:37`、`WebAPI.csproj:56-61`、`Desktop.Infrastructure.csproj:49-54` |
| 5 | **MSSQL sink 配置仅 WebAPI 独有**，且 JSON 与代码冲突：`appsettings.Production.json:73` `autoCreateSqlTable:true` vs 代码 `SerilogMSSqlServerExtensions.cs:43` `false`（备注 L14-20 声明代码优先） | `Program.cs:142`、`SerilogMSSqlServerExtensions.cs:42-43` |
| 6 | **LoggingHttpHandler 非 DI 注册**：手写 `new HttpClient(loggingHandler)` 链（auth→tokenRefresh→logging） | `UnifiedApiClientExtensions.cs:85-89,103-111` |
| 7 | **脱敏双实现并存**：Shared.Logging `SensitiveDataMasker`（日志脱敏）+ Infrastructure `SensitiveDataJsonConverterFactory/Converter`（JSON 序列化脱敏，`SensitiveDataJsonConverterFactory.cs:58,68`），共用 `[SensitiveData]` 属性但各自实现 | `SensitiveDataMasker.cs:132`、`SensitiveDataJsonConverterFactory.cs:141` |
| 8 | **LocalWebAPI 无 Serilog 初始化**：仅注册 `LoggingLevelManager`（`LocalWebApiProgram.cs:63`），本地模式日志走 Desktop 静态 Log.Logger | — |

### 2.2 审查问题回答

**Q1 日志的配置/初始化/CorrelationId/脱敏/MSSQL sink/HTTP 拦截是否应收敛回 Shared.Logging？**
→ **是**。证据：① 双端初始化入口各自实现（§2.1 事实 3）导致 Server 不消费 `UseSharedLogging` 的模板/Enricher 链，Server 与 Desktop 的 CorrelationId 机制完全不同（事实 1/2），脱敏配置也要各自接线（事实 7）；② `LoggingHttpHandler`/`ApiLoggingFilter`/`CorrelationIdMiddleware` 三处 HTTP 日志拦截各自实现且不互通；③ 收敛后 4 项目无需再各自引用 Serilog（仅 Shared.Logging 持有），消除包引用分散（事实 4）。

**Q2 Shared.Logging 应否升级为独立完整日志项目（对外只暴露 AddLybtLogging + ILogger 工厂，全程接管双端日志）？**
→ **是**，可行且收敛收益最大。前置条件：Shared.Logging 需补充对 ASP.NET Core 的依赖（`Microsoft.AspNetCore.Http.Abstractions` 等，用于 Middleware/Filter 扩展）——需走技术引入治理流程（先改权威文档，现有 `P05b_SharedUtilities_Should_Not_Depend_On_AspNetCore` 架构测试需同步更新豁免清单）。统一入口：
- Server：`builder.AddLybtLogging(configuration, options => options.UseMssqlSink = true)`（替代 Program.cs 内联构建）；
- Desktop：`DesktopSerilogConfiguration.Initialize()` 内部全部收敛进 `AddLybtLogging`（文件 sink + CorrelationId + 脱敏）。
- MSSQL sink 为 **Server-only 能力**，用 options 开关控制，Shared.Logging 持有 `Serilog.Sinks.MSSqlServer` 包（或保持 WebAPI 侧引用——二选一，推荐前者以彻底收敛）。

**Q3 收敛后各项目 Serilog 扩展删还是留薄壳？**
→ 全部**删除**（不留薄壳，遵守"禁止兼容层"规则）：

| 现有文件 | 决策 | 去向 |
|----------|------|------|
| `Desktop.Infrastructure/Logging/DesktopSerilogConfiguration.cs` | **删** | 路径/文件 sink/模板/Lazy Provider 逻辑迁入 Shared.Logging `DesktopLoggingOptions` + `AddLybtLogging`；`App.xaml.cs:48,75` 改调 `LoggingBootstrap.Initialize/CloseAndFlush` |
| `WebAPI/Extensions/SerilogMSSqlServerExtensions.cs` | **删** | 迁入 Shared.Logging `MssqlSinkConfiguration`，`Program.cs:142` 改为 options 开关 |
| `Shell/Extensions/LoggingRegistrationExtensions.cs` | **删** | `RegisterLogging`（ILoggerFactory 单例 + `ILogger<>`）由 `AddLybtLogging` 的 DI 注册接管；`ServiceCollectionExtensions.cs:49` 改调统一入口 |
| `Desktop.Foundation/Http/LoggingHttpHandler.cs` | **删（迁移）** | 迁入 Shared.Logging `Http/LoggingHttpHandler`，注册扩展 `AddLybtHttpLogging(HttpClient)` 由 Shared.Logging 提供，`UnifiedApiClientExtensions.cs:85-89` 改用它 |

**Q4 CorrelationId 是否应单点化？**
→ **是**。单点方案：`ICorrelationIdProvider` 由 Shared.Logging 定义 + `ActivityCorrelationIdProvider` 实现 + **统一 DI 注册**（`AddLybtLogging` 内 `AddSingleton<ICorrelationIdProvider>`）；中间件/Handler 统一由 Shared.Logging 提供注册扩展：
- Server：`UseLybtCorrelationId()`（收敛 `CorrelationIdMiddleware` 现有逻辑：traceparent→X-Correlation-ID→短 GUID→TraceIdentifier/LogContext，`CorrelationIdMiddleware.cs:43-72`）；
- Desktop：`LoggingHttpHandler` 改用 `ICorrelationIdProvider.GetCorrelationId()`（替换直接读 `activity.Id`，`LoggingHttpHandler.cs:28`）；
- 所有直接读 `TraceIdentifier` 的 ~9 处（事实 2）收敛为经 Provider/`GetCorrelationId(HttpContext)` 静态辅助（该辅助已在 `BusinessExceptionHandler.cs:70,182` 存在，可上移为共享扩展）。

**Q5 日志整合方案：目标结构 + 迁移清单**

目标结构（Shared.Logging 升级后）：

```
LYBT.Shared.Logging/
├── Bootstrap/
│   ├── LybtLoggingOptions.cs          # 文件路径/MSSQL开关/模板/级别（合并 DesktopSerilogConfiguration + SerilogMSSqlServerExtensions 配置项）
│   ├── LoggingBootstrap.cs            # AddLybtLogging(IServiceCollection/IHostBuilder) + Initialize/CloseAndFlush 静态入口
│   └── ServiceCollectionExtensions.cs # AddLybtLogging 注册：ICorrelationIdProvider、LoggingLevelManager、ILoggerFactory、ILogger<>
├── Correlation/
│   ├── ICorrelationIdProvider.cs      # 现有（补充 DI 注册）
│   ├── ActivityCorrelationIdProvider.cs # 现有
│   └── CorrelationIdEnricher.cs       # 现有（含 WithCorrelationId）
├── Http/
│   ├── LoggingHttpHandler.cs          # ← Desktop.Foundation/Http（改用 Provider）
│   ├── CorrelationIdMiddleware.cs     # ← WebAPI/Middleware（+ UseLybtCorrelationId 扩展）
│   └── ApiLoggingFilter.cs            # ← WebAPI/Filters（+ AddLybtApiLoggingFilter 扩展）
├── Sinks/
│   └── MssqlSinkConfiguration.cs      # ← WebAPI/Extensions/SerilogMSSqlServerExtensions（Server-only options）
├── Management/                        # 现有 LoggingLevelManager/DebugModeInfo
├── Masking/                           # 现有 SensitiveDataMasker/Policy（+ SanitizeException 待删见 §6）
└── Extensions/LoggerConfigurationExtensions.cs # 现有 UseSharedLogging/WithSensitiveDataMasking
```

迁移清单（源→目标）：

| # | 源 | 目标 | 动作 |
|---|----|------|------|
| M1 | `Desktop.Infrastructure/Logging/DesktopSerilogConfiguration.cs` | `Bootstrap/LybtLoggingOptions` + `LoggingBootstrap` | 迁移+改造 |
| M2 | `WebAPI/Extensions/SerilogMSSqlServerExtensions.cs` | `Sinks/MssqlSinkConfiguration` | 迁移+options 化 |
| M3 | `Shell/Extensions/LoggingRegistrationExtensions.cs` | `Bootstrap/ServiceCollectionExtensions` | 迁移+合并 |
| M4 | `Desktop.Foundation/Http/LoggingHttpHandler.cs` | `Http/LoggingHttpHandler` | 迁移+Provider 化 |
| M5 | `WebAPI/Middleware/CorrelationIdMiddleware.cs` | `Http/CorrelationIdMiddleware` + 扩展 | 迁移+单点注册 |
| M6 | `WebAPI/Filters/ApiLoggingFilter.cs` | `Http/ApiLoggingFilter` + 扩展 | 迁移+单点注册 |
| M7 | `WebAPI/Program.cs:75-145` 内联 Serilog 构建 | `Bootstrap` 统一入口 | 重写调用 |
| M8 | `App.xaml.cs:48,75` + `ServiceCollectionExtensions.cs:49` | `LoggingBootstrap.Initialize/CloseAndFlush/AddLybtLogging` | 改调用 |
| M9 | `SensitiveDataJsonConverterFactory`（Infrastructure/Serialization） | 复用 `SensitiveDataMasker.Mask`（`SensitiveDataJsonConverterFactory.cs:141` 已引用，去重内部实现） | 收敛 |
| M10 | Serilog 包从 4 项目移除 | 仅 Shared.Logging 持有 | 删引用 |

保留/删除决策：M1-M6 源文件**删除**；M7-M8 调用点**改写**；M9 去重；`LoggingLevelManager`、脱敏核心、Enricher、`UseSharedLogging`/`WithSensitiveDataMasking` 保留原地（已属 Shared.Logging）。

---

## 3. 异常统一设计专项：现状分布图 + 统一方案 + 迁移清单

### 3.1 现状分布图（已符号级核实）

```
 异常层次（已集中）          处理器（分裂 3 处）                   错误码（Shared.Models）
 LYBT.Shared.ExceptionHandling  ┌ Server  Infrastructure/ExceptionHandling/    ErrorCode.cs (117 值, 682 行)
  7 文件 / 61 方法               │   SystemExceptionHandler.cs (192 行, IExceptionHandler, 无类型过滤恒兜底)    ├ ErrorCodeExtensions.ToHttpStatusCode :12-139
  AppException (GetHttpStatusCode:35) │   BusinessExceptionHandler.cs (82 行, IExceptionHandler, is AppException) │  .ToCategory :144 / .GetModuleName :286 / .ToFormattedString :307
  ├ BusinessException 400    ├ 注册: ApiServiceCollectionExtensions.cs:73-74 (Business 先)   ├ ErrorMessages.GetUserMessage :152 (117 条目)
  ├ ConflictException 409 ⚠整类死 │  ├ 中间件: UnifiedMiddlewareConfiguration.cs:24 UseExceptionHandler 第 1 位     └ ErrorCategory/ErrorSeverity
  ├ NotFoundException 404    │  └ 无第三 handler
  ├ ValidationException 400 ⚠整类死 ├ Desktop  Desktop.Infrastructure/ExceptionHandling/
  ├ ApiException =StatusCode ⚠整类死 │   DesktopExceptionHandler.cs (242 行, IDesktopExceptionHandler)
  └ UnauthorizedException 401 ⚠整类死 │   ├ 挂接: AppDomain.UnhandledException:82 + TaskScheduler.UnobservedTaskException:83
                             │   │  注册链: App.xaml.cs:128 → AppStartupOrchestrator → ErrorHandlingStartupStep.cs:42
                             │   └ GetUserFriendlyMessage:51 → 委托 ClientErrorMessageMapper
                             │   + Desktop.Foundation/ExceptionHandling/ClientErrorMessageMapper.cs (410 行)
                             │      GetUserFriendlyMessage switch:132-152 / 追踪码:365-404 / 本地映射表:27-39,66-76
                             └ 错误码映射: ErrorCode→HTTP 三处独立实现源（见 §3.2-Q4）
```

### 3.2 审查问题回答

**Q1 异常处理器是否应收敛到 Shared.ExceptionHandling（层次+处理器+注册扩展完整职责）？**
→ **是，按职责拆分收敛**：
- Server `SystemExceptionHandler` + `BusinessExceptionHandler`（Infrastructure/ExceptionHandling/）→ **迁入 Shared.ExceptionHandling**（两者仅依赖 `IExceptionHandler` 接口，`BusinessExceptionHandler` 依赖 Shared.ExceptionHandling 类型、`SystemExceptionHandler` 依赖 Shared.Models 的 ApiResponse——均无 Infrastructure 特有依赖；`GetCorrelationId` 私有方法 `System:182, Business:70` 上移为共享静态辅助）。注册扩展 `AddLybtExceptionHandlers()` 由 Shared.ExceptionHandling 提供（替代 `ApiServiceCollectionExtensions.cs:73-74` 的两行直登）。
- Desktop `DesktopExceptionHandler`：**挂接逻辑保留 Desktop**（AppDomain/TaskScheduler 是 WPF 运行时机制，不属于共享层），但**消息映射委托** `GetUserFriendlyMessage`（`DesktopExceptionHandler.cs:51-53`）与 `ClientErrorMessageMapper` 收敛为 Shared 统一映射器（见 Q3/Q4）。
- 约束提示：Shared.ExceptionHandling 引用 `IExceptionHandler`（`Microsoft.AspNetCore.Http` 相关）需补 AspNetCore 依赖，与 §2.2-Q2 同走技术引入治理；或处理器放 Server 侧但**映射规则以共享类型单点定义**（见 Q4）。

**Q2 错误码枚举归属？**
→ **保留在 Shared.Models，不迁入 ExceptionHandling**，并固化归属规则：
- 依赖方向已定：`ExceptionHandling → Models`（`LYBT.Shared.ExceptionHandling.csproj` 唯一 ProjectReference），ErrorCode 是**跨层契约**（Controller/Result/错误消息都用），迁入 ExceptionHandling 将迫使 Models 反向依赖 ExceptionHandling（环）。
- 规则：**错误码属共享契约层（Models/Primitives/ErrorCodes），异常层只消费**；`ErrorCodeExtensions.ToHttpStatusCode`（枚举→HTTP）属"异常→HTTP 映射"域，**标注为映射 SSOT 候选**（见 Q4）。

**Q3 Desktop/Server 异常处理是否应统一设计（同一映射策略）？**
→ **是**。现状 Desktop 仅"状态→消息"反向表（`ClientErrorMessageMapper.cs:44-49,66-76`）且已部分委托 `ErrorMessages`（`:111`，注释"消除原 274 条重复映射"）；Server 是"异常→状态→ProblemDetails/ApiResponse"。统一策略：**异常 → ErrorCode → HTTP 状态码 → 用户消息** 一条链，两端共用：
- 消息：`ErrorMessages.GetUserMessage(ErrorCode)` 为唯一消息源，`ClientErrorMessageMapper` 本地表（`HttpStatusMessages:27-39`、`ErrorCodePrefixMessages:66-76`）删除，全部委托 Shared 映射器；
- Desktop 特例保留：Refit.ApiException（按类型名识别，`ClientErrorMessageMapper.cs:149`）→ 解析服务器 `ApiResponse` 内容的逻辑可上移共享（Server 端 ApiResponse 结构一致）。

**Q4 异常→HTTP 映射 SSOT 位置？注册方式？**
→ **现状三处独立实现源，实质分叉**（符号级确认）：
1. `ErrorCodeExtensions.ToHttpStatusCode`（`ErrorCodeExtensions.cs:12-139`，枚举→HTTP，覆盖 28+9+12+11+3+22+1+2 码）；
2. 6 个子类 `GetHttpStatusCode` **硬编码常量**（`BusinessException.cs:17`=400、`ConflictException.cs:50`=409、`NotFoundException.cs:22`=404、`ValidationException.cs:27`=400、`UnauthorizedException.cs:22`=401、`ApiException.cs:33`=StatusCode），**绕过 ErrorCode 映射**——例：以 400 类错误码构造的 ConflictException 仍返回 409（`AppException.cs:36` 走 TypedErrorCode，子类 override 后失效）；
3. `SystemExceptionHandler.GetExceptionInfo` 独立硬编码 switch（`SystemExceptionHandler.cs:86-180`，FluentValidation→400、DbUpdateConcurrency→409 等 11 分支）。

→ **SSOT 设计**：
- **枚举→HTTP**：`ErrorCodeExtensions.ToHttpStatusCode` 为唯一权威（98 个显式 case + default 500，已完备）；
- **子类硬编码常量 → 删除**：6 个子类的 `GetHttpStatusCode` 改为仅定义 `TypedErrorCode` 默认值（`BusinessException` 已有 `EC.Unknown` 模式），状态码全部由 `AppException.GetHttpStatusCode()` 经 `TypedErrorCode.ToHttpStatusCode()` 推导（`AppException.cs:35-36` 已实现该机制，仅需子类不再 override——当前子类 override 使 422/429 等映射被完全忽略）；
- **系统异常→HTTP**：`SystemExceptionHandler.GetExceptionInfo` 保留为**第二权威**（系统异常无 ErrorCode），但在文档标注为"仅系统异常映射表"；
- **Controller 层特判**：`ControllerBaseExtensions` 的 `BusinessFail` 硬编码 422（`:62`）与 auth 映射 switch（`:100-109`）与枚举映射同源但含特判，统一后改经 `ToHttpStatusCode`（422 场景用对应 ErrorCode）；
- **注册方式**：`AddExceptionHandler` 扩展由 Shared.ExceptionHandling 提供（`AddLybtExceptionHandlers()`，Business 先 System 后，顺序保留 `ApiServiceCollectionExtensions.cs:73-74` 语义）；
- ⚠️ **遗留缺口**：`ProblemDetailsConfiguration.cs:17-50` 的 ProblemDetails 管线与两个 handler 并行注册，但 handler 路径（`UnifiedMiddlewareConfiguration.cs:37-46` 手写 `GetServices<IExceptionHandler>` 迭代）**不经过 ProblemDetails 管线**——统一时二选一（保留 ApiResponse 信封则移除 ProblemDetails 注册，避免双输出源）。

**Q5 异常统一方案：目标结构 + 迁移清单**

目标结构：

```
LYBT.Shared.ExceptionHandling/
├── Exceptions/                 # 现有 7 文件（子类 GetHttpStatusCode 改默认 TypedErrorCode）
├── Handlers/
│   ├── BusinessExceptionHandler.cs   # ← Infrastructure/ExceptionHandling（改造：GetCorrelationId 用共享辅助）
│   ├── SystemExceptionHandler.cs     # ← Infrastructure/ExceptionHandling（映射表保留）
│   └── IExceptionHandlerExtensions.cs # AddLybtExceptionHandlers()
├── Mapping/
│   └── ExceptionMessageMapper.cs     # ← 收敛 ClientErrorMessageMapper 映射逻辑 + DesktopExceptionHandler 委托
├── Abstractions/
│   ├── IExceptionMessageMapper.cs
│   └── CorrelationIdHelper.cs        # ← 上移 System/Business handler 的 GetCorrelationId（:70,:182）
└── ErrorCodes.md / 归属规则注释     # 注明 ErrorCode 属 Models，映射 SSOT 位置
```

迁移清单：

| # | 源 | 目标 | 动作 |
|---|----|------|------|
| E1 | `Infrastructure/ExceptionHandling/SystemExceptionHandler.cs` | `Handlers/SystemExceptionHandler` | 迁移+GetCorrelationId 上移 |
| E2 | `Infrastructure/ExceptionHandling/BusinessExceptionHandler.cs` | `Handlers/BusinessExceptionHandler` | 迁移 |
| E3 | `ApiServiceCollectionExtensions.cs:73-74` | `AddLybtExceptionHandlers()` | 改调用 |
| E4 | `Desktop.Foundation/ExceptionHandling/ClientErrorMessageMapper.cs` | `Mapping/ExceptionMessageMapper`（映射表部分）；本地表（:27-39,66-76）删除 | 迁移+去重 |
| E5 | `DesktopExceptionHandler.cs:51-53` 委托 | 改用共享 `IExceptionMessageMapper` | 改调用（挂接保留） |
| E6 | 6 子类 `GetHttpStatusCode` override | 删除，改默认 `TypedErrorCode` | 改造 |
| E7 | `SystemExceptionHandler.cs:86-180` 映射表 | 保留原地（第二权威）并文档标注 | 保留 |

保留/删除决策：E1-E2 源**删除**；E3/E5 改调用；E4 映射表**删除**；E6 改造；`DesktopExceptionHandler` 挂接部分（AppDomain/TaskScheduler 钩子）**保留** Desktop。

---

## 4. 其他可集中机制（配置/映射/验证/常量/工具）识别

### 4.1 配置 Options

| 发现 | 证据 | 判定 |
|------|------|------|
| Shared 版 `AddLybtClientConfiguration` **生产零调用**（死代码），Desktop 走 Shell 同名重载，两版注册集合不一致 | `ClientConfigurationExtensions.cs:18`（仅测试）；`PrismConfigurationExtensions.cs:21`（+`ServiceCollectionExtensions.cs:74`）；差异见 `PrismConfigurationExtensions.cs:44,47` | **C/D**：删 Shared 版或统一为单入口 |
| `JwtOptions` 与 `LocalJwtOptions` 同 `SectionName="Jwt"` + SecretKey/Issuer/Audience 字段重复，仅过期差异 | `Options/Common/JwtOptions.cs:10` vs `Options/Server/LocalJwtOptions.cs:11` | **C**：合并为 JwtOptions + 派生/属性 |
| Desktop Foundation `RetryPolicyOptions` 与 Shared `DatabaseOptions.cs:65` 同名不同字段 | `Http/RetryPolicyExtensions.cs:127` vs `DatabaseOptions.cs:65` | **C**：合并进 Shared |
| WebAPI 自建 `JsonOptions.cs:6`（别名 LybtJsonOptions 手动 Bind）未归 Shared | `WebAPI/Configuration/JsonOptions.cs:6`、`ServiceCollectionExtensions.cs:19,129-130` | **C**：迁入 Shared |
| LocalJwtOptions 校验链三处登记、机制不一（Server 注册验证器但无绑定链；Desktop Bind+ValidateDataAnnotations；Prism 裸 Bind） | `ServerConfigurationExtensions.cs:26`、`LocalWebApiProgram.cs:103-105`、`PrismConfigurationExtensions.cs:47` | **C/E**：统一校验链 |

### 4.2 Mapperly 映射

| 发现 | 证据 |
|------|------|
| 全仓 13 个 Mapperly `[Mapper]`（8 Server + 5 Desktop），**Shared 内无 Mapperly** | Server：AuthUserMapper.cs:10、UserMapper.cs:11、UserCrossModuleMapper.cs:10、RegistrationMapper.cs:10、PatientMapper.cs:11、HerbMapper.cs:11、FormulaMapper.cs:11、MedicalCaseMapper.cs:21；Desktop：PrescriptionMapper.cs:23、MedicalCaseDetailModelMapper.cs:29、MedicalCaseCloneMapper.cs:20、ConsultationMapper.cs:23、FormulaDetailModelMapper.cs:26 |
| Shared 手写 `DtoConversionExtensions`（ToInputDto×2/ToPrescriptionInputDto，`DtoConversionExtensions.cs:20,40,59`）与 Desktop 模块内手写 `ToInputDto`（`ConsultationMapper.cs:124`、`PrescriptionMapper.cs:154`、`MedicalCaseDetailModelMapper.cs:157`）**两套转换机制并存** | 同上 |
| 无集中映射注册点（各模块自持） | — |

→ **C**：DTO↔DTO 转换统一为 Mapperly（Shared 内可建 `SharedDtoMapper`），删除手写 `DtoConversionExtensions` 与 Desktop 模块内手写 `ToInputDto`（A07 架构测试已强制 Mapperly）。

### 4.3 FluentValidation 验证器

| 发现 | 证据 |
|------|------|
| 30 个验证器：Shared.Models 8 + Server 模块 22；**模块命令验证器与 Shared DTO 验证器字段级重复**：`CreatePatientValidator.cs:13-23` vs `PatientInputDtoValidator.cs:16-41`（模块版 IdNumber 仅 MaxLength 18，Shared 版必填+18 位正则，**规则更严的版本反而不生效**——先验的模块版可能吞错误） | 例证；双注册：`PatientsModule.cs:48+58`、`HerbsModule.cs:50+60`、`FormulaModule.cs:27+60` |
| ValidationBehavior 单定义（`Infrastructure/Validation/ValidationBehavior.cs:10`），AddOpenBehavior 注册 7 处 + Desktop `LocalWebApiProgram.cs:82` 复用 | 无重复实现 ✓ |
| AddValidatorsFromAssembly 10 处逐模块扫描，装配点不统一 | `AuthModule.cs:58` 等 8 文件 |

→ **C/E**：删除模块内与 Shared DTO 验证器重复字段的验证器（命令验证器仅保留命令级校验）；装配点收敛为 Shared 提供 `AddLybtValidation()` 扩展。

### 4.4 常量 SSOT

| 常量 | 现状 | 证据 | 判定 |
|------|------|------|------|
| ErrorCode | ✅ 单一（117 值） | `Shared.Models/Primitives/ErrorCodes/ErrorCode.cs:17` | SSOT 合规 |
| PolicyConstants | 常量单一，**策略注册双写且不一致**：Server 6 策略 vs Desktop LocalJwtConfig 5 策略（**缺 `DoctorOrAdminOrReceptionist`**，而 `LocalWebAPI/Controllers/PatientsController.cs:20` 引用该策略 → 本地模式该端点授权可能异常） | `Infrastructure/Constants/PolicyConstants.cs:3`；`AuthenticationServiceCollectionExtensions.cs:112-134` vs `LocalJwtConfig.cs:70-89` | **C**（双控制器树分叉，需按 AGENTS 规则同步） |
| RoleConstants | ✅ 单一 | `RoleConstants.cs:6` | 合规（sysadmin 字符串两处小写并存：`UserConstants.cs:11` vs `RoleConstants.cs:16`，**C**） |
| 路由前缀 | ❌ **无 SSOT，245 处字面量，版本不一致**：Server `api/v{version:apiVersion}`（`MedicalCasesController.cs:22` 等）+ `ApiVersionConstants.cs:8`；LocalWebAPI `api/v1/[controller]`（`PatientsController.cs:19` 等）；Refit 契约硬编码 `/api/v1/...`（`IMedicalCaseApi.cs:16`、`IUserApi.cs:16` 等） | 同上 | **C/E**：建 `RouteConstants`，双控制器树 + 契约统一引用 |

### 4.5 Utilities 跨项目重复

| 发现 | 证据 | 判定 |
|------|------|------|
| 密码强度评分**双实现**：`PasswordHelper.CheckPasswordStrength`（`Shared.Models/Utilities/Security/PasswordHelper.cs:159`）vs `PasswordPolicyValidator.GetStrengthLevel/CalculateStrength`（同目录 `PasswordPolicyValidator.cs:165,213`）；弱密码表重复（`PasswordHelper.cs:26-33` vs `PasswordPolicyValidator.cs:232-240`） | 同目录两文件 | **C**：归并到 PasswordHelper（或 Policy）单点 |
| `MaskIdNumber` 5 处实现：权威 `PrivacyHelper.cs:12`（前6后4）+ 3 薄包装（`CardReaderViewModel.cs:401`、`PatientCardReaderViewModel.cs:101`、`ReceptionistHomeViewModel.cs:293`）+ **1 处独立实现且格式分叉**（`HuaDaHD100CardReader.cs:286` 前4后4，脱敏结果不一致） | 同上 | **C**：统一走 PrivacyHelper |
| PasswordHelper 367 行自身 TODO「超大类型，建议拆分」（`PasswordHelper.cs` 头部 + `README:13`） | — | 拆分（与 §6 D 级清理联动） |
| StringHelper/DateTimeHelper/EnumHelper/UrlHelper：全仓**不存在**，无散落 | 0 匹配 | ✅ |
| PinYinHelper 单定义双端共用（18 处调用） | `PinYinHelper.cs:13` | ✅ |

---

## 5. C 级重复清单 + E 级可集中清单

### 5.1 C 级重复清单（11 组，跨项目/Shared 内部）

| # | 重复内容 | Shared 侧锚点 | 重复侧证据 | 收敛方向 |
|---|----------|---------------|-----------|----------|
| C1 | 密码强度评分 + 弱密码表双实现 | `PasswordHelper.cs:159,26-33` | `PasswordPolicyValidator.cs:165,213,232-240`（同目录） | 归并单点 |
| C2 | 状态码/错误前缀→消息本地表 | `ErrorMessages.cs:152`（117 条目权威） | `ClientErrorMessageMapper.cs:27-39,66-76` | 全量委托 ErrorMessages |
| C3 | DTO 转换两套机制（手写 vs Mapperly） | `DtoConversionExtensions.cs:11`（手写） | Desktop 3 个 Mapper 内手写 `ToInputDto`（`ConsultationMapper.cs:124` 等） | 统一 Mapperly `SharedDtoMapper` |
| C4 | 模块命令验证器与 Shared DTO 验证器字段级重复 + 双注册 | `PatientInputDtoValidator.cs:16-41` 等 8 个 | `CreatePatientValidator.cs:13-23`、`CreateHerbValidator.cs:13-58`、`CreateFormulaValidator.cs:16-42`；双注册 `PatientsModule.cs:48+58` 等 | 删模块重复字段验证器 |
| C5 | JwtOptions/LocalJwtOptions 同节重复 | `Options/Common/JwtOptions.cs:10` | `Options/Server/LocalJwtOptions.cs:11` | 合并 |
| C6 | RetryPolicyOptions 同名双定义 | `DatabaseOptions.cs:65` | `RetryPolicyExtensions.cs:127`（字段不同） | 合并进 Shared |
| C7 | 策略注册双写且不一致（缺 DoctorOrAdminOrReceptionist） | `PolicyConstants.cs:3` | `AuthenticationServiceCollectionExtensions.cs:112-134` vs `LocalJwtConfig.cs:70-89` | 统一注册扩展（双控制器树同步） |
| C8 | 路由前缀 245 处字面量、版本不一致 | （无） | Server `api/v{version}` vs LocalWebAPI `api/v1/[controller]` vs Refit 硬编码 | 建 `RouteConstants` SSOT |
| C9 | MaskIdNumber 5 实现 + 格式分叉 | （无） | `PrivacyHelper.cs:12` vs `HuaDaHD100CardReader.cs:286`（前4后4） | 统一 PrivacyHelper |
| C10 | sysadmin 小写字符串双常量 | `UserConstants.cs:11` | `RoleConstants.cs:16` | 合并 |
| C11 | JSON 序列化脱敏双实现 | `SensitiveDataMasker.cs:132` | `SensitiveDataJsonConverterFactory.cs:58-141`（Infrastructure） | 复用 Masker |

### 5.2 E 级可集中清单（机制文件/机制点）

| # | 机制 | 现状位置 | 集中到 | 详见 |
|---|------|----------|--------|------|
| E1 | Desktop 日志初始化 | `Desktop.Infrastructure/Logging/DesktopSerilogConfiguration.cs` | Shared.Logging | §2.2-Q5 M1 |
| E2 | MSSQL sink 配置 | `WebAPI/Extensions/SerilogMSSqlServerExtensions.cs` | Shared.Logging | §2.2-Q5 M2 |
| E3 | ILoggerFactory 注册 | `Shell/Extensions/LoggingRegistrationExtensions.cs` | Shared.Logging | §2.2-Q5 M3 |
| E4 | HTTP 日志拦截 | `Desktop.Foundation/Http/LoggingHttpHandler.cs` | Shared.Logging | §2.2-Q5 M4 |
| E5 | CorrelationId 中间件 + ~9 处 TraceIdentifier 直读 | `WebAPI/Middleware/CorrelationIdMiddleware.cs` + 散落点 | Shared.Logging（Provider 单点） | §2.2-Q4 |
| E6 | API 日志过滤器 | `WebAPI/Filters/ApiLoggingFilter.cs` | Shared.Logging | §2.2-Q5 M6 |
| E7 | Server 异常处理器 | `Infrastructure/ExceptionHandling/System|BusinessExceptionHandler.cs` | Shared.ExceptionHandling | §3.2-Q5 E1-E3 |
| E8 | 异常消息映射 | `Desktop.Foundation/ExceptionHandling/ClientErrorMessageMapper.cs` | Shared.ExceptionHandling Mapping | §3.2-Q5 E4 |
| E9 | 异常→HTTP 映射三源 | `ErrorCodeExtensions.cs:12` + 6 子类 override + `SystemExceptionHandler.cs:86` | 单 SSOT（枚举→HTTP 权威 + 系统异常第二权威） | §3.2-Q4 |
| E10 | 验证器装配 | 10 处 `AddValidatorsFromAssembly` | `AddLybtValidation()` 扩展 | §4.3 |

---

## 6. D 级死方法清单（符号级复核确认，共 56 项）

> 复核方法：全仓 src/ 调用计数 = 定义数 → 除定义外 0 调用；public 方法同时扫 tests/ 标注"仅测试"。
> ⚠️ 全部经符号级复核，**生产代码 0 调用**；删除决策由技术总监审批（含对应单元测试的处理）。

### 6.1 整类死类（4 类，37 方法）——生产代码零引用

| 类 | 方法（行号） | 生产引用 | 备注 |
|----|-------------|---------|------|
| **ConflictException**（8） | `GetHttpStatusCode:50`、构造 :54/:60/:67/:74、工厂 `MedicalCaseVersion:80`、`MedicalCaseLocked:94`、`Duplicate:106` | **0**（仅类内工厂自引用 + tests 7 处，如 `BusinessExceptionTests.cs:217`） | MedicalCase 版本冲突实际用 BusinessException（`MedicalCaseStateService.cs:62,78`）；全类死 |
| **ApiException**（11） | `GetHttpStatusCode:33`、构造 :37/:42/:47/:52/:60、`GetDefaultUserMessage:70`、工厂 `Unauthorized:84`/`Forbidden:87`/`ServiceUnavailable:90`/`Timeout:93` | **0**（`ClientErrorMessageMapper.cs:136,148` 仅为注释/类型名文本，且处理的是 **Refit.ApiException** 按类型名反射） | 本地 ApiException 从未被构造；全类死 |
| **UnauthorizedException**（11） | `GetHttpStatusCode:22`、构造 :26/:32/:39/:46、工厂 `InvalidPassword:53`/`InvalidRefreshToken:56`/`UserDisabled:59`/`UserLocked:62`/`PasswordChangeRequired:65`/`TokenExpired:72` | **0**（tests 14 处） | 登录失败走 Result/ErrorCode 而非该异常；全类死 |
| **ValidationException**（7） | `GetHttpStatusCode:27`、构造 :31/:37/:44/:51/:61、`AddError:76` | **0**（`ValidationBehavior.cs:35` 的 `new ValidationException(failures)` 为 **FluentValidation.ValidationException**——文件 `using FluentValidation`、参数 `List<ValidationFailure>`） | 共享 ValidationException（string/Dictionary 构造）src 内 0 次直接构造；全类死 |

### 6.2 单方法死（19 项）

| 项目 | 类 | 方法（行号） | 证据 |
|------|----|-------------|------|
| ExceptionHandling | NotFoundException | 工厂 `User:57`/`Patient:60`/`Herb:63`/`MedicalCase:66`/`Formula:69`（5） | 0 调用；实际构造为直接 `new`（`MedicalCaseServiceHelper.cs:71,82`） |
| Logging | ActivityCorrelationIdProvider | `SetCorrelationId:24`、`GetCorrelationIdOrNew:33`（2） | 0 调用 |
| Logging | ICorrelationIdProvider | `SetCorrelationId:19`（1） | 0 调用 |
| Logging | LoggerConfigurationExtensions | `WriteToConsoleWithTemplate:69`、`WriteToFileWithTemplate:92`（2） | 0 调用 |
| Logging | SensitiveDataMasker | `MaskObject:142`、`SanitizeException:275`（2） | 仅测试（`SensitiveDataMaskerTests.cs:141-166`） |
| Models | Result | `ValidationFailure:75`（1） | 0 调用 |
| Models | ErrorCodeExtensions | `GetModuleName:286`（1） | 0 调用 |
| Models | PasswordHelper | `GenerateTemporaryPassword:45`、`GenerateSalt:76`、`ValidatePassword:98`、`SecureEquals:318`（4） | 仅测试（`PasswordHelperTests.cs:25-423`）；`GenerateSecurePassword` 8 处生产调用**存活** |
| Configuration | ClientConfigurationExtensions | `AddLybtClientConfiguration:18`（1） | 仅测试（`ConfigurationLoadingTests.cs:86`）；Desktop 用 Shell 同名重载 |

> 已排除的 S0 候选（复核为活）：`AppException` 构造 :44/:48/:52（经子类 `: base()` 链生效）、`LoggingLevelManager` 构造（DI `AddSingleton` `Program.cs:148`/`LocalWebApiProgram.cs:63`）、`PagedResult` 构造（32 处）、8 个 DTO 验证器构造（DI 反射）、`CorrelationIdEnricher.Enrich`/`SensitiveDataDestructuringPolicy.TryDestructure`/`SanitizingJsonConverter`（框架接口/内部使用）、`LoginRateLimitOptions` 构造（`SecurityOptions.cs:36` `new()`）。

---

## 7. 统计汇总

| 维度 | 数值 |
|------|------|
| 审查项目/方法 | 5 项目 / **212 方法**（Entities 39 / Configuration 9 / ExceptionHandling 61 / Logging 38 / Models 65） |
| A 依据充分 | **139**（65.6%） |
| B 支撑性 | **17**（8.0%） |
| D 孤儿/死方法 | **56**（26.4%）—— 含 **4 个整类死类**（37 方法：Conflict/Api/Unauthorized/Validation Exception）+ 19 单方法；其中"仅测试引用" 9 项 |
| 跨项目 C 级重复 | **11 组**（密码强度/消息表/DTO 转换/验证器/Options×3/策略/路由前缀/脱敏身份证/常量×2） |
| E 级可集中机制 | **6 文件 + 5 机制点**（日志 6 文件全收敛回 Shared.Logging；异常 2 handler + 1 映射器；验证器装配；路由常量；策略注册） |
| 日志专项结论 | **应收敛**：Shared.Logging 升级独立完整日志项目，`AddLybtLogging` 单入口，4 项目 Serilog 引用归并，CorrelationId 单点化（10 条迁移 M1-M10） |
| 异常专项结论 | **应统一**：handler 收敛 + 映射 SSOT（枚举→HTTP 唯一权威）+ Desktop/Server 同链策略（7 条迁移 E1-E7） |
| 遗留风险 | ① 4 个死异常类删除需联动 tests（约 21+ 处断言）；② 策略双端不一致（缺 DoctorOrAdminOrReceptionist）可能致本地模式 PatientsController 授权异常，**建议优先修复**；③ Shared.Logging/ExceptionHandling 引入 AspNetCore 依赖需先过技术引入治理 + 更新 `P05b` 架构测试；④ MSSQL sink `autoCreateSqlTable` JSON/代码冲突；⑤ ProblemDetails 管线与 handler 路径并行未互通（双输出源） |

**未 commit**：本报告由技术总监统一提交；后续执行批次（日志收敛 / 异常统一 / C-D 级清理）需另立任务书并经审批。
