# Proposal: cleanup-unused-methods

## Summary

清理项目中所有定义但从未被调用的Repository和Service方法，减少代码维护负担和潜在的技术债务。

## Problem Statement

通过代码分析发现，Repository层和Service层中存在多个public方法，它们：
1. 在接口中声明并在实现类中实现
2. 但从未被上层调用（Repository方法未被Service调用，Service方法未被Controller调用）
3. 增加了代码维护成本和测试覆盖负担

部分Service方法是因为之前清理了Controller端点（cleanup-obsolete-code），但遗漏了对应的Service方法。

## Proposed Solution

### Part A: Repository层清理

删除以下未被调用的Repository方法：

#### Phase 1: MedicalCase模块
- `MedicalCaseRepository.GetByDoctorIdAsync` - 按医生ID查询，从未被Service调用

#### Phase 2: Patient模块
- `PatientRepository.GetByDateRangeAsync` - 按日期范围查询，从未被Service调用

#### Phase 3: Formula模块
- `FormulaRepository.GetByCategoryAsync` - 按分类查询，从未被Service调用
- `FormulaRepository.GetSharedFormulasAsync` - 获取共享方剂，从未被Service调用

#### Phase 4: Herbs模块
- `HerbRepository.GetByCategoryAsync` - 按分类查询，从未被Service调用

### Part B: Service层清理

删除以下未被调用的Service方法：

#### Phase 5: UserService
- `UserService.DisableAsync` - 禁用用户，Controller端点已删除
- `UserService.EnableAsync` - 启用用户，Controller端点已删除
- `UserService.ToggleStatusAsync` - 切换状态，Controller端点已删除
- `UserService.BatchDeleteAsync` - 批量删除，Controller端点已删除
- `UserService.ResetPasswordAsync(Guid, string)` - 未使用的重载版本

#### Phase 6: HerbService
- `HerbService.BatchDeleteAsync` - 批量删除，Controller端点已删除

#### Phase 7: FormulaService
- `FormulaService.BatchDeleteAsync` - 批量删除，Controller端点已删除

#### Phase 8: PatientService
- `PatientService.SearchEntityAsync` - 从未被Controller调用

#### Phase 9: TokenRevocationService
- `TokenRevocationService.RevokeAllUserTokensAsync` - 从未被调用

## Scope

- **In Scope**:
  - Repository层未被调用的方法
  - Service层未被调用的方法
  - 对应的接口定义
  - 相关的单元测试（如存在）

- **Out of Scope**:
  - 可能被反射调用的方法
  - 预留给未来功能的方法（需确认）

## Impact Analysis

| 层级 | 模块 | 删除方法数 | 预计减少代码行 |
|------|------|-----------|---------------|
| Repository | MedicalCase | 1 | ~15 |
| Repository | Patient | 1 | ~20 |
| Repository | Formula | 2 | ~30 |
| Repository | Herbs | 1 | ~15 |
| Service | Users | 5 | ~150 |
| Service | Herbs | 1 | ~80 |
| Service | Formula | 1 | ~100 |
| Service | Patients | 1 | ~15 |
| Service | Auth | 1 | ~70 |
| **总计** | | **14** | **~495** |

## Risk Assessment

- **低风险**: 所有方法经过引用分析确认无调用者
- **回滚方案**: Git revert即可恢复

## Success Criteria

1. 删除所有标识的未调用方法
2. 编译通过，0 errors, 0 warnings
3. 现有单元测试全部通过

## Timeline

单次PR完成，预计清理工作量中等。
