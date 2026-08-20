# Registration 模块设计
> 版本: v1.0 | 日期: 2026-08-20

> 复杂度: 6/10 | 状态: 草稿

## 概述

Registration 管理挂号流程，连接前台/医生与医案。支持前台挂号（Source=Receptionist）和医生快速接诊（Source=Doctor）。

**职责**: 挂号 CRUD、待诊队列管理、快速接诊、与 MedicalCase 联动。

**依赖**: 上游 Patients+Users，下游 MedicalCase。

## API 端点

| HTTP | 路由 | 权限（代码实际） |
|------|------|------|
| POST | `/api/v1/registrations` | DoctorOrAdminOrReceptionist |
| GET | `/api/v1/registrations/{id}` | DoctorOrAdminOrReceptionist |
| GET | `/api/v1/registrations` | DoctorOrAdminOrReceptionist |
| GET | `/api/v1/registrations/queue` | DoctorOrAdminOrReceptionist |
| PUT | `/api/v1/registrations/{id}/start-visit` | DoctorOrAdminOrReceptionist |
| PUT | `/api/v1/registrations/{id}/cancel` | DoctorOrAdminOrReceptionist |

## 两源模型

| 属性 | 前台模式(Receptionist) | 医生模式(Doctor) |
|------|----------------------|------------------|
| 初始状态 | Waiting（排队） | Waiting→start-visit→InProgress |
| 是否进入队列 | 是 | 是（两步改造后） |
| 医案创建时机 | 医生接诊时 | StartVisit 时（接诊即建） |
| 取消策略 | 前台手动取消 | CancelAsync 医案自动闭环 |

## 状态机

> **权威定义**: 见 [04-data-model.md「Registration 状态机」](../../03-architecture/04-data-model.md) L268-272 及 [07-medical-cases.md「状态机」](../../02-requirements/07-medical-cases.md) §状态机。

## 接诊即建

`Waiting → InProgress` 时**原子创建** MedicalCase(Active) + Registration(InProgress)。医案取消（物理删除）→ Registration→Cancelled。

## 业务规则

| 规则 | 描述 |
|------|------|
| REG-BR-001 | 仅 Status=Waiting 可取消；有活跃医案时拒绝 |
| REG-BR-002 | Source=Receptionist 仅 Receptionist 可取消 |
| REG-BR-007 | 当天重复挂号检查 |
| REG-BR-008 | 前台仅退当天 Waiting 挂号 |
| REG-BR-009 | 挂号费跟医生相关，创建时快照 |
| REG-BR-011 | 开始看诊并发保护 |

## 已知问题

- MedicalCase 模块直接使用 IRegistrationRepository（而非 IRegistrationService）
- SignalR 未实现，待诊队列通过客户端轮询(30秒)刷新
