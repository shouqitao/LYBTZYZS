# STD-02: CorrelationId 追踪规范

## 适用范围

全系统 HTTP 请求链路追踪、日志关联。

## 规范内容

### 传播机制

HTTP 请求通过 `X-Correlation-Id` Header 传递关联标识:

1. **客户端发起**: Desktop HttpClient 在每个请求 Header 中附加 `X-Correlation-Id` (GUID)
2. **服务端接收**: CorrelationIdMiddleware 从 Header 提取，若缺失则自动生成
3. **日志写入**: 所有日志条目自动包含 CorrelationId 字段，支持全链路追踪

### 规则

| 规则 | 说明 |
|------|------|
| 所有日志必须包含 CorrelationId | 通过 Serilog Enricher 自动注入，无需手动传递 |
| 异步操作需手动传递 | Background Job/Timer 回调等脱离 HTTP 上下文的场景，需显式传递 CorrelationId |
| 客户端同一操作链共享同一 ID | 如"保存医案"触发多个 API 调用，使用同一 CorrelationId |
| 新操作链生成新 ID | 用户每次主动操作 (点击按钮) 生成新的 CorrelationId |

### 日志格式示例

```
[2026-02-26 10:30:15.123 INF] [CorrelationId:a1b2c3d4] MedicalCase saved successfully. CaseId=xxx
[2026-02-26 10:30:15.456 INF] [CorrelationId:a1b2c3d4] AuditLog created. OperationType=Update
```

### 不适用场景

- 本地模式不产生 HTTP 请求，CorrelationId 由 Desktop 内部生成用于日志关联
- 数据库迁移/种子数据等启动阶段操作使用固定前缀 `STARTUP-`

## 参考

- 日志规范: `docs/02-requirements/logging.md`
- 非功能需求: `docs/02-requirements/nfr.md`

---

创建日期: 2026-02-26
