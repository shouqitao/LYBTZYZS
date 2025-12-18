# Change: 优化批量操作实现与DTO命名规范化

## Why

当前批量删除/启用/禁用操作采用客户端foreach循环模式，每个Item触发一次独立API请求，导致N+1性能问题。
同时，批量相关DTO命名不统一（混用Request/Input后缀，Import/ExportDto缺少Item后缀），违反dto-architecture-specification规范。

**问题示例**（UserMasterDetailViewModel）:
```csharp
// N+1调用模式 - 选中10个用户，触发10次API请求
foreach (var item in items)
{
    var result = await _commandHandler.DeleteAsync(item.Id);
}
```

## What Changes

### 1. 批量操作API优化
- 新增服务端批量端点：`POST /api/v1/{entity}/batch-delete`
- 新增服务端批量端点：`POST /api/v1/{entity}/batch-enable`
- 新增服务端批量端点：`POST /api/v1/{entity}/batch-disable`
- 使用EF Core `ExecuteDelete`/`ExecuteUpdate` 实现数据库级批量操作

### 2. 批量DTO命名规范化
**重命名**（遵循dto-architecture-specification）:
| 原名称 | 新名称 | 原因 |
|--------|--------|------|
| `*BatchImportRequestDto` | `*BatchImportInputDto` | Request→Input |
| `*ImportDto` | `*ImportItemDto` | 明确为导入行项目 |
| `*ExportDto` | `*ExportItemDto` | 明确为导出行项目 |
| `BatchCheckReferenceRequestDto` | `HerbBatchCheckReferenceInputDto` | 添加实体前缀+Request→Input |
| `ImportFormulasDataRequest` | `FormulaBatchImportInputDto` | 统一命名格式 |

### 3. 客户端调用优化
- Desktop层批量操作改为单次API调用
- 复用现有 `BatchIdsDto` 和 `BatchOperationResultDto`

## Impact

- **Affected specs**: dto-architecture, batch-operations (新增)
- **Affected modules**:
  - Server: User, Patient, Herb, Formula, MedicalCase Controllers/Services
  - Desktop: *MasterDetailViewModel 批量操作方法
  - Shared: 15+ 批量相关DTO重命名

## Scope

### Phase 1: DTO命名规范化（本次执行）
- 重命名15个批量相关DTO
- 更新所有引用

### Phase 2: 批量操作API优化（后续执行）
- 新增服务端批量端点
- 修改客户端调用

## Status

- [x] 提案创建
- [x] Phase 1: DTO命名规范化 (1.1-1.5)
- [x] Phase 1.6: 文件名与类名一致性整改
- [x] Phase 1.7: 删除重复定义 (BatchIdsDto)
- [x] Phase 1.8: 统一失败详情DTO命名
- [x] Phase 1.9: BatchImportResultDto继承规范化
- [x] Phase 2: 批量删除API优化 (2.1, 2.3, 2.5-2.7)
- [x] Phase 2: Server端批量启用/禁用API (2.2, 2.4)
- [x] Phase 2: Desktop层批量启用/禁用集成 (2.9)
- [x] Phase 2: 测试验证 (2.8.2, 2.8.3) - BatchOperationsTests.cs, BatchOperationsBenchmark.cs
- [x] **ALL PHASES COMPLETED** - 可归档
