# Change: Post-Release代码清理与优化

## Why

Pre-Release阶段为保证稳定性，多个提案的清理工作被DEFERRED。现在需要统一执行这些Post-Release任务：

1. **optimize-entity-data-flow Phase 4-5**: 服务层DTO迁移、过期代码移除
2. **simplify-medicalcase-api**: 查询端点合并、状态端点统一
3. **refactor-medicalcase-management**: Management旧代码删除

这些任务的共同特点：
- 不影响核心功能（功能已验证通过）
- 主要是代码清理和优化
- 减少技术债务，提升代码可维护性

## What Changes

### 1. 服务层DTO迁移 (来自 optimize-entity-data-flow Phase 4)

- Controller返回类型迁移到ListDto/DetailDto
- Service方法签名更新
- AutoMapper配置清理

### 2. 过期代码移除 (来自 optimize-entity-data-flow Phase 5)

删除已标记[Obsolete]的Management组件：
- FormulaManagementViewModel/View
- HerbManagementViewModel/View
- PatientManagementViewModel/View
- UserManagementViewModel/View
- MedicalCaseManagementViewModel/View

清理过期DTO：
- *Legacy类
- *QueryDto类
- *SearchDto类

### 3. MedicalCase API端点优化 (来自 simplify-medicalcase-api)

查询端点合并：
- 合并GetList和GetMedicalCasesList
- 合并GetById和GetMedicalCaseByIdWithDetails (include参数)
- 合并患者查询端点 (filter参数)

状态端点统一：
- 合并Close/Cancel/UpdateStatus为PATCH /{id}/status
- 删除独立的Prescription CRUD端点
- 删除UpdateConsultation端点
- 删除SetPrescriptionFlag端点

### 4. Management旧代码删除 (来自 refactor-medicalcase-management)

- MedicalCaseManagementView.xaml
- MedicalCaseManagementViewModel.cs
- MedicalCaseDetailView.xaml
- MedicalCaseDetailViewModel.cs

## Impact

- **Affected specs**: medicalcase, formula, herb, patient, user
- **Breaking changes**: API端点变更需要同步更新Client
- **Risk level**: Medium - 需要全面回归测试

## Dependencies

- Pre-Release版本已发布并稳定运行
- 所有MasterDetail功能验证通过
