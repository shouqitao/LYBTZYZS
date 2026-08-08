---
feature: a18-p1-convergence-batch1
status: delivered
specs:
  - docs/03-architecture/task-a18-p1-convergence-2026-08-08.md
  - docs/03-architecture/structure-audit-crosscheck-2026-08-08.md
plans: []
branch: master
commits: 8db198b8a..af1c5b2d5
---

# A-18 P1 机制收敛批次 · 第 1 子批次（低风险机械收敛）— Final Report

## What Was Built

A-18 是「架构完全可控」收敛批次，共七项 P1 任务。本次交付第 1 子批次（低风险机械收敛）四项，全部独立验证、独立 commit、独立 push：

1. **P1-7 文档重写先行**（`8db198b8a`）— 按实际 5 项目结构重写 `08-shared.md`（原声称 8 项目实际 5），并同步修正交叉验证 §5 的 13 项文档偏差（D1-D13）跨 5 个文档。
2. **P1-3 CorrelationId 收敛**（`6769b02e0`）— 删除死代码 `AsyncLocalCorrelationIdProvider`（全仓 0 调用点），修 `CorrelationIdEnricher` 矛盾注释，确认双端各自端内单机制。
3. **P1-4 Server 手写 Mapper 改 Mapperly**（`a484f6c15`）— Formula/Herbs/Patients/Users 4 模块手写静态 Mapper 改 `[Mapper]` 静态 partial 类，纯复制方法转编译时生成，工厂方法保留手写（行为等价）。
4. **P1-6 本地配置持久化**（`031682cad`）— 本地 ConfigurationController 的内存 ConcurrentDictionary 改复用远程 `JsonFileConfigurationStore` 落盘，重启不丢。

## Architecture

### P1-7 文档-代码对齐（D1-D13 修正）

| 文档 | 偏差 | 修正 |
|------|------|------|
| 08-shared.md | 声称 8 项目（Primitives/Utilities/Components/Validators 独立） | 重写为实际 5 项目；4 项坍缩为 Shared.Models 内文件夹；Utilities 清单纠错（实际 4 文件）；删虚构 DTO 继承链（BaseDto/TimestampDto/StatusDto/AuditDto 不存在）；ExceptionHandling/Configuration 目录按代码实际；Mapperly 数量 23→13 |
| 03-server.md | D3 ReportsDbContext 不存在 | 修正模块独立 DbContext 小节；RESTful 示例补版本段 `/api/v1/` |
| 05-dual-mode.md | D5-D10 六项 | URL 前缀统一 `/api/v1/`；EnsureCreated→MigrateAsync+双种子；DI 架构 ADR-0010 统一服务层；端点覆盖表 104/99 重写（删虚构 categories/by-phone 端点）；打印日志 404；Rate Limiting 5/60s；LocalWebApiDbContext→AppDbContext |
| 00-architecture-summary.md | D11 BCrypt→PBKDF2 | 修正 Auth 技术栈表述 |
| Core AGENTS.md | D13 LocalData 项目不存在 | 修正目录清单与依赖表述 |

### P1-3 CorrelationId 单机制

- **删除**：`AsyncLocalCorrelationIdProvider.cs` + `AddAsyncLocalCorrelationIdProvider` 扩展（交叉验证 C2：0 调用点，已死）
- **修正**：`CorrelationIdEnricher.cs` 注释（原声称 Desktop 用 AsyncLocal，实际 `DesktopSerilogConfiguration.cs:34` 用 `ActivityCorrelationIdProvider`）
- **确认**：Server = `CorrelationIdMiddleware`（W3C traceparent，`LYBT.WebAPI/Middleware/`）；Desktop = `ActivityCorrelationIdProvider`（Activity.Current.TraceId）。双端各自端内单机制，跨端不强统一（任务书判断成立）

### P1-4 Mapperly 静态 partial 改造

