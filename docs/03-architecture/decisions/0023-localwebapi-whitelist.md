# ADR-0023: LocalWebAPI 架构测试白名单（P07 例外正名）

> 版本: v1.0 | 日期: 2026-08-21
> Status: ACCEPTED
> Related: ADR-0010 (统一服务层), ADR-0009 (URL 驱动双模式), P07 Server 模块间零引用规则

## Context

架构审查 `architecture-deep-review-2026-08-21.md` P0-1 发现：`LYBT.LocalWebAPI.csproj:8-14` 直接引用 6 个 Server 模块，违反总纲“模块间不直接引用”（P07）表述，但 `05-dual-mode.md` 与 ADR-0010 已定义为统一服务层刻意设计。`00-architecture-summary.md` 未声明例外，`tests/LYBT.Tests.Architecture/TestAssemblies.Server` 隐蔽排除 LocalWebAPI，`ArchTests.P07` 未显式豁免，导致“测试绿≠架构干净”的假绿。

## Decision

**正名 LocalWebAPI 为 P07 唯一例外，显式白名单。**

- 允许 `LYBT.LocalWebAPI` 直接引用以下 Server 项目（清单以本 ADR 为准，与 ADR-0010 一致）：
  - `LYBT.Entities` / `LYBT.Infrastructure`
  - `LYBT.Module.Identity` / `LYBT.Module.Catalog` / `LYBT.Module.Patients` / `LYBT.Module.MedicalCases` / `LYBT.Module.Registrations` / `LYBT.Module.Reports`
- 其余 Server 模块间仍零引用（P07）。
- `00-architecture-summary.md` 与 `05-dual-mode.md` 增“LocalWebAPI 特例”段落，并指向本 ADR。
- 架构测试显式豁免 LocalWebAPI（代码注释优于隐蔽的 assembly 排除）。

## Rationale

1. **行为一致**：双模式复用同一 Service 层，单套 bug 修复双端生效。
2. **零重复**：解耦需重写 6 Service+12 Repository，ROI 不足，属“假隔离”。
3. **可审计**：显式白名单 + ADR + 测试注释，使例外可 grep 可追溯。

## Consequences

- 总纲与双模式文档增特例说明；`02-ssot-architecture.md` SSOT 表增“LocalWebAPI 白名单→ADR-0023”。
- `TestAssemblies.Server` 显含 `LYBT.LocalWebAPI`，`ArchTests.P07` 首行 `if (assembly.GetName().Name=="LYBT.LocalWebAPI") continue; // exempt per ADR-0010/0023`。
- 新增 Server Module 引用到 LocalWebAPI 时必须先更新本 ADR 与 ADR-0010。

## 变更协议

1. 更新本 ADR 引用清单
2. 更新 `src/Client/Desktop/LocalWebAPI/LYBT.LocalWebAPI.csproj` 注释 `<!-- P07 Whitelist: unified Service layer, see ADR-0010/0023 -->`
3. 确保 `ArchTests.P07` 审计通过且注释存在

## 关联 US

- 双模式全业务本地端点（US-USER/PAT/HERB/FORM/MC/REG）—— LocalWebAPI 复用 Service 层确保一致性
