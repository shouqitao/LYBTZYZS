# ADR-0022: Desktop JSON Serialization Unification

## 状态
**已实施** — 2026-08-19（实现见任务书 high-priority-fixes，commit c56ec0893）

## 背景

L4 层 Remote 和 Local 模式的 JSON 序列化配置不一致：

| 维度 | Remote (Refit) | Local (HttpApiClientBase) |
|------|----------------|---------------------------|
| PropertyNamingPolicy | camelCase | null (PascalCase) |
| Enum 处理 | JsonStringEnumConverter | 无（数字序列化） |
| CaseInsensitive | true | true |

Local 依赖 `PropertyNameCaseInsensitive` 把 ASP.NET 输出的 camelCase 反序列化进 PascalCase 属性——能工作但脆弱（属性名拼写错误静默 null）；且无 enum 字符串转换器。

## 决策

统一为 **camelCase + JsonStringEnumConverter**：

### HttpApiClientBase
```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // 统一 camelCase
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }     // 枚举字符串化
};
```

### 前提条件
- 确认 LocalWebAPI 所有端点返回 camelCase 格式（ASP.NET 默认行为）
- 确认 LocalWebAPI 所有枚举以字符串格式序列化（非数字）
- 如果有例外，需要在测试中验证

## 实施计划

| 批次 | 内容 |
|------|------|
| Batch 1 | 确认 LocalWebAPI 契约（枚举格式） |
| Batch 2 | 修改 `HttpApiClientBase.JsonOptions` |
| Batch 3 | 更新相关测试 |
| Batch 4 | 集成验证 |

## 后果

- ✅ Remote/Local 序列化行为一致
- ✅ 消除 PascalCase 静默 null 风险
- ✅ Local 枚举支持字符串格式
- ⚠️ 需先确认 LocalWebAPI 契约