- 4 模块 Mapper 类声明统一为 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target, AutoUserMappings = false)] static partial class`
- **转 Mapperly 生成**（`public static partial`）：Herbs/Patients 的 ToListDto/ToDetailDto（属性全同名）；Formula 的 ToListDto（需 `[MapProperty]` Indication→Indications + `[MapperIgnoreTarget]` TotalPrice）
- **保留手写**（映射方法签名不变、行为等价）：所有 `ToEntity`（走领域工厂 Formula.Create/Herb.Create/Patient.Create，Mapperly 无法表达）；Formula 的 ToDetailDto（Category 空值回退 + Herbs 嵌套）；UserMapper（`?? string.Empty` null 合并防御）
- **关键技术点**：`AutoUserMappings = false` 使不带 `[UserMapping]` 标记的 ToEntity 不被 Mapperly 自动发现，规避 RMG001（带额外参数签名不受支持）；`[UserMapping(Default = false)]` 标记 UserMapper 手写方法
- 静态调用点（`FormulaDtoMapper.ToListDto(...)` 等）零改动——Mapperly 4.3.1 官方支持 `[Mapper] public static partial class` + 静态 partial 方法

### P1-6 本地配置持久化

- 本地 `ConfigurationController` 由静态 `ConcurrentDictionary` 内存存储改为注入 `IConfigurationStore`
- 复用远程 `JsonFileConfigurationStore`（`LYBT.Infrastructure/Configuration/Stores/`）：落盘 `{BaseDirectory}/config/runtime-overrides.json`，原子写入（临时文件 + Move），重启构造时 `LoadFromFile()` 恢复
- `LocalWebApiProgram.CreateApplication` 注册 `IConfigurationStore` 单例
- Controller 方法改 async；P20 依赖白名单（LYBT.Infrastructure）合规

## Usage

无用户可见行为变化（第 1 子批次全部为内部机制收敛）：

- 本地模式 PUT `/api/v1/configuration/{key}` 写入的配置项现在持久化到 `{BaseDirectory}/config/runtime-overrides.json`，重启 Desktop 后仍保留（此前内存存储重启即失）
- Server Mapper 对外方法签名不变，仅实现由手写改为 Mapperly 编译时生成

## Verification

每项独立验证通过后才 commit + push：

| 子任务 | build | 架构测试 | 单测 |
|--------|-------|---------|------|
| P1-7 文档 | 纯文档改动 | — | — |
| P1-3 | 0 错误 0 警告 | 83/83 | 日志相关 16/16 |
| P1-4 | 0 错误 0 警告 | 83/83 | Formula/Herb/Patient/User 161/161 |
| P1-6 | 0 错误 0 警告 | 83/83（P20 合规） | JsonFileConfigurationStore 4/4 |

Desktop LocalWebAPI 集成测试 404 失败经 stash 基线复现确认为 C-01 已知环境项（无运行中 LocalDB 服务），与本次改动无关。

## Journey Log

- [lesson] Mapperly 对 `[Mapper]` 类中带额外参数的手写方法（如 `ToEntity(dto, createdBy)`）默认自动发现并报 RMG001 签名错误——`AutoUserMappings = false` + 不加 `[UserMapping]` 是干净解法，比逐方法 `[UserMapping(Ignore = true)]` 更彻底
- [lesson] Mapperly 4.x 支持 `[Mapper] public static partial class` 与静态 partial 方法，静态调用点可完全不变，收敛成本远低于改实例注入
- [lesson] 端点覆盖表原文档数字（113/112、categories/by-phone 端点）为虚构——以 13b-api-endpoints.md（SSOT）与控制器代码实际为准重写
- [lesson] D1 声称的 BaseDto/TimestampDto/StatusDto/AuditDto DTO 继承链在代码中不存在，重写时按实际 Contracts/Common 类型清单纠正

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/03-architecture/task-a18-p1-convergence-2026-08-08.md` | 任务书 | 第 2 子批次（P1-5/P1-1/P1-2）待执行，保持活跃 |
| `docs/03-architecture/structure-audit-crosscheck-2026-08-08.md` | 依据 | §二 P1 清单 + 交叉纠错 C2 |
| `docs/03-architecture/structure-audit-mimo-2026-08-08.md` | 依据 | §5 文档偏差清单 D1-D13 |
