# 服务器实例优化复检报告（凌隐宝堂中医诊所 / LYBT.Server.sln）

- 日期：2025-09-21
- 依据文档：`20250920/optimization-plan.md`、`20250920/er-and-ef-config.md`
- 复检对象：Server 端实体/关系/EF 配置/WebAPI 装配与安全
- 结论：核心结构化优化已基本落地；少量项与实现方式不同但等价，个别建议属于应用层（打印/前端）不在本次代码范围

——

## 一、实体与关系（ER/EF）优化复检

- Consultation 与 MedicalCase 一对一（唯一索引 + 外键级联）
  - 证据：`AppDbContext.ConfigureConsultations()` 设置 `HasIndex(c => c.MedicalCaseId).IsUnique()`，并 `HasOne(...).WithOne(...).OnDelete(Cascade)`
  - 结论：已实现，符合建议

- Prescription 与 MedicalCase 一对一（唯一索引 + 外键级联）
  - 证据：`AppDbContext.ConfigurePrescriptions()` 设置 `HasIndex(p => p.MedicalCaseId).IsUnique()`，并 `HasOne<MedicalCase>().WithOne(...).OnDelete(Cascade)`
  - 结论：已实现，符合建议

- 每名患者同一时间仅允许一条“未完成/未取消”的病历（唯一约束）
  - 建议表达：对 `MedicalCases(PatientId)` 设唯一索引并过滤 `Status NOT IN (Completed, Cancelled)`（整数存储）
  - 实际实现：`AppDbContext.ConfigureMedicalCases()` 使用字符串存储 `Status`，并过滤 `[Status] = 'Active' OR [Status] = 'Draft'`
  - 结论：等价实现（语义一致），已满足业务约束

- 并发控制（RowVersion）
  - 证据：`Consultation/Prescription/MedicalCase` 等实体均设置 `RowVersion` 并标记并发令牌
  - 结论：已实现

- 审计字段（CreatedBy/CreatedAt）
  - 证据：各实体设置 `CreatedBy`（必填）与 `CreatedAt`（必填，适用场景）
  - 结论：已实现

——

## 二、财务/处方字段与计算建议

- 单价/数量精度与类型
  - 证据：`PrescriptionItem.UnitPrice` 设定 `HasPrecision(18,2)`；`Quantity` 使用 `int`
  - 结论：已实现

- 处方折扣与总价计算
  - 证据：`Prescription.Discount` 设定 `HasPrecision(3,2)`；关于“打印展示与总价取整/截断 2 位”的逻辑属于应用层/前端打印，不在后端数据库/实体层硬化范围
  - 结论：字段与约束已实现；打印展示建议留给前端/打印模块实现

——

## 三、WebAPI 安全与硬化（与 20250920 间接相关）

- 生产禁用 Swagger：已落地（仅非生产启用）
- 安全响应头（CSP/XFO/CTO/Referrer/Permissions-Policy）：已按配置应用
- 压缩/缓存/OutputCache：已启用
- 速率限制（全局 + 登录策略）：已接线，按“≤20 人内部使用”场景配置
- JSON 编码：默认安全，可通过配置开关放宽

——

## 四、与建议不同但等价/可接受的实现

- 病历唯一约束的过滤条件
  - 建议：整数状态 + `NOT IN (2,3)`
  - 现状：字符串状态 + `Active/Draft`
  - 评估：等价；无需强制迁移至整数（除非后续统一枚举策略）

——

## 五、未纳入本轮范围的建议项

- 打印视图与金额展示（按每帖/总价/折扣后金额取两位截断）
  - 说明：属于客户端/打印模板逻辑；不影响 DB/实体层与 API 的结构性优化
  - 建议：在前端/打印模块中实现，并补充端到端测试用例

——

## 六、优化完成度评估（打分）

- 实体关系与唯一约束：100%
- 并发控制与审计字段：100%
- 价格/数量精度：100%
- WebAPI 安全与性能硬化：100%
- 打印/展示层建议：N/A（非本层职责）

综合结论：
- 20250920 文档提出的“实体关系约束、并发控制、精度与折扣字段”均已落实；
- 安全与性能硬化也已同步完成；
- 打印展示相关建议应交由前端/打印模块实现。

——

## 七、后续建议（如需进一步统一）

- 统一状态存储策略（可选）：将字符串状态改为整型枚举，配套迁移脚本（仅在确有收益时执行）
- 健康检查精简与限权（见硬化复核报告）：减少 details 信息暴露
- 清理遗留扩展与显式化 CORS

> 本报告仅为复检结论与建议，未对代码做任何改动。
