# Tasks: cleanup-infrastructure-unused-code

## Phase 1: IRepository接口方法清理

### Task 1.1: 删除IRepository中未用方法声明
- [x] 删除 `DeleteRangeAsync(IEnumerable<T>)` 声明
- [x] 删除 `DeleteRangeAsync(IEnumerable<Guid>)` 声明
- [x] 删除 `ExistsAsync(Guid)` 声明

### Task 1.2: 删除BaseRepository中对应实现
- [x] 删除 `DeleteRangeAsync(IEnumerable<T>)` 实现
- [x] 删除 `DeleteRangeAsync(IEnumerable<Guid>)` 实现
- [x] 删除 `ExistsAsync(Guid)` 实现

### Task 1.3: 验证Phase 1
- [x] 编译通过
- [x] 相关测试通过

---

## Phase 2: 未使用工具类清理

### Task 2.1: SensitiveDataAttribute
- [x] **保留** `SensitiveDataAttribute.cs` (安全合规预留，Issue #2254)

### Task 2.2: 删除未使用的数据类
- [x] 删除 `src/Server/Core/LYBT.Infrastructure/Data/PaginatedList.cs`

### Task 2.3: 删除未使用的Controller基类
- [x] 删除 `src/Server/Core/LYBT.Infrastructure/Web/BaseSystemController.cs`

### Task 2.4: 删除未使用的验证器
- [x] 删除 `src/Server/Core/LYBT.Infrastructure/Validation/BusinessRuleValidator.cs`

### Task 2.5: 验证Phase 2
- [x] 编译通过
- [x] 无引用错误

---

## Phase 3: 冗余健康检查控制器清理

### Task 3.1: 删除冗余控制器
- [x] 删除 `src/Server/Services/LYBT.WebAPI/Controllers/RootHealthController.cs`

### Task 3.2: 验证健康检查端点
- [x] 确认 `/health` 端点由 `MapHealthChecks()` 提供
- [x] 路由冲突已解决

---

## Phase 4: 文档同步

### Task 4.1: 更新架构文档
- [x] 更新 `docs/reference/quick-reference/api-reference.md` 移除RootHealthController引用
- [x] 其他文档为历史存档，无需更新

### Task 4.2: 最终验证
- [x] 完整编译通过 (dotnet build LYBT.All.sln) - 2025-12-04
- [x] 所有测试通过 (dotnet test) - 2025-12-04
- [x] 健康检查端点功能正常

---

## Summary

| Phase | 描述 | 预计删除行数 | 实际删除行数 | 状态 |
|-------|------|-------------|-------------|------|
| 1 | IRepository接口清理 | ~120行 | ~98行 | Completed |
| 2 | 未使用工具类清理 | ~400行 | ~468行 | Completed |
| 3 | 冗余健康检查控制器 | ~50行 | ~48行 | Completed |
| 4 | 文档同步 | N/A | N/A | Completed |

**总计**: ~614行代码删除

**保留项**: SensitiveDataAttribute.cs - 安全合规预留 (Issue #2254)

**完成时间**: 2025-12-04
