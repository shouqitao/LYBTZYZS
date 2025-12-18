# Design: optimize-batch-operations

## Context

当前系统批量操作分为两类：
1. **批量导入/导出** - 已有高效实现（单次API调用）
2. **批量删除/启用/禁用** - 使用低效的foreach循环模式（N次API调用）

本设计解决第二类问题，同时规范化批量DTO命名。

## Goals / Non-Goals

### Goals
- 将批量删除/启用/禁用从N次API调用优化为1次
- 统一批量DTO命名规范
- 复用现有基础设施（BatchIdsDto, BatchOperationResultDto）

### Non-Goals
- 不改变批量导入/导出的现有实现
- 不引入新的ORM框架
- 不改变现有API契约（仅新增端点）

## Decisions

### Decision 1: DTO命名规范

**规范**:
| 类型 | 命名格式 | 示例 |
|------|----------|------|
| 批量输入 | `{Entity}Batch{Operation}InputDto` | `PatientBatchImportInputDto` |
| 批量结果 | `{Entity}Batch{Operation}ResultDto` | `PatientBatchImportResultDto` |
| 导入行项 | `{Entity}ImportItemDto` | `PatientImportItemDto` |
| 导出行项 | `{Entity}ExportItemDto` | `PatientExportItemDto` |
| 通用批量ID | `BatchIdsDto` | 保持不变 |
| 通用批量结果 | `BatchOperationResultDto` | 保持不变 |

**Rationale**: 遵循dto-architecture-specification，使用Input后缀替代Request，使用Item后缀明确行数据角色。

### Decision 2: 批量API设计

**端点设计**:
```
POST /api/v1/{entity}/batch-delete
POST /api/v1/{entity}/batch-enable
POST /api/v1/{entity}/batch-disable
```

**请求体**: 复用现有 `BatchIdsDto`
```csharp
public class BatchIdsDto
{
    public List<Guid> Ids { get; set; } = [];
}
```

**响应体**: 复用现有 `BatchOperationResultDto`

**Rationale**:
- POST用于批量操作（语义为"执行操作"而非"创建资源"）
- 复用现有DTO避免重复定义

### Decision 3: 数据库级批量操作

**实现方式**: 使用EF Core 7+ `ExecuteDelete`/`ExecuteUpdate`

```csharp
// 批量删除
public async Task<int> BatchDeleteAsync(List<Guid> ids)
{
    return await _dbContext.Users
        .Where(u => ids.Contains(u.Id))
        .ExecuteDeleteAsync();
}

// 批量更新状态
public async Task<int> BatchUpdateStatusAsync(List<Guid> ids, CommonStatus status)
{
    return await _dbContext.Users
        .Where(u => ids.Contains(u.Id))
        .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, status));
}
```

**Rationale**:
- 单次数据库往返，性能远优于N次SaveChanges
- 不加载实体到内存，减少内存占用
- EF Core原生支持，无需引入额外依赖

**Alternatives considered**:
1. **存储过程** - 不考虑，增加维护复杂度
2. **原始SQL** - 不考虑，失去类型安全
3. **Dapper批量操作** - 不考虑，引入额外依赖

## Risks / Trade-offs

### Risk 1: ExecuteDelete绕过Change Tracker
**风险**: 审计日志可能不自动记录
**缓解**: 在Service层显式记录审计日志

### Risk 2: 并发更新
**风险**: 批量操作期间可能有其他更新
**缓解**: 使用事务包裹，返回实际影响行数

### Risk 3: 大批量性能
**风险**: 超大批量（>1000）可能导致SQL参数过多
**缓解**: 分批处理，每批最多500条

## Migration Plan

### Phase 1: DTO重命名（无风险）
1. 重命名DTO文件和类名
2. 全局替换引用
3. 编译验证

### Phase 2: API优化（需测试）
1. 新增批量端点（不影响现有端点）
2. 修改Desktop调用
3. 集成测试验证
4. 性能对比测试

**Rollback**: Phase 2若出问题可快速回退（只需恢复foreach调用）

## Open Questions

1. 是否需要支持批量软删除（仅更新Status）而非硬删除？
   - **答案**: 根据现有逻辑，User/Patient等使用软删除（更新Status），Herb/Formula使用硬删除

2. 批量操作是否需要审批流程？
   - **答案**: 当前无此需求，保持与单条操作相同的权限检查
