# Progress Log

## Session: 2026-02-21 MedicalCase 模块全面简化

### Phase 1: 重置上下文 + 编译验证 -- complete
- [x] 覆盖重建三文件
- [x] 编译: 0 error | 测试: 33/33 pass

### Phase 2: 清理死代码 -- complete
- [x] MedicalCaseRules.cs: 移除 CanComplete, IsSameDayByCreator, ValidationResult, ValidateNewCaseCreation, ValidateCaseUpdate (5个死方法)
- [x] 合并 MedicalCaseValidationHelper.IsValidStatusTransition 到 MedicalCaseRules
- [x] 删除 MedicalCaseValidationHelper.cs
- 编译: 0 error | 测试: 33/33 pass

### Phase 3: 统一权限层 -- complete
- [x] IMedicalCasePermissionService: 添加 CanEdit(userId, isAdmin, mc) 和 CanDelete(userId, isAdmin, mc) 重载
- [x] MedicalCasePermissionService: 实现重载 (委托给主方法)
- [x] CommandService: 注入 _permissionService, 替换 4 处 CanEdit + 1 处 CanDelete
- [x] StateService: 注入 _permissionService, 替换 2 处 CanEdit
- [x] MedicalCaseRules: 移除 CanEdit 和 CanDelete (不再需要)
- [x] 测试: 更新 StateServiceTests 和 CommandServiceTests 构造函数
- 编译: 0 error | 测试: 33/33 pass (修复 1 个权限测试)

### Phase 4: 合并创建逻辑 -- complete
- [x] ServiceHelper: 新增 ValidateAndFetchCreationContextAsync (统一 Patient/Doctor/BR-001 验证)
- [x] CreateAsync 简化为委托给 CreateFromInputDtoAsync (~85行->~10行)
- [x] CreateFromInputDtoAsync 使用 Helper 验证 + CreateNewPrescription 统一处方创建
- [x] 移除 ValidateSingleActiveCaseRule (已合并到 Helper)
- 编译: 0 error | 测试: 33/33 pass (修复 1 个错误消息断言)

### Phase 5: 提取共享 Helper -- complete
- [x] ServiceHelper: 新增 ExecuteWithConcurrencyRetryAsync (通用重试逻辑)
- [x] ServiceHelper: 新增 EnsureCanEdit/EnsureCanDelete (权限验证 helper)
- [x] SaveAsync 重试逻辑替换为 Helper
- [x] CreatePrescriptionAsync 重试逻辑替换为 Helper
- [x] 6 处权限检查 check+log+throw 替换为 EnsureCanEdit 单行调用
- 编译: 0 error | 测试: 33/33 pass

### Phase 6: 精简日志 -- complete
- [x] 移除 ~20 条冗余日志 (started/completed 配对中的 completed, 私有方法入口)
- [x] "started" 简化为无 "started" 后缀的入口日志
- [x] 合并重复的审计日志调用为 LogUpdateAuditAsync
- 编译: 0 error 0 warning | 测试: 33/33 pass

### Phase 7: 全量验证 + 文档更新 -- complete
- [x] 全量测试: 767/767 pass (LYBT.Tests.* 范围)
- [x] MedicalCase 专项: 33/33 pass
- [x] docs/03-architecture/server.md v1.4: CQRS 服务列表更新

### 最终行数对比

| 文件 | 变更前 | 变更后 | 变化 |
|------|--------|--------|------|
| CommandService | 1,026 | 714 | -312 (-30%) |
| MedicalCaseRules | 172 | 57 | -115 (-67%) |
| StateService | 290 | 277 | -13 (-4%) |
| ServiceHelper | 97 | 222 | +125 (吸收公共逻辑) |
| PermissionService | 207 | 226 | +19 (添加重载) |
| ValidationHelper | 32 | 0 | -32 (删除/合并) |
| **净减少** | | | **-328 行** |

### 变更文件汇总

| 文件 | Phase | 变更类型 |
|------|-------|----------|
| MedicalCaseRules.cs | 2,3 | 移除5个死方法+CanEdit/CanDelete, 添加IsValidStatusTransition |
| MedicalCaseValidationHelper.cs | 2 | 删除 (合并到 Rules) |
| MedicalCaseCommandService.cs | 3,4,5,6 | 注入PermissionService, 合并CreateAsync, 提取Helper, 精简日志 |
| MedicalCaseStateService.cs | 2,3,5,6 | 更新引用, 注入PermissionService, EnsureCanEdit, 精简日志 |
| MedicalCaseServiceHelper.cs | 4,5 | 新增 ValidateAndFetchCreationContextAsync, ExecuteWithConcurrencyRetryAsync, EnsureCanEdit/Delete |
| MedicalCasePermissionService.cs | 3 | 添加 isAdmin 重载 |
| IMedicalCasePermissionService.cs | 3 | 添加重载签名 |
| MedicalCaseStateServiceTests.cs | 3 | 更新构造函数+mock |
| MedicalCaseCommandServiceTests.cs | 3 | 更新构造函数+mock+断言 |
| docs/03-architecture/server.md | 7 | v1.4 CQRS 服务列表更新 |
