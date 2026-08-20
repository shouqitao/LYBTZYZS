# MedicalCase 模块设计
> 版本: v1.0 | 日期: 2026-08-20

> 复杂度: 8/10 | 状态: 草稿

## 概述

MedicalCase 是系统核心聚合根，承载中医诊疗全流程：挂号→就诊→诊断→开方→打印→取药。

**职责**: 医案 CRUD、诊断记录(Consultation)+处方(Prescription)聚合管理、状态流转、与 Registration 联动。

**依赖**: 上游 Registration/Patients/Herbs，下游 Printing/Formula。

## 服务拆分（非 MediatR，A-03 定案）

| 接口 | 职责 |
|------|------|
| IMedicalCaseCommandService | 写操作（含 `.Deletion` partial） |
| IMedicalCaseQueryService | 读操作 |
| IMedicalCaseStateService | 状态变更 |
| IMedicalCaseReferenceRepository | 医案引用检查 |
| IMedicalCaseFacade | 组合以上三接口 |

## API 端点（关键）

| HTTP | 路由 | 权限 |
|------|------|------|
| POST | `/api/v1/medicalcases` | DoctorOnly |
| PUT | `/api/v1/medicalcases/{id}` | Doctor(自己的)/Admin |
| DELETE | `/api/v1/medicalcases/{id}` | Doctor/Admin |
| PUT | `/api/v1/medicalcases/{id}/status` | Doctor/Admin |
| PUT | `/api/v1/medicalcases/{id}/close` | Doctor(自己的) |
| PUT | `/api/v1/medicalcases/{id}/cancel` | Doctor(自己的) |

## 状态机

> **权威定义**: 见 [07-medical-cases.md「状态机」](../../02-requirements/07-medical-cases.md) §状态机 (L107-121) 及 [04-data-model.md「MedicalCase 状态机」](../../03-architecture/04-data-model.md) L119-123。

## 业务规则

| 规则 | 描述 |
|------|------|
| BR-001 | 同一患者同时只能有一个未完成医案 |
| BR-003 | Complete 前必须设置 NeedsPrescription + 处方 + TcmDiagnosis |
| BR-MC-LOCK | Completed 同日可编辑，次日锁定 |
| BR-004 | Admin 可跳过工作流验证 |
| 编号 | MC{yyyyMMdd}{seq:3}，RX{yyyyMMdd}{seq:4} |
| 取消 | 物理删除，级联清除聚合（2026-08-03 决策） |
| 联动 | Complete→Registration.Completed，Cancel→Registration.Cancelled |

## 聚合保存

PUT 保存时：更新 MedicalCase + Consultation + Prescription + Items，乐观锁重试（最多3次）。

## 架构违规（待修复）

- MedicalCase csproj 直接 ProjectReference Registration（违反模块隔离），应通过接口解耦
