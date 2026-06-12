---
type: concept
title: 内存缓存容量管理
created: 2026-06-11
updated: 2026-06-11
tags: [performance, caching, infrastructure]
related: [herb-cache-strategy, caching-strategy]
sources: ["docs/06-operations/02-configuration.md"]
---

# 内存缓存容量管理

## 定义

内存缓存容量管理是指通过配置参数限制服务端内存缓存的大小、压缩行为及过期扫描频率，以防止缓存无限膨胀导致服务进程内存溢出 (OOM)。

## 核心配置参数

在 `MemoryCache` 配置节中，系统定义了以下关键指标：

*   **SizeLimit**: 缓存占用的最大内存字节数（默认 100MB）。当缓存总量接近此限制时，系统将触发压缩机制。
*   **CompactionPercentage**: 压缩比例（默认 0.05，即 5%）。当需要释放空间时，系统将移除一定比例的过期或最少使用缓存项。
*   **ExpirationScanFrequencySeconds**: 过期键扫描频率（默认 60 秒）。定期清理已过期的缓存项，释放内存空间。
*   **DefaultExpirationMinutes**: 默认缓存过期时间（默认 5 分钟），适用于未指定具体过期时间的缓存项。

## 最佳实践

*   **容量评估**: 100MB 的限制需结合业务数据量进行评估。对于 [药材内存缓存](herb-cache-strategy.md) 等全量预加载场景，需监控实际占用情况，必要时调整上限。
*   **过期策略**: 合理设置 `DefaultExpirationMinutes` 平衡数据一致性与命中率。高频变动数据应设置较短的过期时间或采用主动失效机制。
*   **监控告警**: 建议集成内存使用率监控，当缓存命中率显著下降或内存占用持续高位时发出告警。

## 关联概念

*   [缓存策略](caching-strategy.md): 内存缓存是服务端缓存策略的具体实现之一。
*   [药材内存缓存策略](herb-cache-strategy.md): 药材模块重度依赖内存缓存以提升检索性能。