# PRD——Shared 清单梳理与结构优化（LYBT.Shared.*）

- 文档日期：2025-09-21
- 项目经理：ccpm（Claude Code Project Manager）
- 作用范围：`src/Shared/*`（LYBT.Shared.Models / LYBT.Shared.Interfaces / LYBT.Shared.Utilities）及其与 Server/Client 的引用关系；文档同步与索引

## 背景（Problem & Context）
- Shared 是前后端共享契约的唯一来源，应承载 DTO、接口、枚举、通用异常/结果等规范化资产
- 现状问题：
  - DTO/接口/枚举分布与命名存在不一致，目录/命名空间部分偏差
  - 个别不当依赖风险（例如误入 AspNetCore/Swashbuckle 等运行时依赖）
  - 缺少“权威清单”和约定文档（枚举展示/国际化、DTO 继承、分页约定等）
  - 缺少“结构优化建议”和“架构门禁（ArchTests）”项的规范记录

## 目标（Goals）
- G1：产出 Shared 全量清单（DTO/接口/枚举/通用结果/异常）与引用关系文档，可溯源到 Server/Client/Tests
- G2：产出 Shared 目录/命名空间/依赖边界的结构优化建议与迁移映射
- G3：产出“枚举规范”文档（编码/code 与本地化展示/name 的约定、i18n 展示规范、前后端一致性）
- G4：梳理并补充（或规划）架构门禁（ArchTests）规范，避免不当依赖与层次越界
- G5：同步更新根 README、Shared 门面 README 与文档导航，形成权威入口

## 非目标（Non-Goals）
- 不引入业务功能或接口 breaking changes
- 不在本 PRD 中实施大规模重构/迁移，仅提出并评估“结构优化建议”（如需落地，另立实现 PRD）

## 用户故事（User Stories）
- 作为后端/前端开发者，我需要一份权威清单与约定文档，以在新增或调整 DTO/接口/枚举时保持一致
- 作为架构负责人，我需要门禁规范避免越界依赖、保证分层稳定

## 范围（Scope）
- In Scope：
  - 文档产出：Shared 清单、依赖图、枚举规范、结构优化建议、ArchTests 约定
  - README 与文档导航更新（根 README、src/Shared/README.md、docs/index.md、docs/modules/index.md）
- Out of Scope：
  - 影响接口的 breaking change
  - 大规模代码迁移/重构（仅在后续 PRD 实施）

## 需求明细（Requirements）
- R1：清单与依赖
  - docs/prds-summary/shared-inventory/shared-types.md：列出 DTO/接口/枚举/通用结果/异常，标注来源与引用
  - docs/prds-summary/shared-inventory/shared-deps.md：Shared ↔ Server/Client 依赖关系图（Mermaid）
- R2：枚举规范
  - docs/prds-summary/shared-inventory/shared-enums-spec.md：约定 code/name、i18n 展示、前端字典缓存、接口约束
- R3：结构优化建议
  - docs/prds-summary/shared-inventory/shared-structure-proposal.md：目录/命名空间/依赖边界建议；附迁移映射与风险评估
- R4：架构门禁规范
  - docs/prds-summary/shared-inventory/shared-arch-gates.md：约定 Shared 禁止依赖清单（AspNetCore/EF/Swashbuckle 等）、边界规则与 ArchTests 方向
- R5：README 与导航更新
  - 更新根 README 的文档目录/模块索引（已存在，如有新增路径同步）
  - 更新 src/Shared/README.md 增加“关键约定/清单索引”链接
  - docs/index.md 与 docs/modules/index.md 增加 Shared 清单与规范入口

## 成功指标（Success Metrics）
- 清单覆盖率 ≥ 95%（与代码核对，体现来源/去向多处引用）
- 依赖图完整且无明显遗漏；门禁规范与现状一致/规划可落地
- README 与文档导航均可达，形成单一真相来源

## 验收标准（Acceptance Criteria）
- 按 R1–R5 产出文件均已提交且可读；命令/链接可用，0 错误
- 实现必须严格遵循本 PRD 的要求与范围。任何偏差须先更新 PRD 并获批准
- 相关 README（根、Shared 门面）与 docs/index.md、docs/modules/index.md 已更新链接与说明

## 里程碑与实施步骤（Milestones）
- 提交 1：清单与依赖（R1）
- 提交 2：枚举规范（R2）
- 提交 3：结构优化建议（R3）
- 提交 4：门禁规范（R4）
- 提交 5：README 与导航更新（R5）

## 风险与缓解（Risks & Mitigations）
- 清单覆盖不全 → 通过 rg 扫描 + 人工核对 + 持续补齐
- 结构优化建议影响面大 → 仅文档化提出方案与映射，落地另立 PRD 执行与回滚策略
- 门禁与现状存在差距 → 标注差距与过渡期策略

## 依赖与前置（Dependencies & Preconditions）
- 固定 SDK（global.json：9.0.305）、目录结构稳定
- 访问权限：可读取 src/Shared/* 与 Server/Client 引用

## 回滚策略（Rollback）
- 文档回滚到前一版本；不涉及代码改动

## 测试（Testing）
- 文档链接与目录导航检查
-（后续实现 PRD）ArchTests 的构建与执行

## 产出物（Deliverables）
- 文档：shared-types.md / shared-deps.md / shared-enums-spec.md / shared-structure-proposal.md / shared-arch-gates.md
- README 更新：根 README、src/Shared/README.md、docs/index.md、docs/modules/index.md 的链接与说明
- 完成总结文档（Summary）：docs/prds-summary/PRD-server-shared-inventory-and-structure-optimization-20250921-SUMMARY.md（包含变更摘要、验证与测试、已更新 README 列表与链接、风险与后续）
