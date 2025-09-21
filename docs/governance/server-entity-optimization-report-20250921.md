# 服务器端“实例（实体/关系/配置）”全面检查与优化报告（凌隐宝堂中医诊所 / LYBT.Server.sln）

- 日期：2025-09-21
- 范围：Server 端实体模型（LYBT.Entities）、EF Core 映射与关系（LYBT.Infrastructure/Data/*）、WebAPI 相关的实例化与装配（仅与数据一致性/性能/安全相关）
- 方法：源码静态审阅 + 结构化对照（20250920 优化文档、ER/EF 配置、现有 DbContext 映射）

——

## 执行结论

- 核心结构（问诊/处方与病历一对一、关键字段精度、并发控制 RowVersion、审计字段）已经落地，整体建模趋于稳定。
- 发现若干“可进一步优化”的一致性、可维护性与可观测性问题，主要集中在：
  - 状态枚举的存储不一致（字符串 vs 整数）与过滤索引的维护成本（P1）
  - 审计字段在部分实体上不统一（CreatedAt/UpdatedAt/CreatedBy）（P1）
  - 索引策略与查询习惯的对齐（新增复合索引/覆盖索引的机会）（P2）
  - 价格/敏感字段精度与长度规范化（P2）
  - 业务约束可读性（过滤索引可改为计算列 + 普通唯一索引）（P2）
  - 一致的删除策略（Cascade vs Restrict）与软删除策略评估（P2）
- 不建议进行破坏性大改，建议以“低风险、渐进式”的方式推进优化。

——

## 检查范围与样本

- DbContext 与实体映射：
  - src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs
- 代表性实体/关系：
  - Consultation、Prescription ↔ MedicalCase 一对一
  - Users、Patients、Herbs、Formula 及其字段与索引
  - SystemLog 与 Serilog MSSqlServer Sink 的落库模型
- 参考文档：
  - 20250920/optimization-plan.md、20250920/er-and-ef-config.md

——

## 发现项汇总（优先级）

- P1｜状态枚举存储不一致（字符串 vs 整数）
  - 现状：如 MedicalCase.Status 使用字符串（HasConversion<string>()），Patients.Status 使用整数（HasConversion<int>()）。
  - 风险：
    - 过滤索引基于字符串常量（例如 `[Status] = 'Active' OR 'Draft'`）维护成本较高；
    - 枚举值变更/本地化易引入一致性问题；
    - 字符串存储在空间与比较上均弱于整数。
  - 建议：统一为整型（HasConversion<int>()）+ 集中枚举定义；为可读性通过视图/映射层转换。

- P1｜审计字段不统一
  - 现状：MedicalCase 要求 CreatedBy/CreatedAt；Consultation/Prescription 仅见 CreatedBy 强制，CreatedAt 未统一强制。
  - 风险：审计链不完整，问题追溯困难；数据生命周期分析受限。
  - 建议：统一在所有关键实体强制 CreatedAt/CreatedBy（可通过拦截器或 SaveChanges 钩子自动填充）。

- P2｜过滤唯一索引的维护性（PatientId 上“仅活动病历”的唯一约束）
  - 现状：通过 Filter（字符串表达式）实现：`[Status] = 'Active' OR [Status] = 'Draft'`。
  - 风险：
    - 过滤表达式与代码枚举常量耦合，变更时容易遗漏；
    - 过滤语义分散在数据库层与代码层。
  - 建议：引入计算列 `IsOpen`（bit），由状态机统一设置；在 `IsOpen=1` 上建立唯一索引，提升可读性与维护性。

- P2｜索引策略与查询习惯对齐
  - 现状：已在 PatientId/UserId/CreatedAt 等字段建立索引；
  - 机会：
    - 常见分页/筛选复合查询（如 `Status + CreatedAt`、`PatientId + CreatedAt`）可评估复合索引；
    - 针对读多写少的列表型接口，考虑覆盖索引（仅在热点查询明确的情况下）。

- P2｜价格/口令等字段规范化
  - 现状：Herb.Price/CostPrice、PrescriptionItem.UnitPrice/Quantity 精度合理；Users.PasswordHash（256）与 AdminSecret.PasswordHash（500）长度不统一；
  - 建议：口令哈希字段长度统一为同一上限（例如 512），避免后续算法迁移时的长度瓶颈；统一 decimal 精度规范表（decimal(18,2) 作为金额标准）。

- P2｜删除策略一致性与软删除评估
  - 现状：一对一链路使用 Cascade 合理；
  - 建议：评估所有一对多/多对一关系的 OnDelete 行为（关键主档建议 Restrict 防止误删）；若未来有“合规留存”需求，预留软删除标记（IsDeleted + 查询过滤器）。

- P3｜可观测性与调试
  - 建议：为关键关系/状态变更操作补充审计日志（领域事件或统一日志），结合现有 SystemLogs；在测试环境保留易读视图（如 V_OpenMedicalCases）。

——

## 优化建议详解

1) 统一状态存储为整型（建议 P1）
- 方案：
  - 将字符串存储改为整型（HasConversion<int>()），统一枚举中心；
  - 以 Automapper 或 API 输出层保证可读性（转换标签）。
- 影响评估：
  - 需要迁移脚本（字符串→整数）；
  - 业务逻辑无需变更，查询与索引表达式更为稳定与高效。

2) 审计字段自动化（建议 P1）
- 方案：
  - 通过 EF 拦截器或 SaveChanges 覆写，自动填充 CreatedAt/CreatedBy/UpdatedAt；
  - 在 DbContext 中集中管理，减少手工漏填风险。
- 影响评估：
  - 极低风险；对既有实体新增 UpdatedAt 字段需迁移脚本。

3) 过滤索引改为计算列（建议 P2）
- 方案：
  - 在 MedicalCases 增加计算列 `IsOpen`（依据状态枚举判定）；
  - 唯一索引改为 `IsOpen=1` 上的唯一索引；
  - 业务代码不变，过滤语义集中在一处维护。
- 影响评估：
  - 需要一次性迁移；
  - DBA 角度更易读，后期状态扩展成本低。

4) 复合/覆盖索引评估（建议 P2）
- 场景：
  - 列表页典型排序/筛选（如 CreatedAt DESC + Status/DoctorId/PatientId）
- 方案：
  - 基于实际查询频次（可从日志/慢查询采样）增补复合索引；
  - 谨慎使用覆盖索引，避免写入压力增大与过多索引维护。

5) 口令哈希与金额规范化（建议 P2）
- 方案：
  - PasswordHash 字段统一长度（如 nvarchar(512)）；
  - 统一金额字段 decimal(18,2) 的规范清单，作为未来 DDL 基线。

6) 删除策略与软删除评估（建议 P2）
- 方案：
  - 审阅所有关系的 OnDelete 行为（链路关键主档使用 Restrict）；
  - 若需软删除，统一 IsDeleted + 全局查询过滤器（但需评估与过滤索引的交互）。

——

## Quick Wins（低风险快速收益）

- 在 DbContext 中引入审计自动填充（CreatedAt/UpdatedAt/CreatedBy）。
- 统一金额与口令哈希长度规范，不影响业务逻辑。
- 为常用筛选增加少量复合索引（基于日志验证）。

——

## 需要确认的决策点

- 是否接受“状态统一为整型”并安排一次迁移？
- MedicalCase 的“开放病历唯一约束”是否改用计算列 IsOpen？
- 软删除是否纳入中短期路线？

——

## 建议落地步骤（不修改代码，仅方案）

1) 决策评审（0.5 天）：确认 P1 项与计算列方案；
2) 迁移与变更设计（1 天）：出具更改脚本与回滚脚本；
3) 小范围实施（1–2 天）：在测试库验证；
4) 回归与基线更新（0.5 天）：更新开发规范与 ER/DDL 基线文档。

> 注：本报告仅为分析与方案建议，未对代码做任何改动。如需我输出对应迁移脚本与示例代码，请确认优先级与时间窗口。
