# ADR-0021: SwitchingApiClient Client Lifecycle Management
> 版本: v1.0 | 日期: 2026-08-20

## 状态
**已实施** — 2026-08-19（实现见任务书 high-priority-fixes，commit c56ec0893）

## 背景

`SwitchingApiClient` 在模式切换时创建新 client（`HttpClientApiClient` 或 `RefitApiClient`），但旧 client 从不 Dispose。两个实现类均未实现 `IDisposable`，导致 `_current as IDisposable` 恒 null。

模式切换后旧 client 的 HttpClient/底层 socket/Refit 实例被弃置不回收。多模式频繁切换会累积资源（连接池/socket），长期驻留进程有泄漏风险。

## 决策

让 `HttpClientApiClient` 和 `RefitApiClient` 实现 `IDisposable`：

### HttpClientApiClient
```csharp
public sealed class HttpClientApiClient : IApiClient, IDisposable
{
    // Dispose 时不释放 IHttpClientFactory（由 DI 容器管理）
    // 但释放已创建的惰性子接口实例持有的资源
    public void Dispose() { /* 清理子接口实例 */ }
}
```

### RefitApiClient
```csharp
public sealed class RefitApiClient : IApiClient, IDisposable
{
    // Dispose 时不释放 HttpClient（由外部 handler chain 管理）
    // 但释放 Refit 生成的代理实例
    public void Dispose() { /* 清理 Refit 代理 */ }
}
```

### SwitchingApiClient
```csharp
// 慢路径（URL 变化时）
var oldClient = _current as IDisposable;
_current = newClient; // 先赋值，再释放旧的
oldClient?.Dispose();
```

## 实施计划

| 批次 | 内容 |
|------|------|
| Batch 1 | `HttpClientApiClient` + `RefitApiClient` 加 `IDisposable` |
| Batch 2 | `SwitchingApiClient` 慢路径加 Dispose 调用 |
| Batch 3 | 测试验证（模式切换后旧 client 被释放） |

## 后果

- ✅ 消除模式切换资源泄漏
- ✅ 测试可验证 Dispose 行为
- ⚠️ 需要确认子接口实例是否持有需释放的资源
