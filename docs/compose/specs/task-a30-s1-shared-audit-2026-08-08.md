# 任务 A-30-S1：Shared 层方法级深审（含日志/异常集中定义专项）

> 派发对象：Mimo Code（审查只读，不改代码）
> 版本：v1.0 | 日期：2026-08-08
> 依据：`docs/compose/plans/2026-08-08-method-audit-plan.md`（S1 阶段）+ S0 基线（`docs/compose/reports/method-audit-baseline-2026-08-08.md` 等）
> 用户方针：先收敛再完善

## 任务

对 **Shared 层 5 项目**进行方法级深度审查（A/B/C/D/E 分级），重点是**日志/异常集中定义专项**——评估这两大机制是否应收敛为独立完整项目。

## 范围

- ✅ `src/Shared/` 全部 5 项目：LYBT.Entities / LYBT.Shared.Models / LYBT.Shared.Configuration / LYBT.Shared.ExceptionHandling / LYBT.Shared.Logging
- ✅ 消费方核实（跨层引用）：Server/Desktop 中引用 Shared 类型的位置（只读核实，不改）
- ❌ 排除：Server/Desktop 自身深审（S2/S3 阶段）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线 `4a956a851` + S0 产出

## 第一部分：Shared 层方法级分级（5 项目逐方法）

沿用 A/B/C/D/E 分级（见方案 §3.1），对 5 项目每个方法判定：
- A 依据充分（设计依据 + 真实调用链）
- B 支撑性（从属 A 类类的辅助方法）
- C **重复/分散**（同职责方法在 Shared 内或与 Server/Desktop 重复）
- D 孤儿/死方法（0 调用）
- E **可集中**（机制性方法应集中到某个 Shared 项目）

产出：5 项目方法分级表（项目/类/方法/级/证据），重点标注 C/E 级。

## 第二部分：日志集中定义专项（核心）

**现状证据（技术总监已核实）**：
- `LYBT.Shared.Logging` 8 文件：Abstractions(ActivityCorrelationIdProvider/ICorrelationIdProvider) / Enrichers(CorrelationIdEnricher) / Extensions(LoggerConfigurationExtensions) / Management(DebugModeInfo/LoggingLevelManager) / Masking(SensitiveDataDestructuringPolicy/SensitiveDataMasker)
- **配置实现散落 4 项目**：
  - `DesktopSerilogConfiguration.cs`（Desktop.Infrastructure/Logging/）
  - `SerilogMSSqlServerExtensions.cs`（WebAPI/Extensions/）
  - `LoggingRegistrationExtensions.cs`（Shell/Extensions/）
  - `LoggingHttpHandler.cs`（Desktop.Foundation/Http/）——HTTP 日志拦截器
  - CorrelationId 机制散落：`CorrelationIdMiddleware.cs`（WebAPI）、`LoggingHttpHandler`（Foundation）、`ActivityCorrelationIdProvider`（Shared.Logging）
  - `ApiLoggingFilter.cs`（WebAPI/Filters/）
- Serilog 包被 4 项目引用：Shared.Logging / Infrastructure / WebAPI / Desktop.Infrastructure

**审查问题**：
1. 日志的「配置/初始化/CorrelationId/脱敏/MSSQL sink/HTTP 拦截」是否应收敛回 `LYBT.Shared.Logging`？
2. `LYBT.Shared.Logging` 应否升级为「独立完整日志项目」——对外只暴露 `AddLybtLogging(IServiceCollection/IHostBuilder)` 初始化入口 + `ILogger` 工厂，**全程接管 Server+Desktop 日志**？
3. 收敛后各项目自己的 Serilog 扩展（DesktopSerilogConfiguration/SerilogMSSqlServerExtensions/LoggingRegistrationExtensions）应删还是留薄壳？
4. CorrelationId 机制是否应单点化（Provider 在 Shared.Logging，Middleware/Handler 统一由 Shared.Logging 提供注册扩展）？
5. 给出**日志整合方案**：目标项目结构（文件/类/职责）+ 迁移清单（源→目标）+ 保留/删除决策

## 第三部分：异常统一设计专项（核心）

**现状证据（技术总监已核实）**：
- `LYBT.Shared.ExceptionHandling` 7 文件：AppException 层次（Business/Conflict/NotFound/Validation/Api/Unauthorized）✅ 已集中
- **但处理器分裂**：
  - Server：`SystemExceptionHandler.cs` + `BusinessExceptionHandler.cs` 在 **Infrastructure/ExceptionHandling/**（不在 ExceptionHandling 项目）
  - Desktop：`DesktopExceptionHandler.cs` + `IDesktopExceptionHandler.cs` 在 **Desktop.Infrastructure/ExceptionHandling/**、`ClientErrorMessageMapper.cs` 在 **Foundation/ExceptionHandling/**
  - 错误码 `ErrorCode.cs` 在 `Shared.Models/Primitives/ErrorCodes/`
- ExceptionHandling 项目被 4 项目引用：Foundation / Desktop.Infrastructure / Server.Infrastructure / WebAPI

**审查问题**：
1. 异常处理器（Server 的 System/Business Handler）是否应收敛到 `LYBT.Shared.ExceptionHandling`（与异常层次同项目，完整职责：层次定义 + 处理器 + 注册扩展）？
2. 错误码枚举是否应收敛到 ExceptionHandling（或注明与异常层次同域的归属规则）？
3. Desktop 侧异常处理（DesktopExceptionHandler/ClientErrorMessageMapper）与 Server 侧是否应统一设计（同一映射策略）？
4. 异常→HTTP 映射的 SSOT 位置？注册方式（AddExceptionHandler 扩展是否应由 ExceptionHandling 提供）？
5. 给出**异常统一方案**：目标结构 + 迁移清单 + 保留/删除决策

## 第四部分：其他可集中机制识别

- **配置**：`LYBT.Shared.Configuration` 25 文件的 Options 类/验证器，是否有散落（Server/Desktop 自建配置类）？
- **映射**：Mapperly 注册是否集中？Shared 是否有映射需求？
- **验证**：FluentValidation 管道/验证器分布（Shared.Validators vs 模块内 Validators）
- **错误码/常量**：ErrorCode/PolicyConstants 等常量 SSOT 核实
- **工具类**：Utilities 下是否有跨项目重复（如字符串/日期/枚举工具）

## 硬性约束

1. **只读**：不修改任何 src 代码；只写报告
2. 每发现带证据：`文件:行号` + 调用链
3. 分级按方案 §3.1（A/B/C/D/E），C/E 级必须给出收敛方向（集中到哪、统一成什么）
4. 专项必须给**可执行整合方案**（不是"建议统一"空话）
5. **不要 commit**：报告由技术总监统一提交
6. 产出单一报告：`docs/compose/reports/method-audit-shared-2026-08-08.md`

## 报告结构

```
# Shared 层方法级深审报告（Mimo 独立分析）
## 0. 方法说明（工具/基线/时间点）
## 1. 5 项目方法分级总表（A/B/C/D/E 计数）
## 2. 日志集中定义专项：现状分布图 + 整合方案 + 迁移清单
## 3. 异常统一设计专项：现状分布图 + 统一方案 + 迁移清单
## 4. 其他可集中机制（配置/映射/验证/常量/工具）识别
## 5. C 级重复清单 + E 级可集中清单
## 6. D 级死方法清单（待符号级复核确认）
## 7. 统计汇总
```

## 明确不做（防发散）

- ❌ 不审 Server/Desktop 模块内部（S2/S3）
- ❌ 不修改任何代码
- ❌ 不执行整合（只出方案，执行另立批次）
