# EF Core 并发问题排查指南

> **问题类型**: 数据库并发控制
> **技术栈**: Entity Framework Core 8.0, SQL Server
> **难度等级**: 中高级

## 问题现象

### 422 Unprocessable Entity 错误

```
Refit.ApiException: Response status code does not indicate success: 422 (Unprocessable Entity)
```

### DbUpdateConcurrencyException 异常

```
Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
The database operation was expected to affect 1 row(s), but actually affected 0 row(s).
```

## 根因分析

### RowVersion 机制原理

EF Core 使用 `RowVersion` (或 `Timestamp`) 实现乐观并发控制:

```csharp
[Timestamp]
public byte[] RowVersion { get; set; }
```

每次UPDATE操作会自动检查并更新RowVersion，若版本不匹配则抛出异常。

### 常见触发场景

1. **前端缓存过期**: ViewModel持有的RowVersion与数据库不同步
2. **ChangeTracker缓存**: DbContext缓存了旧的实体状态
3. **并发修改**: 多个操作同时修改同一实体

## 解决方案

### 方案1: 强制刷新实体 (推荐)

在Repository层添加强制刷新方法:

```csharp
// IMedicalCaseRepository.cs
Task<MedicalCaseEntity?> GetByIdWithDetailsFreshAsync(Guid id);

// MedicalCaseRepository.cs
public async Task<MedicalCaseEntity?> GetByIdWithDetailsFreshAsync(Guid id)
{
    // 分离所有缓存实体
    foreach (var entry in _context.ChangeTracker.Entries<MedicalCaseEntity>()
        .Where(e => e.Entity.Id == id))
    {
        entry.State = EntityState.Detached;
    }

    // 重新查询获取最新RowVersion
    return await GetByIdWithDetailsAsync(id);
}
```

### 方案2: 在Service层处理

保存前刷新实体:

```csharp
public async Task<Result> UpdateAsync(Guid id, UpdateDto dto)
{
    // 获取最新实体(包含最新RowVersion)
    var entity = await _repository.GetByIdWithDetailsFreshAsync(id);
    if (entity == null)
        return Result.NotFound();

    // 更新属性
    _mapper.Map(dto, entity);

    // 保存
    await _repository.UpdateAsync(entity);
    return Result.Ok();
}
```

### 方案3: 乐观锁重试机制

```csharp
public async Task<Result> UpdateWithRetryAsync(Guid id, UpdateDto dto, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var entity = await _repository.GetByIdWithDetailsFreshAsync(id);
            _mapper.Map(dto, entity);
            await _repository.UpdateAsync(entity);
            return Result.Ok();
        }
        catch (DbUpdateConcurrencyException) when (attempt < maxRetries)
        {
            // 记录日志，继续重试
            _logger.LogWarning("Concurrency conflict on attempt {Attempt}, retrying...", attempt);
        }
    }
    return Result.Fail("并发冲突，请刷新后重试");
}
```

## 预防措施

### 1. ChangeTracker管理

定期清理缓存或使用短生命周期DbContext:

```csharp
// 手动清理
_context.ChangeTracker.Clear();

// 或使用No-Tracking查询
var entity = await _context.Entities
    .AsNoTracking()
    .FirstOrDefaultAsync(e => e.Id == id);
```

### 2. 前端同步策略

保存成功后刷新RowVersion:

```csharp
// ViewModel中
var result = await _apiClient.UpdateAsync(Id, dto);
if (result.IsSuccess)
{
    // 刷新RowVersion
    RowVersion = result.Data.RowVersion;
}
```

### 3. 审计日志

记录并发冲突便于排查:

```csharp
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogError(ex,
        "Concurrency conflict for entity {EntityType} with Id {Id}. " +
        "Expected RowVersion: {Expected}, Actual: {Actual}",
        typeof(TEntity).Name, entity.Id,
        entity.RowVersion, currentRowVersion);
    throw;
}
```

## 排查清单

- [ ] 检查前端传递的RowVersion是否与数据库一致
- [ ] 检查DbContext生命周期是否过长导致缓存过期
- [ ] 检查是否有多处代码同时修改同一实体
- [ ] 检查Repository层是否正确处理Detach操作
- [ ] 检查Service层是否在保存前刷新实体

## 相关报告

- [医案工作区问题修复反思报告](../../reports/medicalcase-workspace-bug-reflection-2025-11-29.md)

## 参考资料

- [EF Core Concurrency Conflicts](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [Optimistic Concurrency Patterns](https://learn.microsoft.com/en-us/ef/core/modeling/concurrency)

---

**文档类型**: Troubleshooting Guide
**更新时间**: 2025-11-29
**维护团队**: 架构组
