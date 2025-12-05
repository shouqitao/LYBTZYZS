# Tasks: refactor-baseservice-permission

## Phase 1: 删除BaseService死代码

### Task 1.1: 删除BaseService<T>泛型权限方法
- [x] 删除GetEntityId<TEntity>虚方法
- [x] 删除GetCreatedUserId<TEntity>虚方法
- [x] 删除GetCreatedDate<TEntity>虚方法
- [x] 删除ValidateEditPermission<TEntity>泛型版本
- [x] 删除相关region注释
- **验证**: 编译通过 ✓

### Task 1.2: 可选 - 评估非泛型版本
- [x] 检查ValidateEditPermission(参数版本)是否有调用 - 无外部调用
- [x] 检查ValidateDeletePermission(参数版本)是否有调用 - 无外部调用
- [x] 决定保留：这些是有效的辅助方法，可能未来使用
- **验证**: grep确认无外部调用，保留为内部辅助方法

## Phase 2: 删除服务中的无用重写

### Task 2.1: 清理MedicalCaseStateService
- [x] 删除GetEntityId重写方法
- [x] 删除GetCreatedUserId重写方法
- [x] 删除GetCreatedDate重写方法
- **验证**: 编译通过 ✓

### Task 2.2: 清理MedicalCaseQueryService
- [x] 删除GetEntityId重写方法
- [x] 删除GetCreatedUserId重写方法
- [x] 删除GetCreatedDate重写方法
- **验证**: 编译通过 ✓

### Task 2.3: 清理MedicalCaseCommandService
- [x] 删除GetEntityId重写方法
- [x] 删除GetCreatedUserId重写方法
- [x] 删除GetCreatedDate重写方法
- **验证**: 编译通过 ✓

## Phase 3: 验证

### Task 3.1: 编译验证
- [x] 运行完整编译
- [x] 确认0错误(1个不相关警告)
- **验证**: 编译通过 ✓

### Task 3.2: 测试验证
- [x] 运行MedicalCase相关单元测试
- [x] 确认所有42个测试通过
- **验证**: 测试通过 ✓

### Task 3.3: 更新文档
- [x] tasks.md已更新标记完成状态
- [x] spec无需修改(已正确描述最终状态)
- **验证**: 文档一致 ✓

---

## Dependencies

```
Phase 1 (BaseService清理) → Phase 2 (服务清理) → Phase 3 (验证)
```

可并行：Task 2.1 / 2.2 / 2.3

## Estimated Effort

| Phase | Tasks | Complexity |
|-------|-------|------------|
| Phase 1 | 2 | Low |
| Phase 2 | 3 | Low |
| Phase 3 | 3 | Low |
| **Total** | **8** | **Low** |

## Notes

- 这是一个简单的死代码清理任务
- 实际权限验证逻辑在MedicalCaseRules和MedicalCasePermissionService中，保持不变
- 删除代码不影响任何功能
