# Tasks: unify-enums-to-shared

## Phase 0: 删除未使用枚举

### Task 0.1: 清理SystemEnums.cs中未使用枚举
- [ ] 删除 `DataStatus` 枚举
- [ ] 删除 `AuditStatus` 枚举
- [ ] 删除 `DeleteStatus` 枚举
- [ ] 删除 `TimeSlot` 枚举
- [ ] 删除 `WorkDay` 枚举
- [ ] 删除 `PaymentStatus` 枚举
- [ ] 删除 `PaymentMethod` 枚举
- [ ] 删除 `CompatibilityType` 枚举
- [ ] 删除 `CompatibilitySeverity` 枚举

### Task 0.2: 清理MedicalCaseEnums.cs中未使用枚举
- [ ] 删除 `PendingType` 枚举

### Task 0.3: 验证删除后编译通过
- [ ] 执行 `dotnet build LYBT.All.sln`
- [ ] 确认无编译错误

## Phase 1: 合并重复枚举

### Task 1.1: 创建ErrorEnums.cs合并错误相关枚举
- [ ] 创建`LYBT.Shared.Models/Enums/ErrorEnums.cs`
- [ ] 合并`ErrorCode`、`ErrorCategory`、`ErrorSeverity`
- [ ] 删除重复定义:
  - `Shared/LYBT.Shared.Models/Contracts/Common/SharedCommon.cs`中的ErrorCategory、ErrorSeverity
  - `Shared/LYBT.Shared.Models/Errors/ErrorCategory.cs`
  - `Shared/LYBT.Shared.Models/Contracts/Common/ErrorCategory.cs`
  - `Shared/LYBT.Shared.Models/Contracts/Common/ErrorSeverity.cs`
- [ ] 更新所有引用到新命名空间

### Task 1.2: 处理Client层ErrorSeverity
- [ ] `Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ErrorHandling/ErrorContext.cs`
- [ ] 改为使用`LYBT.Shared.Models.Enums.ErrorSeverity`
- [ ] 删除Client层重复定义

## Phase 2: 迁移分散枚举

### Task 2.1: 迁移MedicalCaseUpdateMode
- [ ] 从`MedicalCaseDtos.cs`移至`MedicalCaseEnums.cs`
- [ ] 更新所有引用

### Task 2.2: 创建ValidationEnums.cs
- [ ] 创建`LYBT.Shared.Models/Enums/ValidationEnums.cs`
- [ ] 迁移`BusinessOperation`从`ValidationContext.cs`
- [ ] 更新所有引用

### Task 2.3: 创建SecurityEnums.cs
- [ ] 创建`LYBT.Shared.Models/Enums/SecurityEnums.cs`
- [ ] 迁移`PasswordStrength`从`PasswordHelper.cs`
- [ ] 更新所有引用

## Phase 3: 创建枚举中文扩展方法

### Task 3.1: 创建EnumExtensions.cs
- [ ] 创建`LYBT.Shared.Models/Extensions/EnumExtensions.cs`
- [ ] 实现以下枚举的ToChinese()方法:

**系统枚举:**
- [ ] `Gender` → 男/女/未知
- [ ] `CommonStatus` → 启用/停用/已删除
- [ ] `OperationResult` → 成功/失败/部分成功

**业务枚举:**
- [ ] `MedicalCaseStatus` → 草稿/进行中/已完成/已取消
- [ ] `ConsultationStatus` → 待诊/进行中/已完成
- [ ] `PatientStatus` → 正常/停诊/注销
- [ ] `CaseStatus` → 草稿/待审/已审/归档
- [ ] `DecocteMethod` → 常规/先煎/后下/包煎/另煎/烊化/冲服
- [ ] `AuditOperationType` → 提交/审核通过/驳回/撤回
- [ ] `FormulaValidationStatus` → 有效/无效/需审核
- [ ] `DuplicateStrategy` → 跳过/覆盖/报错

**认证枚举:**
- [ ] `UserRole` → 管理员/医生/药师/前台
- [ ] `LoginType` → 密码/验证码/指纹
- [ ] `AuthSessionStatus` → 活跃/已过期/已登出
- [ ] `AuthErrorCode` → 用户名错误/密码错误/账户锁定/...

### Task 3.2: 创建EnumToChineseConverter (可选)
- [ ] 创建WPF值转换器`EnumToChineseConverter`
- [ ] 支持XAML绑定直接显示中文

## Phase 4: 序列化配置清理

### Task 4.1: 清理冗余[JsonConverter]属性
- [ ] 搜索所有带有`[JsonConverter(typeof(JsonStringEnumConverter))]`的枚举
- [ ] 删除冗余属性（全局已配置，无需单独标注）
- [ ] 验证API请求/响应枚举序列化正常

### Task 4.2: 验证全局配置
- [ ] 确认Server端`ServiceCollectionExtensions.cs`有`JsonStringEnumConverter`
- [ ] 确认Client端`ApiService.cs`有`JsonStringEnumConverter`
- [ ] API测试：枚举以字符串形式传递

## Phase 5: 验证与清理

### Task 5.1: 编译验证
- [ ] 执行`dotnet build LYBT.All.sln`
- [ ] 修复所有编译错误
- [ ] 消除所有警告

### Task 5.2: 测试验证
- [ ] 执行`dotnet test`
- [ ] 确保所有测试通过

### Task 5.3: 文档更新
- [ ] 更新架构文档
- [ ] 归档此提案

## 进度追踪

| Task | 状态 | 完成日期 |
|------|------|----------|
| 0.1 清理SystemEnums未使用枚举 | Pending | - |
| 0.2 清理MedicalCaseEnums未使用枚举 | Pending | - |
| 0.3 验证删除后编译 | Pending | - |
| 1.1 ErrorEnums合并 | Pending | - |
| 1.2 Client ErrorSeverity | Pending | - |
| 2.1 MedicalCaseUpdateMode | Pending | - |
| 2.2 ValidationEnums | Pending | - |
| 2.3 SecurityEnums | Pending | - |
| 3.1 EnumExtensions.cs | Pending | - |
| 3.2 EnumToChineseConverter | Pending | - |
| 4.1 清理冗余[JsonConverter]属性 | Pending | - |
| 4.2 验证全局配置 | Pending | - |
| 5.1 编译验证 | Pending | - |
| 5.2 测试验证 | Pending | - |
| 5.3 文档更新 | Pending | - |
