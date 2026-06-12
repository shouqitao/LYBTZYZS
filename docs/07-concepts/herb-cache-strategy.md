---
type: concept
title: 药材内存缓存策略
tags: [performance, caching, architecture, herbs]
related: [herb-module, IHerbCacheService, dual-mode-architecture]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/02-requirements/herbs.md"]
---

# 药材内存缓存策略

## 定义

**药材内存缓存策略**是指凌隐宝堂系统在 Desktop 客户端实现的针对中药材数据的全量预加载与内存索引机制。该策略旨在解决医生开方时药材检索的性能瓶颈，将查找耗时从秒级降低至毫秒级。

## 核心组件

*   **IHerbCacheService**：负责管理内存中的药材数据集合。
*   **数据结构**：
    *   `Dictionary<Guid, HerbDto>`：主键索引，用于快速根据 ID 获取详情。
    *   `Dictionary<string, List<HerbDto>>`：拼音前缀索引，支持快速模糊搜索（如输入 "DG" 匹配 "当归"）。
    *   `Dictionary<string, List<HerbDto>>`：分类索引，支持按功效分类（如 "补血药"）快速筛选。

## 工作机制

1.  **全量预加载**：
    *   在应用启动或用户登录后，Desktop 客户端从服务端（远程模式）或 LocalDbContext（本地模式）一次性拉取所有状态为 `Enabled` 的药材数据。
    *   数据加载到内存后，构建上述索引结构。

2.  **零延迟检索**：
    *   医生在开方界面输入关键词时，系统直接在内存中进行过滤和匹配，无需发起网络请求或数据库查询，实现 < 2 秒甚至毫秒级的响应速度。

3.  **缓存失效与更新策略 (HERB-D01)**：
    *   **增量更新**：当发生单条药材的 CRUD 操作时，同步更新内存中的对应条目。
    *   **全量重加载触发条件**：
        *   执行批量导入（Excel/JSON）完成后。
        *   切换运行模式（远程 <-> 本地）。
        *   数据同步模块完成双向同步后。
        *   用户闲置超过 30 分钟后重新激活。
        *   用户重新登录。

## 性能优势

*   **开方体验优化**：消除了网络延迟和数据库 I/O 开销，确保在弱网或高并发场景下开方流畅。
*   **资源占用可控**：常用中药材数量通常在 300-500 种，即使扩展至 2000 种，内存占用也极小（MB 级别），对客户端性能无显著影响。

## 相关决策

*   [[HERB-D01]]：确定采用 Desktop 全量预加载策略，替代服务端的 OutputCache，以适配离线场景。
*   [[dual-mode-architecture]]：缓存策略在远程和本地模式下均有效，本地模式下通过 LocalWebAPI 读取 SQL Server LocalDB 构建缓存。

## 参见

*   [[herb-module]]
*   [[IHerbCacheService]] (实体/服务占位)
*   [[clinical-workflow]]