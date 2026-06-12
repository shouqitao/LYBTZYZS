---
type: module
title: 医案管理模块
tags: [module, medical-case, ddd, cqrs]
created: 2026-06-10
updated: 2026-06-10
source: docs/02-requirements/medical-cases.md
---

# 医案管理模块

## 概述

医案管理是中医诊所管理系统的核心模块，采用 DDD 聚合根模式设计。MedicalCase 作为系统唯一的聚合根，统一管理诊断 (Consultation) 和处方 (Prescription) 的创建、更新和状态流转。模块采用 CQRS 模式分离读写操作，通过状态机管理医案从创建到完成的全生命周期。

该模块覆盖中医诊疗的核心业务：四诊合参 (望闻问切)、辨证论治、处方开具、价格计算、打印保护等操作，同时保障数据完整性和操作可追溯性。

## 核心能力

| 能力 | 说明 | 技术实现 |
|------|------|----------|
| 聚合根管理 | MedicalCase 统一管理诊断和处方的 CRUD | 一次性保存 MedicalCase + Consultation + Prescription + Items |
| 状态机 | Active → Suspended / Completed / Cancelled | 状态转换受权限和时间锁定规则约束 |
| 打印保护 | 打印后修改需提供 EditReason，自动递增 PrintVersion | `IsPrinted` 标记 + `PrintVersion` 递增 |
| 审计追踪 | 19 个字段的自动 diff 变更记录 | MedicalCaseAuditLog 记录操作者、时间、变更内容 |
| 自动价格计算 | 单价 x 剂量 x 帖数 x 折扣 | 消除手动计算错误，准确率 100% |
| 验方导入 | 一键导入常用验方或历史处方 | ReferencedFormulas JSON 数组记录来源 |
| 权限控制 | 基于角色 + 资源所有权 + 时间锁定 | Doctor/Admin/SuperAdmin 分级权限 |

## 角色权限

| 角色 | 权限 |
|------|------|
| SuperAdmin | 查看/编辑全部医案，无时间限制 |
| Admin | 查看/编辑全部医案，无时间限制 |
| Doctor | 创建医案；查看/编辑自己的未完成医案；已完成医案当天可编辑 (需 EditReason)，隔天 403 |
| Receptionist | 仅可查看未完成医案简要提示 (创建时间 + 主治医生，不含诊断/处方详情) |

> 创建/编辑/完成/取消等写操作受 `DoctorOrAdmin` 策略保护。创建医案仅限 Doctor (`[Authorize(Roles = "Doctor")]`)。

## 关键业务规则

### 状态机

```
创建医案 → Active (进行中)
  ├─ 填写诊断 (Consultation)
  ├─ 标记处方需求 (NeedsPrescription)
  ├─ 开具处方 (Prescription + Items)
  ├─ 挂起 → Suspended (医生暂时离开) → 恢复 → Active
  ├─ 完成 → Completed (锁定规则: 隔天自动锁定)
  └─ 取消 → Cancelled (IsDeleted=true 软删除)
```

| 状态 | 值 | 说明 | 允许操作 |
|------|-----|------|----------|
| Active | 1 | 进行中 (初始状态) | 编辑、挂起、完成、取消 |
| Suspended | 0 | 已挂起 | 恢复、完成、取消 |
| Completed | 2 | 已完成 | Doctor: 当天可编辑 (需 EditReason), 隔天 403; Admin/SuperAdmin: 可编辑 (需 EditReason) |

> 取消操作统一通过 `IsDeleted=true` 软删除实现。已完成的医案不可取消。

### 业务规则

| 规则编号 | 名称 | 说明 |
|----------|------|------|
| BR-001 | 诊断必填 | 完成医案时 TcmDiagnosis (中医辨证) 为必填字段 |
| BR-002 | 处方锁定 | 处方打印后修改必须提供 EditReason，PrintVersion 递增 |
| BR-003 | 隔天锁定 | CompletedAt.Date < Today 的医案对 Doctor 角色只读 (返回 403) |

### 打印保护机制

- **IsPrinted**: 聚合根级标记，打印后任何内容修改需提供 `EditReason`
- **PrintVersion**: 内容变更时递增，用于打印溯源
- **PrintCount**: 跨 PrintType 总计打印次数
- **LastPrintedAt**: 最后打印时间

### 审计日志

MedicalCaseAuditLog 记录以下信息:
- OperatorId / OperatorName / OperatorRole: 操作者信息
- OperationTime: 操作时间
- ChangedFields: 变更字段列表 (旧值/新值)
- EditReason: 编辑理由 (打印后修改或已完成医案编辑时必填)

### 处方数据结构

**Prescription 关键字段:**

| 字段 | 类型 | 说明 |
|------|------|------|
| PrescriptionNumber | string(20) | 处方编号 (RX-YYYYMMDD-NNNN) |
| DosageCount | int | 帖数 (默认 7) |
| Discount | decimal(3,2) | 折扣 (范围 0.00~1.00，默认 1.0) |
| ReferencedFormulas | string(1000) | JSON 数组，记录验方/历史处方导入来源 |

**PrescriptionItem 关键字段:**

| 字段 | 类型 | 说明 |
|------|------|------|
| HerbId | Guid | 药材ID |
| HerbName | string(100) | 药材名称 |
| Dosage | int | 剂量 (数值部分) |
| Unit | string(16) | 单位 (克/g/ml/条/粒 等) |
| DecocteMethod | DecocteMethod | 煎法 (Normal/DecocteFirst/DecocteLater 等 7 种) |
| UnitPrice | decimal(18,2) | 单价 (元/单位) |
| Amount | decimal(18,2) | 小计 (计算属性: UnitPrice x Dosage) |

**煎法枚举 (DecocteMethod):**

| 值 | 名称 | 打印标注 |
|----|------|---------|
| Normal (0) | 水煎 | (无) |
| DecocteFirst (1) | 先煎 | 先煎 |
| DecocteLater (2) | 后下 | 后下 |
| WrapDecoction (3) | 包煎 | 包煎 |
| SeparateDecoction (4) | 另炖 | 另炖 |
| MeltIn (5) | 烊化 | 烊化 |
| TakeWithDecoction (6) | 冲服 | 冲服 |

## 相关链接

- [[medical-case]] - 医案实体总览
- [[consultation]] - 诊断子实体
- [[prescription]] - 处方子实体
- [[patient]] - 患者管理模块
- [[herb]] - 药材管理模块
- [[formula]] - 验方管理模块
- [[ADR-001-medicalcase-aggregate-root]] - MedicalCase 作为唯一聚合根的架构决策
