# ADR-0015: API 版本控制策略

**状态**: Accepted
**日期**: 2026-06-28
**来源**: 01-system-overview.md API 版本控制策略节

## 背景

系统提供 RESTful API 供 Desktop 客户端和未来 Web 客户端调用。随着功能迭代和客户端版本分化，需要明确的 API 版本控制策略以保证向后兼容性。

## 决策

采用 **URL Path Versioning** (`/api/v{version}/`) 作为 API 版本化方案。

### 核心规则

| 规则 | 约束 | 说明 |
|------|------|------|
| 版本化方式 | URL Path | `/api/v1/` 前缀，`[ApiVersion("1")]` |
| 版本号格式 | 整数递增 | v1 → v2 → v3，无 v1.1 等子版本 |
| 触发条件 | 破坏性变更 | 删除/重命名端点、改变必需字段、改变语义 |
| 共存期 | ≥6 个月 | v1 和 v2 同时可用，客户端逐步迁移 |
| 弃用流程 | 标记 → 过渡 → 移除 | 先 `[Obsolete]` + 6 个月过渡期，再物理删除 |
| 新端点 | 允许在当前版本 | 非破坏性新功能直接加到当前版本，不递增版本号 |

### 弃用端点处理

1. 代码中标记 `[Obsolete("Use /api/v{new}/... instead. Deprecated since YYYY-MM-DD")]`
2. 文档中保留端点说明，标注 `[Deprecated]` + 替代方案
3. 监控弃用端点调用频率（通过审计日志）
4. 6 个月过渡期后移除

### 本地模式版本同步

本地 LocalWebAPI 与远程 WebAPI 使用相同版本号，但本地模式端点是远程的子集（约 80% 覆盖率）。版本变更时：
- 两端同步更新到相同版本
- 本地模式缺失的端点（如 Sync 相关）不纳入本地 API 契约

## 理由

- **简单性**: URL Path 是最直观的版本化方式，客户端和开发者都能一眼识别版本
- **兼容性**: 多版本共存期保证客户端平滑迁移
- **可测试性**: 每个版本可独立测试，不受其他版本影响
- **文档友好**: API 参考文档按版本组织，易于维护

## 后果

- URL 路径略长（`/api/v1/auth/login` vs `/api/auth/login`）
- 需要维护多版本端点代码（共存期内）
- 本地模式需与远程模式保持版本同步（由 ADR-0010 保障）

## 关联

- [ADR-0009: URL 驱动双模式](0009-url-driven-dual-mode.md) — SwitchingApiClient 依赖版本化 URL
- [ADR-0010: LocalWebAPI 统一服务层](0010-localwebapi-unified-service-layer.md) — 本地/远程 API 契约同步
- [04-api-reference/](../04-api-reference/) — API 端点文档，按版本组织

## 关联 US

- 全部 141 个 US 的 API 端点均受此版本控制策略约束
