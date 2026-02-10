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
- ConsultationRepository 仅提供只读查询 (BaseReadRepository)
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
- 业务规则复杂度超出 500 行 Service
- 团队规模超过 5 人需要独立开发
- 性能瓶颈需要读写分离

## 变更记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 初始决策 |
| 2025-12-15 | 废弃独立 Repository 接口 |
| 2026-01-05 | Desktop.Prescriptions 模块移除，功能迁入 MedicalCase |
