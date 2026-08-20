# ADR-0020: Desktop Service Layer Error Contract

## 状态
**提议** — 2026-08-19

## 背景

L1 Service 层错误处理三态并存：
1. **CommandResult.Failed**（UI 友好，Service 层主流）
2. **返回 null**（Lifecycle 的 CloseCase/Suspend/UpdateStatus 用 `try/catch → return null`）
3. **throw InvalidOperationException**（Repository 层 CRUD 失败）

同一 Service 内三种模式混用，调用方无法预知失败行为，是前几轮分层/错误语义反复踩坑的根源。

## 决策

制定 **Service 边界错误契约**：

### 读操作
- **返回 null 或空集合**，不抛异常
- 调用方检查 null/空后走正常逻辑
- 基础设施故障（网络/序列化）在 Repository 层捕获，Service 层不暴露

### 写操作 / 状态变更
- **返回 CommandResult**（Success/Failed + FailureReason + Message）
- 调用方根据 CommandResult 决定 UI 反馈
- 不抛异常

### 允许抛异常的情况（仅以下）
- **ArgumentException**：参数非法（调用方必须修复）
- **InvalidOperationException**：基础设施彻底失效（调用方确能处理，如登录时网络完全不可达）

### 异常传播规则
- Repository 层异常被 `ExecuteAsync<T>` 统一包装
- Service 层只在「参数非法」和「基础设施彻底失效」时抛出
- 其余情况一律返回 CommandResult 或 null

## 实施计划

| 批次 | 内容 |
|------|------|
| Batch 1 | 更新 `docs/03-architecture/02-desktop.md` §Desktop 分层规则 补充错误契约 |
| Batch 2 | 扫描所有 Service 方法，分类当前错误模式 |
| Batch 3 | 逐个 Service 对齐契约（throw → CommandResult / return null） |
| Batch 4 | 测试验证（错误场景覆盖） |

## 后果

- ✅ 消除调用方对错误语义的三态猜测
- ✅ 统一 UI 错误反馈模式
- ⚠️ 需要逐个 Service 迁移（改动面中等）
- ⚠️ 部分 ViewModel 需要适配新的错误返回格式
