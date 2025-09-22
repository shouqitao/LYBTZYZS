# PRD——Server 实体一致性与结构优化（状态/约束/审计/IsOpen）

- 文档日期：2025-09-21
- 项目经理：ccpm（Claude Code Project Manager）
- 作用范围：`LYBT.Server.sln`（`src/Server/Core/LYBT.Infrastructure`、`src/Server/Core/LYBT.Entities`）及迁移脚本与文档（不涉及业务接口变更）

## 背景（Problem & Context）
- 实体建模存在历史不一致：
  - 状态枚举存储混用（string/int），维护成本高
  - 审计字段（CreatedAt/UpdatedAt/CreatedBy）在关键实体未统一
  - 医疗案例开放态标识与约束不足（IsOpen/唯一约束）
  - 级联删除与查询路径未统一约束，存在误删/查询不稳风险
- 目标：以最小风险实现“可回滚”的一致性优化，提升可维护性与可观测性，不改变现有对外行为

## 目标（Goals）
- G1：统一状态字段为 `int` 存储（EF HasConversion<int>()），保持枚举约束
- G2：DbContext 统一维护审计字段（CreatedAt/UpdatedAt/CreatedBy）
- G3：对 MedicalCase 建立 `IsOpen` 计算/约束策略，并以 `IsOpen=1` 的唯一约束替代易冲突的复合唯一键
- G4：梳理关键关系的 OnDelete 规则（默认 Restrict），并固化常用查询路径的索引规范
- G5：形成完整文档与迁移/回滚脚本，ArchTests 约束不当依赖

## 非目标（Non-Goals）
- 不修改对外 API/DTO 契约，不引入业务功能改动
- 不做大规模重构，仅提出与验证“可落地的优化方案”

## 用户故事（User Stories）
- 作为后端开发，我需要统一的实体与审计规范，从而稳定迭代与排查
- 作为 DBA/运维，我需要可追踪的一致性规则与唯一约束，减少运行时风险

## 范围（Scope）
- In Scope：
  - Entities/Infrastructure：状态字段转换、审计填充、IsOpen 与唯一约束策略、OnDelete/索引建议
  - EF Migrations：迁移/回滚脚本与验证
  - 文档：DDL/说明/约束与回滚指引
- Out of Scope：
  - API/DTO 的展示变更（前端仍使用既有映射/标签，由文档说明存储变更）
  - 跨域重构（需另立实现 PRD）

## 需求明细（Requirements）
- R1 状态字段统一
  - 现有 string 存储统一为 `HasConversion<int>()`；补齐枚举与字典定义，避免“魔法字符串”
  - 迁移脚本与回滚脚本成对提供
- R2 审计字段
  - 在 SaveChanges 管道自动维护 `CreatedAt/UpdatedAt/CreatedBy`
  - `CreatedBy` 来源于当前用户上下文（无用户时回退为系统标识），保证可测试
- R3 IsOpen 与唯一约束
  - 为 MedicalCases 生成 `IsOpen`（开放态）策略；基于 `IsOpen=1` 建立新的唯一约束
  - 提供迁移/回滚脚本与校验 SQL
- R4 关系与索引
  - 关键导航的 `OnDelete` 明确为 Restrict（除非另有证明需求）
  - 为高频查询提供复合索引建议，并在文档中列出
- R5 文档与门禁
  - 形成一揽子文档：
    - docs/server-entities/entity-consistency-plan.md（方案）
    - docs/server-entities/migrations-guide.md（迁移/回滚）
    - docs/server-entities/indexing-and-deletion-rules.md（索引与删除规则）
  - ArchTests：约束 Infrastructure/Entities 不引用 Web 层或不当包（规划即可）

## 成功指标（Success Metrics）
- 迁移脚本在测试环境执行成功（含回滚验证）
- 构建/测试通过，ArchTests 门禁无新增违规项
- 关键查询计划稳定（可选记录前后 diff）

## 验收标准（Acceptance Criteria）
- 提交 R1–R5 的文档与迁移脚本（若涉及），命令/链接可用，0 错误
- 实现必须严格遵循本 PRD 的要求与范围。任何偏差须先更新 PRD 并获批准
- 相关 README 与导航（根 README、src/Server/README.md、docs/index.md）已更新链接与说明

## 里程碑与实施步骤（Milestones）
- 提交 1：状态字段统一（R1）
- 提交 2：审计字段（R2）
- 提交 3：IsOpen/唯一约束（R3）
- 提交 4：关系/索引（R4）
- 提交 5：文档与门禁（R5）

## 风险与缓解（Risks & Mitigations）
- 迁移覆盖风险 → 基于 rg 扫描 + 实际数据库核对 + 回滚脚本
- 审计字段填充影响现有写入 → 在文档中描述回退策略与兼容方案
- OnDelete 调整影响数据操作 → 提供排查与运维指引

## 依赖与前置（Dependencies & Preconditions）
- 固定 SDK（global.json）与目录结构；数据库连接可用

## 回滚策略（Rollback）
- 按步骤回滚迁移；文档提供 SQL 示例与验证步骤

## 测试（Testing）
- 迁移/回滚脚本的演练；构建与 ArchTests 的执行

## 产出物（Deliverables）
- 文档：entity-consistency-plan / migrations-guide / indexing-and-deletion-rules
- README 更新：根 README（文档目录）、src/Server/README.md、docs/index.md 增加链接
- 完成总结文档（Summary）：docs/prds-summary/PRD-server-entity-consistency-optimization-20250921-SUMMARY.md（包含变更摘要、验证与测试、已更新 README 列表与链接、风险与后续）
