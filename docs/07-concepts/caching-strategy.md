---
type: concept
title: 缓存策略与失效机制
tags: [performance, caching, consistency, architecture]
related: [herb-cache-strategy, dual-mode-architecture]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/02-requirements/nfr.md"]
---
# 缓存策略与失效机制

## 概述

为解决多端数据一致性与性能平衡问题，系统采用了分层缓存策略，包括服务端 OutputCache/MemoryCache 和客户端 ApiService/SearchCache。核心原则是**“TTL + 主动失效”**双保险机制，确保数据修改后下次查询能立即获取最新数据，同时保持极低的内存开销。

## 服务端缓存 (Server-Side)

### 1. OutputCache 策略
基于 ASP.NET Core OutputCache 中间件，按标签（Tag）分组管理。

| 缓存策略 | 过期时间 (TTL) | 标签 (Tag) | 挂载端点 |
|----------|---------------|------------|---------|
| HerbsCache | 30 分钟 | `herbs` | GET /api/v1/herbs |
| FormulasCache | 2 小时 | `formulas` | GET /api/v1/formulas |
| PatientsCache | 30 分钟 | `patients` | GET /api/v1/patients |
| MedicalCaseCache | 20 分钟 | `medicalcases` | GET /api/v1/medicalcases |
| UserPermissionsCache | 10 分钟 | `permissions` | GET /api/v1/users |
| Default | 5 分钟 | - | 全局兜底 |

### 2. MemoryCache 配置
*   **SizeLimit**: 100 MB (硬上限)
*   **CompactionPercentage**: 5%
*   **ExpirationScanFrequency**: 60 秒
*   **实际占用**: 估算 < 7 MB (诊所数据量小)

### 3. 主动失效映射表
写操作成功后，调用 `IOutputCacheStore.EvictByTagAsync(tag)` 清除受影响缓存。

| 模块 | 写操作 | 清除标签 | 原因 |
|------|--------|---------|------|
| Patient | CRUD / 状态切换 | `patients` | 患者列表更新 |
| Herb | CRUD / 状态切换 | `herbs` | 药材列表更新 |
| Formula | CRUD | `formulas` | 验方列表更新 |
| MedicalCase | 创建/完成 | `medicalcases`, `patients` | 医案更新且患者统计变更 |
| MedicalCase | 其他写操作 | `medicalcases` | 医案列表更新 |
| User | CRUD / 角色变更 | `permissions` | 权限列表更新 |

## 客户端缓存 (Desktop Client)

### 1. ApiService GET 缓存
*   **容量**: 1000 条逻辑单位
*   **过期**: 5 分钟绝对过期
*   **键格式**: `GET:{url}`
*   **失效规则**: 写操作 (POST/PUT/DELETE) 成功后，按模块前缀清除相关 GET 缓存。
    *   Patient 写 -> 清除 `GET:*/patients*`
    *   MedicalCase 写 -> 清除 `GET:*/medicalcases*` 和 `GET:*/patients*`

### 2. PatientSearchCache (专用 LRU)
*   **容量**: 10 条
*   **过期**: 5 分钟
*   **失效**: 患者写操作后调用 `Invalidate()`，会话切换时自动清理。

## 内存占用估算

| 缓存层 | 上限 | 典型占用 | 说明 |
|--------|------|---------|------|
| Server OutputCache | TTL 自然淘汰 | < 2 MB | 用户少，查询组合有限 |
| Server MemoryCache | 100 MB | < 5 MB | 数据量百~千级 |
| Desktop ApiService | 1000 条 | < 2 MB | 单用户实际缓存少 |
| Desktop SearchCache | 10 条 | < 0.1 MB | 极小 |
| **总计** | - | **< 10 MB** | 可忽略不计 |

## 相关链接

- [药材缓存策略](herb-cache-strategy.md)
- [双模式架构](dual-mode-architecture.md)