# Findings - MedicalCase 模块全面简化

## 代码状态分析 (已执行)

### MedicalCaseRules.cs: 172->57行 (-67%)
- 移除 5 个死方法: CanComplete, IsSameDayByCreator, ValidationResult, ValidateNewCaseCreation, ValidateCaseUpdate
- 移除 CanEdit/CanDelete (迁移到 PermissionService 为唯一权限权威)
- 吸收 ValidationHelper.IsValidStatusTransition
- 保留: CanCreateNewCase, HasActiveCase, HasDraftCase, IsValidStatusTransition

### MedicalCaseCommandService.cs: 1026->714行 (-30%)
- CreateAsync 简化为委托给 CreateFromInputDtoAsync (~85行->~10行)
- 重试逻辑提取到 ServiceHelper.ExecuteWithConcurrencyRetryAsync
- 6处权限检查简化为 ServiceHelper.EnsureCanEdit 单行调用
- 移除 ~20 条冗余日志

### RequiresEditReason: 保留
- 初始计划标记为死代码，但经验证 GetPermissions 被 Controller 调用
- RequiresEditReason 作为 GetPermissions 内部依赖，不是死代码

### 权限统一
- PermissionService 是唯一权限权威 (CanEdit/CanDelete)
- Rules 仅保留无状态策略检查 (CanCreateNewCase, HasActiveCase, IsValidStatusTransition)
- 添加 isAdmin 重载避免修改公开接口签名

### 测试修复
- UpdateConsultation_WhenStatusNotActive: 需覆盖默认 mock 的 CanEdit 返回值
- CreateAsync_WhenDoctorIdIsEmpty: 错误消息变更 ("DoctorId不能为空" -> "不能为空")
