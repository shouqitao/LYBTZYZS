# 任务 A-31-C1：日志集中定义（Shared.Logging 升级独立完整日志项目）

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-08
> 依据：`docs/03-architecture/15-solution-integration-plan.md` §3.1（日志专项 A）+ `docs/compose/reports/method-audit-shared-2026-08-08.md` §2（S1 日志专项，M1-M10 迁移清单）
> 用户方针：先收敛再完善｜已授权执行 C 批次

## 任务

将 `LYBT.Shared.Logging` 升级为**独立完整日志项目**——对外只暴露 `AddLybtLogging` 单入口 + `LoggingBootstrap`，**全程接管 Server+Desktop 日志**，消除双机制分叉与配置散落。

## 范围

- ✅ `src/Shared/LYBT.Shared.Logging/`（现有 8 文件 + 新增）
- ✅ 迁移源：`Desktop.Infrastructure/Logging/DesktopSerilogConfiguration.cs`、`WebAPI/Extensions/SerilogMSSqlServerExtensions.cs`、`Shell/Extensions/LoggingRegistrationExtensions.cs`、`Desktop.Foundation/Http/LoggingHttpHandler.cs`、`WebAPI/Middleware/CorrelationIdMiddleware.cs`、`WebAPI/Filters/ApiLoggingFilter.cs`
- ✅ 调用方改造：`WebAPI/Program.cs`（内联 Serilog 构建）、`App.xaml.cs`、`Shell/Extensions/ServiceCollectionExtensions.cs`、`UnifiedApiClientExtensions.cs`
- ✅ `Directory.Packages.props`（Serilog 包引用收敛）
- ❌ 排除：不碰业务模块、不碰异常（C-2 批次）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线（C-0 完成后）

## 目标结构（S1 已定）

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
├── Masking/                           # 现有 SensitiveDataMasker/Policy（SanitizeException 死方法待删）
└── Extensions/LoggerConfigurationExtensions.cs # 现有 UseSharedLogging/WithSensitiveDataMasking
```

## 迁移清单（M1-M10，S1 已定）

| # | 源 | 目标 | 动作 |
|---|----|------|------|
| M1 | `Desktop.Infrastructure/Logging/DesktopSerilogConfiguration.cs` | `Bootstrap/LybtLoggingOptions` + `LoggingBootstrap` | 迁移+改造 |
| M2 | `WebAPI/Extensions/SerilogMSSqlServerExtensions.cs` | `Sinks/MssqlSinkConfiguration` | 迁移+options 化 |
| M3 | `Shell/Extensions/LoggingRegistrationExtensions.cs` | `Bootstrap/ServiceCollectionExtensions` | 迁移+合并 |
| M4 | `Desktop.Foundation/Http/LoggingHttpHandler.cs` | `Http/LoggingHttpHandler` | 迁移+Provider 化 |
| M5 | `WebAPI/Middleware/CorrelationIdMiddleware.cs` | `Http/CorrelationIdMiddleware` + 扩展 | 迁移+单点注册 |
| M6 | `WebAPI/Filters/ApiLoggingFilter.cs` | `Http/ApiLoggingFilter` + 扩展 | 迁移+单点注册 |
| M7 | `WebAPI/Program.cs:75-145` 内联 Serilog 构建 | `Bootstrap` 统一入口 | 重写调用 |
| M8 | `App.xaml.cs:48,75` + `Shell/Extensions/ServiceCollectionExtensions.cs:49` | `LoggingBootstrap.Initialize/CloseAndFlush/AddLybtLogging` | 改调用 |
| M9 | `SensitiveDataJsonConverterFactory`（Infrastructure/Serialization） | 复用 `SensitiveDataMasker.Mask` | 收敛（去重内部实现） |
| M10 | Serilog 包从 4 项目移除 | 仅 Shared.Logging 持有 | 删引用 |

## 关键设计要点

1. **统一 DI 注册**：`AddLybtLogging` 内 `AddSingleton<ICorrelationIdProvider>`（解决 S1 发现的零注册问题）
2. **CorrelationId 单点化**：
   - Server：`UseLybtCorrelationId()` 收敛 `CorrelationIdMiddleware` 现有逻辑（traceparent→X-Correlation-ID→短 GUID→TraceIdentifier/LogContext，`CorrelationIdMiddleware.cs:43-72`）
   - Desktop：`LoggingHttpHandler` 改用 `ICorrelationIdProvider.GetCorrelationId()`（替换直接读 `activity.Id`，`LoggingHttpHandler.cs:28`）
   - 直接读 `TraceIdentifier` 的 ~9 处收敛为经 Provider/共享扩展（`BusinessExceptionHandler.cs:70,182` 已有 `GetCorrelationId(HttpContext)` 辅助可上移共享）
3. **MSSQL sink**：Server-only 能力，options 开关控制（`options.UseMssqlSink = true`）；**解决 JSON 与代码配置冲突**（`appsettings.Production.json:73` autoCreateSqlTable:true vs 代码 :43 false——迁移时统一，代码优先）
4. **不删 `UseSharedLogging`**：Desktop 现有调用链（`DesktopSerilogConfiguration.cs:57`）迁移后走 `LoggingBootstrap`；`LoggerConfigurationExtensions.UseSharedLogging` 保留（模板/Enricher 链仍被 Bootstrap 内部使用）

## 前置（技术引入治理）

Shared.Logging 需补 ASP.NET Core 依赖（`Microsoft.AspNetCore.Http.Abstractions` 等，用于 Middleware/Filter）：
- 蓝图 `14-structure-design-blueprint.md` §0.5 技术栈表标注 Shared.Logging 职责扩展
- 架构测试 `P05b_SharedUtilities_Should_Not_Depend_On_AspNetCore` 同步更新豁免清单（Shared.Logging 例外）
- **先改文档再改代码**（T2 流程）

## 硬性约束

1. **Surgical Changes**：只改日志相关文件
2. **文档先行**：蓝图 §0.5 + 架构测试 P05b 豁免清单先更新
3. **0 错误 0 警告**：`dotnet build LYBTZYZS.sln --no-incremental`
4. **架构测试**：`dotnet test tests/LYBT.Tests.Architecture/` 全绿（P05b 豁免更新后）
5. 单 commit + push：`refactor(logging): A-31-C1 日志集中——Shared.Logging 升级独立完整日志项目（AddLybtLogging 单入口 M1-M10）`
6. 产出报告：`docs/compose/reports/a31-c1-logging-centralization.md`（每项迁移/验证/残留检查）

## 明确不做（防发散）

- ❌ 不碰异常（C-2 批次）
- ❌ 不执行项目合并（C-3/C-4）
- ❌ 不删 `UseSharedLogging`（保留内部使用）
- ❌ 不改业务模块日志调用（ILogger 消费方不动）
