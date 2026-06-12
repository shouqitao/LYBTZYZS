---
type: module
title: 药材管理模块
tags: [module, herb, prescription]
created: 2026-06-10
updated: 2026-06-10
source: docs/02-requirements/herbs.md
---

## 概述

药材管理模块负责中药材库的完整生命周期管理，包括基本信息维护、分类、价格管理、启用/禁用状态控制、批量导入导出及引用安全检查。该模块是处方开方的基础依赖——没有药材数据，开方功能无法使用，因此是系统上线的前置条件。

## 核心能力

| 能力 | 说明 |
|------|------|
| 药材 CRUD | 创建/查看/更新/软删除/恢复，自动生成拼音码 |
| 状态管理 | 启用/禁用切换（单个 + 批量），禁用药材开方时不可选 |
| 批量操作 | Excel 导入、JSON 批量导入（最多 10000 条）、Excel/JSON 导出、批量删除 |
| 引用安全 | 删除前检查 PrescriptionItem / FormulaItem 引用，有引用则禁止删除并建议禁用 |
| 内存缓存 | Desktop 全量预加载到内存（IHerbCacheService），开方时 0ms 纯内存过滤 |
| 价格快照 | 处方创建时记录药材单价，历史处方金额不受后续改价影响 |

## 角色权限

| 角色 | 权限 |
|------|------|
| SuperAdmin | 全部药材 CRUD |
| Admin | 全部药材 CRUD |
| Doctor | 创建药材；编辑/删除/启用/禁用自己创建的药材；查看全部药材 |
| Receptionist | 无权限 |

> Update/Delete/ToggleStatus 端点包含所有权检查：Admin/SuperAdmin 可操作全部，Doctor 仅可操作自己创建的数据。

## 关键业务规则

1. **引用保护**：删除前检查处方（PrescriptionItem）和验方（FormulaItem）引用，有引用则拒绝删除，建议使用禁用功能
2. **软删除机制**：药材删除采用软删除（IsDeleted=true），支持恢复操作
3. **所有权校验**：Doctor 角色仅能编辑/删除/启用/禁用自己创建的药材，Admin/SuperAdmin 可操作全部
4. **内存缓存策略**：Desktop 端启动时全量预加载药材到 IHerbCacheService，开方检索时纯内存过滤，避免频繁 HTTP 请求
5. **价格快照机制**：处方创建时记录药材当时单价，确保历史处方金额不被后续价格调整篡改
6. **双模式支持**：远程模式（HTTP API + SQL Server）和本地模式（SQLite + NPOI 本地解析）均完整支持
7. **批量导入上限**：JSON 批量导入最多支持 10000 条，Excel 导入默认上限由配置决定

## 相关链接

- [[herb]] - 药材实体定义
- [[formula]] - 验方模块（引用药材）
- [[prescription]] - 处方模块（引用药材）
- [[medical-case]] - 医案模块（通过处方间接依赖药材）
