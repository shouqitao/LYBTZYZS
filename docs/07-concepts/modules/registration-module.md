---
type: module
title: 挂号管理模块 (Registration Module)
tags: [module, registration, patient, medical-case]
created: 2026-06-10
updated: 2026-06-10
source: docs/02-requirements/registration.md
---

## 概述

挂号管理模块为中医诊所提供系统化的患者分流和排队机制，支持前台挂号和医生直接看诊两种入口模式。每次就诊都创建对应的 Registration 记录，实现 100% 就诊可追溯，并为运营报表提供日均就诊量和平均等待时长等关键指标。

## 核心能力

| 能力 | 说明 |
|------|------|
| **前台挂号** | Receptionist 查询/创建患者，指派医生，进入等待队列 |
| **医生直接看诊** | Doctor 选择患者，系统后台静默创建 Registration（Source=Doctor）+ MedicalCase |
| **等待队列** | 按挂号时间升序展示 Waiting 状态的挂号记录，医生按序接诊 |
| **状态自动联动** | Registration 状态跟随 MedicalCase 状态自动流转，减少手动操作 |
| **取消分流** | 根据 Source 执行不同取消策略：前台手动取消 vs 医生自动取消 |

## 角色权限

| 角色 | 权限范围 | 主要操作 |
|------|---------|---------|
| **Receptionist** | 创建挂号、取消挂号、查看全部队列 | 患者到达时创建挂号记录，指派医生 |
| **Doctor** | 查看个人队列、从队列接诊、直接看诊 | 按序接诊或跳过挂号直接看诊 |
| **Admin** | 查看全部队列和历史（只读） | 统计就诊量，运营监控 |
| **SuperAdmin** | 与 Admin 相同（只读） | 系统级监控，不参与具体操作 |

## 关键业务规则

### 两种入口模式

**前台模式（Source=Receptionist）**：
1. 前台查询患者（不存在则提示创建）
2. 选择患者，指派医生
3. 创建 Registration（状态：Waiting）
4. 医生从队列选中 → 自动创建 MedicalCase → Registration（InProgress）
5. 医案完成 → Registration（Completed）
6. 或医案取消 → Registration 回退（Waiting）→ 前台手动取消（Cancelled）

**医生模式（Source=Doctor）**：
1. 医生查询患者（不存在则提示创建）
2. 选择患者 → 系统自动创建 Registration（InProgress）+ MedicalCase
3. 医生无感知 Registration 存在（后台静默创建）
4. 医案完成 → Registration（Completed）
5. 或医案取消 → Registration（Cancelled）自动闭环

### 状态生命周期

```
前台模式: (创建) -> Waiting -> InProgress -> Completed
                       |
                       v
                   Cancelled

医生模式: (创建) -> InProgress -> Completed
                       |
                       v
                   Cancelled
```

### 自动联动规则

- Registration 创建时自动生成对应的 MedicalCase（医生模式下两者同时创建）
- MedicalCase 状态变更时，Registration 状态同步更新：
  - MedicalCase.InProgress → Registration.InProgress
  - MedicalCase.Completed → Registration.Completed
  - MedicalCase.Cancelled → Registration.Cancelled（前台模式需回退到 Waiting）

### 业务指标

| 指标 | 目标 | 衡量方式 |
|------|------|---------|
| 挂号使用率 | 100% 就诊有 Registration 记录 | COUNT(Registration) / COUNT(MedicalCase) = 1.0 |
| 平均等待时间 | < 15 分钟（Waiting → InProgress） | AVG(InProgress时间 - CreatedAt) WHERE Source=Receptionist |
| 取消率 | < 10% | COUNT(Cancelled) / COUNT(Total) |

## 相关链接

- [[registration]] - 挂号实体定义和状态枚举
- [[patient]] - 患者模块，挂号记录的关联主体
- [[medical-case]] - 医案模块，与挂号记录自动联动创建
- [[user]] - 用户模块，医生和前台角色的权限来源
