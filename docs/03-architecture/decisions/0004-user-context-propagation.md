# ADR-0004: 用户上下文传递模式

**状态**: 已采纳
**日期**: 2025-11-01
**来源**: ADR-001 (architecture/decisions)

## 背景

多层架构中需要将当前登录用户信息从 HTTP 请求传递到 Service/Repository 层，用于审计日志和权限控制。

## 决策

Controller 层显式提取 userId，通过 Service 方法参数传递:

```csharp
// Controller 层
[HttpPost]
public async Task<IActionResult> Create(PatientInputDto dto)
{
    var userId = GetOperator(); // 从 JWT Claims 提取
    return Ok(await _service.CreateAsync(dto, userId));
}

// Service 层
public async Task<Result<PatientDto>> CreateAsync(PatientInputDto dto, Guid userId)
{
    entity.CreatedBy = userId;
    // ...
}
```

### 规则
- Controller 使用 `GetOperator()` 提取用户 ID
- Service 方法签名必须包含 userId 参数
- 禁止 Service 层注入 `IHttpContextAccessor`
- 禁止通过 ambient context 传递用户信息

## 理由

- 显式传递避免隐式依赖
- Service 层可独立测试 (传入 userId 即可)
- 审计字段永远不会遗漏

## 变更记录

| 日期 | 变更 |
|------|------|
| 2025-11-01 | 初始决策 |
