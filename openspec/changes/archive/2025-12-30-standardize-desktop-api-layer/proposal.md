# Proposal: standardize-desktop-api-layer

## Summary

统一Desktop客户端API层设计规范，修正返回类型不一致问题，补充缺失功能，删除重复/无用方法，确保所有业务实体API接口遵循统一模式。

## Problem Statement

当前Desktop API层存在以下问题：

1. **返回类型不一致**: 4个Delete方法返回`ApiResponse<ApiResponse>`嵌套类型，应为`ApiResponse`
2. **功能缺失不统一**: 各实体API的功能覆盖不一致（如Formula缺少导入导出，User缺少导出）
3. **存在重复方法**: `IMedicalCaseApi.QueryMedicalCasesAsync`与`SearchMedicalCasesAsync`功能重复
4. **缺少恢复功能**: `IMedicalCaseApi`缺少`RestoreAsync`方法

## Proposed Solution

### Phase 1: 修正返回类型（4处）

| 接口 | 方法 | 当前 | 修正后 |
|-----|------|------|--------|
| IPatientApi | DeletePatientAsync | `ApiResponse<ApiResponse>` | `ApiResponse` |
| IHerbApi | DeleteHerbAsync | `ApiResponse<ApiResponse>` | `ApiResponse` |
| IFormulaApi | DeleteFormulaAsync | `ApiResponse<ApiResponse>` | `ApiResponse` |
| IUserApi | DeleteUserAsync | `ApiResponse<ApiResponse>` | `ApiResponse` |

### Phase 2: 删除重复方法（1处）

- 删除 `IMedicalCaseApi.QueryMedicalCasesAsync` - 与 `SearchMedicalCasesAsync` 功能重复

### Phase 3: 补充缺失功能

| 接口 | 新增方法 | 说明 |
|-----|---------|------|
| IMedicalCaseApi | `RestoreAsync` | 恢复误删医案 |
| IFormulaApi | `BatchImportAsync` | 批量导入验方 |
| IFormulaApi | `ExportTemplateAsync` | 导出导入模板 |
| IFormulaApi | `ExportFormulasAsync` | 导出验方数据 |
| IUserApi | `ExportTemplateAsync` | 导出导入模板 |
| IUserApi | `ExportUsersAsync` | 导出用户数据 |

## Scope

### In Scope

- Desktop.Contracts/Api/ 下的6个API接口
- 对应的Repository层适配
- Server端对应的Controller端点（如缺失）

### Out of Scope

- Service层重构（后续提案）
- ViewModel层重构（后续提案）
- 新增业务功能

## Success Criteria

1. 所有Delete方法返回统一的`ApiResponse`类型
2. 各实体API功能矩阵达到目标状态
3. 无重复/无用的API方法
4. 编译通过，现有功能不受影响
5. 更新client-api-conventions规范

## Risks & Mitigations

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| Delete返回类型变更影响调用方 | 中 | 全局搜索确认调用方式后修改 |
| Server端缺少对应端点 | 中 | 先检查Server端实现，按需添加 |
| 删除方法影响现有功能 | 低 | 已确认QueryMedicalCasesAsync无直接调用 |

## Timeline

- Phase 1: 0.5天（返回类型修正）
- Phase 2: 0.5天（删除重复方法）
- Phase 3: 1天（补充缺失功能）
- 测试验证: 0.5天

总计: 2.5天

## Related

- Spec: `client-api-conventions` - 需更新API设计规范
- Spec: `webapi-cleanup` - Server端API清理规范
