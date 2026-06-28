# ADR-0001: MedicalCase 作为唯一聚合根

**状态**: 已采纳
**日期**: 2025-12-04
**来源**: ADR-003, ADR-005, ADR-006, ADR-008

## 背景

系统核心是诊疗流程: 患者 -> 医案 -> 诊断 -> 处方。MedicalCase、Consultation、Prescription 三者存在强一致性约束，需要作为整体管理。

## 决策

MedicalCase 是系统唯一的 DDD 聚合根:
- Consultation 和 Prescription 是 MedicalCase 的内部实体
- 所有对 Consultation/Prescription 的操作必须通过 MedicalCase 聚合进行
- 禁止为 Consultation/Prescription 创建独立的 Repository
- 跨聚合引用使用 ID (如 PatientId, UserId)

## 执行规则

### Server 端
- MedicalCaseRepository 负责聚合整体的 CRUD
- ConsultationRepository 仅提供只读查询（通过 `IMedicalCaseQueryService` 的查询方法访问，无独立 Repository 实体）
- 无独立的 PrescriptionRepository

### Desktop 端
- 无 IConsultationRepository / IPrescriptionRepository
- Consultation/Prescription 操作通过 MedicalCaseDataManager 协调
- CommandHandler 模式处理子实体命令

### API 端
- 已废弃的 9 个绕过聚合的 API 端点已删除
- 所有写操作通过 MedicalCases Controller

## 演进触发条件

当出现以下情况时可考虑拆分:
- MedicalCaseService 超过 800 行（当前约 600 行）
- 咨询/处方需要独立于医案的生命周期（如独立的处方库）
- 团队规模超过 5 人需要独立开发
- 读写比超过 8:2 且读性能成为瓶颈

## 变更记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 初始决策 |
| 2025-12-15 | 废弃独立 Repository 接口 |
| 2026-01-05 | Desktop.Prescriptions 模块移除，功能迁入 MedicalCase |
| 2026-02-21 | MedicalCaseModel 从贫血模型演进为充血模型: 新增 `Complete()`, `Suspend()` (原名 `SaveAsDraft()`，MC-D20 重命名), `SoftDelete()`, `UpdateConsultation()` 域方法; 移除 `Cancelled` 枚举值 (取消=软删除); 新增 MedicalCaseServiceHelper 提取共享代码 |
| 2026-06-25 | 修正 ADR-0001: 更新 BaseReadRepository 引用为当前实际查询接口 |

## 关联 US

- **US-MC-001 ~ US-MC-019**（医案管理全部 19 项，聚合根是其执行基础）
- **US-REG-005 / US-REG-007**（挂号-医案联动：开始就诊创建医案、完成/取消自动回写）
- **US-REG-008**（SignalR 推送：挂号状态变更通过聚合根触发推送）
- **US-PRINT-001 / US-PRINT-004**（打印保护字段挂载在 MedicalCase 聚合根）
- **US-PAT-005 / US-PAT-008 / US-PAT-009 / US-PAT-010**（删除引用检查指向 MedicalCase）
- **US-HERB-005 / US-HERB-008 / US-HERB-009**（删除引用检查指向 PrescriptionItem）
- **US-REPORT-001/002/003**（报表查询依赖 MedicalCase 聚合数据）
- **D1**（医案审计日志：聚合根状态变更触发审计记录）
- **D2**（打印回写：IsPrinted/PrintVersion 字段在聚合根上）
- **D9**（历史聚合：跨医案查询依赖聚合根数据结构）
