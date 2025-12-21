# Tasks: adopt-activity-api-tracing

**Total Phases**: 4
**Estimated Complexity**: Medium
**Status**: ✅ 全部完成 - 可归档

---

## 验证结果

经检查，此提案的目标已在之前的工作中实现。

### Phase 1: 基础设施准备 ✅

- [x] TraceContext.cs已存在于LYBT.Shared.Logging
  - 实现CurrentTraceId属性（Activity.Current?.TraceId）
  - 实现TraceIdOrNew属性（回退到Guid）
  - 实现StartActivity方法
  - 实现HasActiveTrace属性

- [x] ActivityCorrelationIdProvider已实现
  - 基于System.Diagnostics.Activity
  - 实现ICorrelationIdProvider接口
  - 用于Serilog enricher

- [x] Serilog配置已集成
  - DesktopSerilogConfiguration使用ActivityCorrelationIdProvider
  - 日志输出包含CorrelationId（实际为TraceId）

### Phase 2: 迁移使用处 ✅

- [x] ViewModelBase已迁移
  - HandleApiExceptionAsync使用TraceContext.TraceIdOrNew（line 199）
  - HandleError使用TraceContext.TraceIdOrNew（line 323）

- [x] ClientErrorMessageMapper已迁移
  - GetSafeMessageWithTrackingCode使用TraceContext.TraceIdOrNew（line 351）
  - GetFullTrackingCode使用TraceContext.TraceIdOrNew（line 362）

- [x] 其他使用处已清理
  - grep扫描确认无CorrelationIdContext.使用

### Phase 3: 删除旧代码 ✅

- [x] CorrelationIdContext.cs已删除
  - 原位置: LYBT.Desktop.Foundation/Logging/
  - 状态: 目录已不存在

- [x] CorrelationIdDelegatingHandler.cs已删除
  - 原位置: LYBT.Desktop.Infrastructure/Http/
  - 状态: 文件已不存在

- [~] ICorrelationIdProvider接口保留
  - 用于Serilog enricher和Server端HttpContext
  - ActivityCorrelationIdProvider基于Activity API实现

### Phase 4: HttpClient配置优化 ✅

- [x] 自定义Handler已移除
  - CorrelationIdDelegatingHandler已删除
  - HttpClient使用默认W3C TraceContext传播

---

## Completion Criteria ✅

- [x] 所有日志包含有效TraceId（通过ActivityCorrelationIdProvider）
- [x] HTTP请求自动携带traceparent头（.NET 8默认行为）
- [x] Desktop自定义CorrelationId类已删除
- [x] 编译通过，无警告
- [x] 使用TraceContext.TraceIdOrNew替代旧API

---

## 归档说明

此提案的目标已通过以下方式实现：

1. **TraceContext.cs** - 提供统一的Activity API访问
2. **ActivityCorrelationIdProvider** - ICorrelationIdProvider的Activity实现
3. **代码迁移** - ViewModelBase/ClientErrorMessageMapper已使用TraceContext
4. **旧代码删除** - Desktop端CorrelationIdContext/DelegatingHandler已删除

**保留的抽象**:
- ICorrelationIdProvider接口保留用于Serilog enricher兼容性
- Server端HttpContextCorrelationIdProvider保留用于HTTP上下文追踪

---

## 统计摘要

| 指标 | 数值 |
|------|------|
| 已删除文件 | 2个 (Desktop端) |
| 已修改文件 | 2个 (ViewModelBase, ClientErrorMessageMapper) |
| 新增文件 | 2个 (TraceContext.cs, ActivityCorrelationIdProvider.cs) |
| 保留抽象 | ICorrelationIdProvider (兼容性) |
