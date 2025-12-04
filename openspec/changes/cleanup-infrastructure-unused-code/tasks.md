# Tasks: cleanup-infrastructure-unused-code

## Phase 1: IRepository接口方法清理

### Task 1.1: 删除IRepository中未用方法声明
- [ ] 删除 `DeleteRangeAsync(IEnumerable<T>)` 声明 (IRepository.cs:101-102)
- [ ] 删除 `DeleteRangeAsync(IEnumerable<Guid>)` 声明 (IRepository.cs:108-109)
- [ ] 删除 `ExistsAsync(Guid)` 声明 (IRepository.cs:117-118)

### Task 1.2: 删除BaseRepository中对应实现
- [ ] 删除 `DeleteRangeAsync(Expression<...>)` 实现 (BaseRepository.cs:461-489)
- [ ] 删除 `DeleteRangeAsync(IEnumerable<T>)` 实现 (BaseRepository.cs:492-527)
- [ ] 删除 `DeleteRangeAsync(IEnumerable<Guid>)` 实现 (BaseRepository.cs:530-576)
- [ ] 删除 `ExistsAsync(Guid)` 显式接口实现 (BaseRepository.cs:302-311)

### Task 1.3: 验证Phase 1
- [ ] 编译通过
- [ ] 相关测试通过

---

## Phase 2: 未使用工具类清理

### Task 2.1: 删除未使用的Attribute
- [ ] 删除 `src/Server/Core/LYBT.Entities/Attributes/SensitiveDataAttribute.cs`

### Task 2.2: 删除未使用的数据类
- [ ] 删除 `src/Server/Core/LYBT.Infrastructure/Data/PaginatedList.cs`

### Task 2.3: 删除未使用的Controller基类
- [ ] 删除 `src/Server/Core/LYBT.Infrastructure/Web/BaseSystemController.cs`

### Task 2.4: 删除未使用的验证器
- [ ] 删除 `src/Server/Core/LYBT.Infrastructure/Validation/BusinessRuleValidator.cs`

### Task 2.5: 验证Phase 2
- [ ] 编译通过
- [ ] 无引用错误

---

## Phase 3: 冗余健康检查控制器清理

### Task 3.1: 删除冗余控制器
- [ ] 删除 `src/Server/Services/LYBT.WebAPI/Controllers/RootHealthController.cs`

### Task 3.2: 验证健康检查端点
- [ ] 确认 `/health` 端点仍可通过MapHealthChecks访问
- [ ] 确认 `/health/database` 端点正常工作

---

## Phase 4: 文档同步

### Task 4.1: 更新架构文档
- [ ] 检查并更新 `docs/` 中对已删除代码的引用

### Task 4.2: 最终验证
- [ ] 完整编译通过 (dotnet build LYBT.All.sln)
- [ ] 所有测试通过 (dotnet test)
- [ ] 健康检查端点功能正常

---

## Summary

| Phase | 描述 | 预计删除行数 | 状态 |
|-------|------|-------------|------|
| 1 | IRepository接口清理 | ~120行 | Pending |
| 2 | 未使用工具类清理 | ~400行 | Pending |
| 3 | 冗余健康检查控制器 | ~50行 | Pending |
| 4 | 文档同步 | N/A | Pending |

**总计**: ~570行代码删除
