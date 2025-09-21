# 医案/诊断/处方 优化方案

## 目标
- 明确一病案一诊断、一病案至多一张处方；强化一致性与并发安全。
- 统一计价精度与打印口径；落实同日编辑与权限规则。

## 数据模型与关系
- 聚合根：`MedicalCase`（Draft/Completed/Cancelled）。
- 从属：`Consultation`（Completed/Cancelled）与 `Prescription`（Draft/Completed/Cancelled）均以 `MedicalCaseId` 外键关联同一病案。
- 去冗余：删除 `MedicalCase.PrescriptionId` 字段，仅保留病案 → 处方 的唯一关联（见下文索引）。
- 审计：为 `MedicalCase`/`Consultation`/`Prescription` 增加 `CreatedBy`（医生用户ID）。

## 约束与索引（EF Core / SQL Server）
- 一病案一诊断：`Consultations(MedicalCaseId)` 唯一索引。
- 一病案至多一处方：`Prescriptions(MedicalCaseId)` 唯一索引。
- 单患者仅一条未完成病案：`MedicalCases(PatientId)` 过滤唯一索引，条件 `Status NOT IN ('Completed','Cancelled')`。
- 常用查询索引（如未建）：Patients(Name)、Prescriptions(PatientId, CreatedAt)、Consultations(PatientId, ConsultationDate)、MedicalCases(PatientId, CreatedAt)。

## 计价与精度
- 处方项保存价格快照：`PrescriptionItem.UnitPrice` 固化保存时单价；`Quantity` 使用整数（剂量无小数）。
- 单副价 = Σ(UnitPrice × Quantity)。
- 总价 = 单副价 × 副数 × 折扣（0.8＝八折）。仅在“折扣后的最终总价”截断到 2 位小数（直接舍去）。
- 打印显示：每副价格（未折扣）与总价（折扣后）。不显示药材单价/单位。

## 权限与同日编辑
- 同日判定：以服务器本地时区，比较 `MedicalCase.CreatedAt.Date == Today`。
- 医生：仅创建者可在同日编辑/取消；管理员不受时间限制。
- 已完成病案在同日允许新增/修改处方（仍受“最多一张处方”约束与同日规则）。

## 经验方合并
- 前端合并多个经验方时对同名药材去重并取较小剂量；可提示冲突。
- 后端校验：处方项必须提供 `HerbId`，且药材未禁用（禁用仅允许历史查看，不得新开）。

## API 与流程（事务）
1) 新建病案：`MedicalCase`=Draft，保存创建者。
2) 诊断：保存 `Consultation`（Completed/Cancelled）。
3) 处方：保存 `Prescription`（Draft→Completed/Cancelled），持久化项的 `UnitPrice` 与整型 `Quantity`。
4) 保存病历：在单事务内将 `MedicalCase` → Completed（或 Cancelled），并联动写入 `Consultation`/`Prescription` 状态；失败则回滚。

## 校验与边界
- 病案完成/取消后跨日只读（医生端）；管理员可任意时间更改。
- 禁用药材在新单不可选；历史处方/经验方可显示名称但不可链接详情（或提示“已禁用”）。

## 迁移步骤（建议顺序）
1) 为 `Consultations(MedicalCaseId)`、`Prescriptions(MedicalCaseId)` 添加唯一索引；
2) 为 `MedicalCases(PatientId)` 添加过滤唯一索引（排除 Completed/Cancelled）；
3) 为三类对象新增 `CreatedBy`；为 `MedicalCase`/`Consultation` 增加乐观并发 `RowVersion`；
4) `PrescriptionItem.Quantity` 迁移为整数（如需）；确保 `UnitPrice` 快照存在；
5) 删除 `MedicalCase.PrescriptionId`（若存在），脚本回填验证后移除列与外键；
6) 回归测试：同日编辑/取消、合并经验方校验、折扣截断与打印、并发下唯一约束冲突处理。

## 风险与回滚
- 若唯一索引创建时发现脏数据（多诊断/多处方），需先数据清理与合并策略；
- 度量对报表与历史接口的影响，提供回滚脚本（索引/列恢复、数据回填）。
