# ADR-0026: 授权矩阵 SSOT 收敛

> 版本: v1.0 | 日期: 2026-08-20
> Status: ACCEPTED
> Related: 01-product/04-permissions.md, 03-architecture/12-permissions-matrix.md, 02-ssot-architecture.md

## Context

- 权限矩阵在三处重复定义：产品层 `01-product/04-permissions.md`（业务规则）、架构层 `03-architecture/12-permissions-matrix.md`（视图速查）、以及各模块文档/代码注释中的零散描述，导致矩阵修订需同步多文件，易漂移（例：前台不可查药材/验方、医案创建仅 Doctor 等 2026-08-03 决策曾在两层不一致）。
- 架构评审 R2（权限一致性）与 R26-R30 文档审计均指出：权限为产品规则，应由产品文档权威定义，架构层仅需视图引用。

## Decision

1. **SSOT 定为产品层**：`01-product/04-permissions.md` 为权限矩阵唯一权威（L1），包含四角色 × 操作矩阵、策略映射、待对齐清单。
2. **架构层为视图**：`03-architecture/12-permissions-matrix.md` 仅保留行级安全、授权策略速查、代码待对齐清单，头部显式标注“权威见 04-permissions.md”，不得出现权威中没有的新规则。
3. **模块文档仅引用**：各需求/模块文档中的“模块权限摘要”保留上下文所需行，但必须标注权威链接，不复制全矩阵。
4. **代码以 SSOT 为准**：`PolicyConstants` 与 Controller `[Authorize]` 的后续变更必须先更新 `04-permissions.md`，再改代码，架构视图层随之同步（不反向定义）。

## Rationale

- 产品规则归产品，架构视图归架构，符合 SSOT 分层（L1 vs L2）与 `02-ssot-architecture.md` 原则。
- 单一权威使任务书状态列、总账 §九、R1/R2 覆盖矩阵与代码策略同源，减少“改一处漏一处”风险。

## Consequences

- `02-ssot-architecture.md` 表新增 16 号映射：授权矩阵 SSOT = 04-permissions.md。
- `12-permissions-matrix.md` 头部权威声明已满足要求，无需结构调整。
- 后续权限变更 PR 必须包含 `04-permissions.md` diff，否则视为文档滞后。

## 关联 US

- US-USER-004/005、US-PAT-XXX、US-HERB-XXX 等涉及角色权限的 US 均以本 ADR 的 SSOT 为解释源。
