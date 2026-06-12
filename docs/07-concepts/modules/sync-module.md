---
type: module
title: 数据同步模块
tags: [module, sync, dual-mode]
created: 2026-06-10
updated: 2026-06-10
source: docs/02-requirements/sync.md
---

## 概述

数据同步模块实现本地模式 (SQLite) 与远程服务端 (SQL Server) 之间的双向数据同步，基于 SHA256 Checksum 比对差异并支持冲突手动解决。该模块是双模式架构闭环的关键一环，支撑医生外出看诊离线工作后数据可靠回传。

## 核心能力

| 能力 | 说明 |
|------|------|
| SHA256 差异检测 | 通过 SHA256 Checksum 比对本地与服务端数据，识别 LocalOnly / ServerOnly / Modified / Identical 四种差异类型 |
| 双向同步 | 本地变更上传到服务端 + 服务端变更下载到本地，确保两端数据一致性 |
| 冲突手动解决 | 左右对比 UI，用户逐条选择保留本地版本 / 使用服务端版本 / 跳过，避免自动覆盖导致医疗数据丢失 |
| 依赖排序 | 按实体依赖关系顺序同步 (Herb/Patient/Formula → MedicalCase)，保证外键引用完整性 |
| 幂等跳过 | Checksum 比对已同步数据，自动跳过无需重复同步的实体 |

**同步范围:** Herb (药材)、Patient (患者)、Formula (验方)、MedicalCase (医案，含 Consultation + Prescription + Items)

**同步流程:**
```
进入同步模块 → 加载实体类型 → 选择要同步的类型 → 检查差异 (SHA256)
    → 展示差异列表 (LocalOnly/ServerOnly/Modified)
    → 冲突逐条解决 (保留本地/使用服务端/跳过)
    → 执行同步 (上传+下载) → 结果汇总 (按实体类型分组)
失败时 → 显示错误摘要 → 重新同步 (Checksum 幂等跳过已同步数据)
```

## 角色权限

| 角色 | 权限 |
|------|------|
| SuperAdmin | 全部同步操作 |
| Admin | 全部同步操作 |
| Doctor | 全部同步操作 |
| Receptionist | 无权限 |

> 端点受 `DoctorOrAdmin` 策略保护。

## 关键业务规则

1. **SHA256 Checksum 差异检测**: 每个实体计算 SHA256 校验和，通过比对本地与服务端的 checksum 识别数据是否发生变更
2. **四种差异类型**: LocalOnly (仅本地存在)、ServerOnly (仅服务端存在)、Modified (两端均修改)、Identical (完全一致，跳过)
3. **冲突手动解决**: 医疗数据冲突必须人工确认，禁止自动覆盖，提供左右对比 UI 供用户逐条选择
4. **依赖顺序同步**: 基础数据 (Herb/Patient/Formula) 优先于关联数据 (MedicalCase) 同步，保证外键引用完整性
5. **幂等性保证**: 通过 Checksum 比对自动跳过已同步数据，重复执行同步不会产生重复记录
6. **模式切换检查**: 远程/本地模式切换前检查未同步变更，防止数据孤岛

## 相关链接

- [[herb]] - 药材基础数据同步
- [[patient]] - 患者信息同步
- [[formula]] - 验方数据同步
- [[medical-case]] - 医案数据同步 (含 Consultation + Prescription + Items)
- [[dual-mode-architecture]] - 双模式架构设计
