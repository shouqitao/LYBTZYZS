---
feature: webapi-arch-fix
status: delivered
specs:
plans:
  - docs/compose/plans/2026-08-05-webapi-arch-fix.md
branch: master
commits: 59be25317..434f0ebb5
---

# WebApi 架构修复 — Final Report

## What Was Built

`docs/03-architecture/03-server.md` 最后一次更新于 2026-06-28，此后的大规模重构（MediatR 简化 A-03、实体源统一 A-05、Repository 泛型化 A-06、连接字符串回退 H-02 等）使其与代码全面脱节。技术总监 + OpenCode 审查确认 14 项脱节问题（P0×2, P1×8, P2×4），本任务将其全部修复：

- **P0-1（唯一代码违规）**：ReportsController 原本直接注入 `IReportRepository`（违反「Controller 注入 Service 接口，禁止注入 Repository/DbContext」规则）。新增 `IReportService` 接口 + `ReportService` 实现（internal，与 ReportRepository 一致），Controller 改为注入 Service 接口。
- **其余 13 项（纯文档）**：03-server.md 逐节重写，反映代码真实状态。

同时补上了 P0-1 漏检的根因——架构测试 `ServerAssemblies` 数组缺少 `LYBT.Module.Reports` 与 `LYBT.Module.Registration`，修复后此类违规将被守卫测试捕获。

## Architecture

**代码变更（Reports 模块，分层从 Controller → Repository 直连改为 Controller → Service → Repository）**：

- `src/Server/Modules/LYBT.Module.Reports/Interfaces/IReportService.cs` — 3 个只读聚合查询方法（日收入/日问诊/日药材使用），返回 `LYBT.Shared.Models.Contracts.Reports` 的 DTO
- `src/Server/Modules/LYBT.Module.Reports/Services/ReportService.cs` — internal 实现，组合 IReportRepository 的 5 个查询方法组装 DTO，不再持有 DbContext
- `ReportsModule.cs` — 在仓储注册后追加 `AddScoped<IReportService, Services.ReportService>()`
- `ReportsController.cs` — 字段/构造参数改为 `IReportService`，三个端点改为单次 Service 调用
- `tests/LYBT.Tests.Architecture/ServerArchTests.cs` — `ServerAssemblies` 追加 Reports/Registration 两个程序集

**文档变更（03-server.md v2.3）**：架构模式总述（6 模块 MediatR CQRS + MedicalCase Service 拆分 + Reports 只读聚合）、架构图（Entities 移入 Shared 层、模块标注实际模式）、模块目录三形态、模块清单跨模块通信方向、MedicalCase 5 接口清单（删 Permission/Audit/Rules）、Controller 规范示例（`[ApiVersion("1")]` + `api/v{version:apiVersion}` 路由、BaseCrudController 注入 ISender）、新增「模块独立 DbContext」小节、错误码表（删 7xxxx、增 8xxxx、枚举指向 ErrorCode.cs）、BaseService 实际状态、BaseRepository 5 方法、Entities 位置与 10 目录、错误码枚举位置，变更记录追加 v2.3。

### Design Decisions

- 选择了「新增 Service 层」而非「Controller 直连」——Reports 是只读聚合模块，业务逻辑薄，但必须满足分层规则，使架构守卫能监督所有模块。
- `ReportService` 设为 internal（与 `ReportRepository` 一致），不继承 `BaseService`——与 Users/Patients/Herbs/Formula 的 Service 现状一致（BaseService 仅 MedicalCase 使用），避免引入不必要的基类依赖。
- 文档中 Reports 模块的架构模式标注为「Service + Repository（只读聚合）」而非计划草稿的「Controller → Repository」——因为 Task A 已先行修复代码，文档必须描述修复后的状态（验收标准：文档与代码完全一致）。

## Usage

无用户可见行为变化。三个报表端点的路由与响应格式不变：

- `GET /api/v1/reports/daily/income`
- `GET /api/v1/reports/daily/consultations`
- `GET /api/v1/reports/daily/herbs`

架构约束演进：此后任何模块的 Controller 直连 Repository 都会被 `ServerArchTests`（现 92 个守卫测试）捕获。

## Verification

- `dotnet build LYBTZYZS.sln --no-incremental` — **0 错误 0 警告**（存量警告 0，全量非增量编译）
- `dotnet test tests/LYBT.Tests.Architecture/` — **92/92 通过**（含新增程序集后的全部守卫：P02 Repository 继承、P10 Service 禁注入 AppDbContext、Services 后缀、模块循环依赖等，Registration 的 `RegistrationRepository` 经自有-DbContext 豁免、`BaseRegistrationsController` 为 abstract Base 类被排除，均预判并实测通过）
- 03-server.md 逐项对照代码核实（BaseRepository 5 方法、MedicalCaseModel 域方法、CrossModule 8 文件、模块级 DbContext 归属、错误码分区等全部 grep/read 验证）

## Journey Log

- [lesson] 架构守卫漏检的根因往往在守卫本身——Reports 违规长期存在正是因为 `ServerAssemblies` 未包含该模块；修复违规时必须同时补守卫。
- [lesson] 计划文档为「文档先行」的措辞与任务列表的 A→B→C 顺序冲突，以用户明确指定的执行顺序为准；文档中 Reports 行按修复后的代码状态撰写而非计划草稿的修复前状态。
- [dead end] `ICrossModuleAuthService` 与 Token Family 段落在代码中 0 匹配（仅存在于 README 与文档），但计划明确将范围限定为删除 Core 层职责清单中的一行，Token Family 段属 AUTH-D06/D07 设计说明，按计划范围保留（已列入报告知会，待后续清理决策）。

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/compose/plans/2026-08-05-webapi-arch-fix.md` | Implementation plan | 14 项勘察结论 + A/B/C 任务，已按此执行 |
| `docs/03-architecture/03-server.md` | Target document | v2.3，与代码全面对齐 |
| `docs/03-architecture/13-project-master-plan.md` | Project ledger | §八/§九 记录完成状态与决策 |
