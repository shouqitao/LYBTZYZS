---
type: concept
title: 医案锁定规则
created: 2026-06-10
updated: 2026-06-10
tags: [business-rules, medical-case, data-integrity, audit]
related: [medical-case, business-rules, resource-level-permissions]
sources: ["docs/01-product/user-roles.md"]
---

# 医案锁定规则

医案锁定规则是一组基于医案完成状态和时间限制其编辑权限的业务规则，旨在确保数据完整性和审计追踪。

## 锁定条件

医案的锁定状态由以下公式决定：
`IsLocked = IsCompleted && (CompletedAt.Date != Today)`

即：医案已完成，且完成日期不是今天。

## 权限影响

锁定状态对不同角色的编辑权限产生不同影响：

| 场景 | 医生（Doctor） | 管理员（Admin） |
|------|----------------|-----------------|
| 当天自己的未完成医案 | 可编辑 | 可编辑 |
| 当天自己的已完成医案 | 可编辑（当天内） | 可编辑 |
| 隔天自己的已完成医案 | **不可编辑（已锁定）** | 可编辑（需填写修改原因） |
| 他人的医案 | 不可查看/编辑 | 可查看/编辑（需填写修改原因） |

## 审计要求

与锁定规则紧密相关的是审计要求，规定了在何种情况下修改医案需要提供理由：
- 隔天修改已完成的医案。
- 修改非本人创建的医案。
- 修改已完成的医案。
- 取消医案（可能需要）。

## 设计目的

此规则平衡了医生的日常工作灵活性（当天可自由修改）与数据的长期完整性和可追溯性（隔天修改需审计），是[[business-rules]]的核心组成部分。

## 相关概念

- [[medical-case]]：锁定规则直接作用于医案实体。
- [[resource-level-permissions]]：锁定规则是资源级权限的关键实现。
- [[business-rules]]：锁定规则是系统关键业务规则之一。