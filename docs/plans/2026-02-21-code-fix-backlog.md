# CODE 偏差修复任务清单

> 创建时间: 2026-02-21
> 数据来源: `docs/plans/2026-02-21-deviation-triage-checklist.md`
> 总任务数: 201 项

---

## 统计总览

### 按 Tier 分布

| Tier | 主题 | 任务数 | 预计 Sprint | 涉及模块 |
|------|------|--------|------------|----------|
| Tier 1 | 安全漏洞+数据完整性 | 30 | Sprint 1 | auth, users, patients, herbs, medical-cases |
| Tier 2 | 核心功能修复 | 42 | Sprint 2 | medical-cases, printing, 多模块, formulas, logging, nfr, patients, error-handling, health-diagnostics |
| Tier 3 | 体系统一 | 26 | Sprint 3 | 全模块 |
| Tier 4 | 本地模式补齐 | 37 | Sprint 4 | users, patients, herbs, formulas, printing, desktop-shell, nfr |
| Tier 5 | 细节完善 | 66 | Sprint 5+ | 多模块 |
| **合计** | | **201** | | |

### 按模块分布

| 模块 | CODE 总数 | T1 | T2 | T3 | T4 | T5 |
|------|-----------|----|----|----|----|-----|
| auth | 12 | 1 | 1 | 1 | 0 | 9 |
| users | 29 | 15 | 1 | 2 | 8 | 3 |
| patients | 28 | 6 | 2 | 5 | 4 | 11 |
| herbs | 22 | 4 | 5 | 4 | 6 | 3 |
| formulas | 17 | 0 | 4 | 3 | 4 | 6 |
| medical-cases | 32 | 4 | 7 | 1 | 0 | 20 |
| printing | 23 | 0 | 12 | 0 | 11 | 0 |
| sync | 8 | 0 | 0 | 1 | 0 | 7 |
| desktop-shell | 8 | 0 | 1 | 0 | 4 | 3 |
| configuration | 5 | 0 | 2 | 0 | 0 | 3 |
| error-handling | 6 | 0 | 1 | 3 | 0 | 2 |
| logging | 4 | 0 | 4 | 0 | 0 | 0 |
| health-diagnostics | 2 | 0 | 2 | 0 | 0 | 0 |
| nfr | 5 | 0 | 0 | 0 | 1 | 4 |

---

## 横切面索引

- [X1: 错误码 MCCEE 统一](#x1-错误码-mccee-统一-15项) (15项, Tier 3)
- [X2: IDataSource+导入导出](#x2-idatasource导入导出-22项) (22项, Tier 4)
- [X3: Token Family 撤销](#x3-token-family-撤销-6项) (6项, Tier 1)
- [X4: Service层ErrorCode替代](#x4-service层errorcode替代-5项) (5项, Tier 3)
- [X5: 字段验证值对齐](#x5-字段验证值对齐-code项-15项) (15项, Tier 2)
- [X6: 分页筛选迁移Repository](#x6-分页筛选迁移repository-5项) (5项, Tier 3)
- [X7: 引用检查修复](#x7-引用检查修复-10项) (10项, Tier 1)
- [X8: 打印层级重构](#x8-打印层级重构-12项) (12项, Tier 2)

---

## Tier 1: 安全漏洞+数据完整性 (30项)

### X3: Token Family 撤销 (6项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T1-X3-01 | AUTH-01 | auth | 登录时撤销已有 Token Family，实现单会话登录 | `Server/Modules/LYBT.Module.Auth/Services/AuthService.cs` | 在 LoginAsync 中调用 TokenRevocationService 撤销同用户已有 Token Family | 集成测试: 二次登录后旧 Token 失效 |
| T1-X3-02 | USER-02 | users | 角色变更后撤销目标用户 Token Family | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 在角色更新后调用 TokenRevocationService.RevokeAllByUserId | 集成测试: 角色变更后旧 Token 失效 |
| T1-X3-03 | USER-05 | users | 删除用户后撤销其 Token Family | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 在 DeleteAsync 后调用 TokenRevocationService.RevokeAllByUserId | 集成测试: 删除用户后 Token 立即失效 |
| T1-X3-04 | USER-06 | users | 重置密码后撤销目标用户 Token Family | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 在 ResetPasswordAsync 后调用 TokenRevocationService.RevokeAllByUserId | 集成测试: 重置密码后旧 Token 失效 |
| T1-X3-05 | USER-08 | users | 修改密码后撤销当前用户 Token Family | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 在 ChangePasswordAsync 后调用 TokenRevocationService.RevokeAllByUserId | 集成测试: 修改密码后旧 Token 失效 |
| T1-X3-06 | USER-14 | users | 禁用用户后撤销其 Token Family | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 在 ToggleStatusAsync 中禁用分支调用 TokenRevocationService | 集成测试: 禁用后 Token 立即失效 |

### X7: 引用检查修复 (10项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T1-X7-01 | PAT-05 | patients | 单条删除时调用引用检查 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | 在 DeleteAsync 中先调用 CheckReferenceAsync，有引用时返回 422 | 单元测试 |
| T1-X7-02 | PAT-06 | patients | 批量删除时调用引用检查 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | 在 BatchDeleteAsync 中逐个检查引用 | 单元测试 |
| T1-X7-03 | PAT-09 | patients | Controller 添加 check-reference 端点 | `Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs` | 添加 GET /api/patients/{id}/check-reference 端点 | 集成测试 |
| T1-X7-04 | PAT-10 | patients | CheckReferenceAsync 实现实际医案引用计数查询 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | 查询 MedicalCase 表替代硬编码 CanDelete=true | 单元测试 |
| T1-X7-05 | PAT-11 | patients | Controller 添加 batch-check-reference 端点 | `Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs` | 添加 POST /api/patients/batch-check-reference 端点 | 集成测试 |
| T1-X7-06 | PAT-12 | patients | BatchCheckReference 实现实际引用计数查询 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | 批量查询 MedicalCase 引用计数 | 单元测试 |
| T1-X7-07 | HERB-01 | herbs | 删除药材时检查处方引用 | `Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` | 在 DeleteAsync 中查询 PrescriptionItem 引用 | 单元测试 |
| T1-X7-08 | HERB-02 | herbs | 批量删除药材时检查处方引用 | `Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` | 在 BatchDeleteAsync 中逐个检查引用 | 单元测试 |
| T1-X7-09 | HERB-03 | herbs | CanDelete 实现实际处方引用查询 | `Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` | 替代硬编码 true，查询 PrescriptionItem 表 | 单元测试 |
| T1-X7-10 | HERB-09 | herbs | 删除被引用药材时返回 422 | `Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` | 引用存在时返回 HTTP 422 Unprocessable Entity | 集成测试 |

### 专项 S1: 密码哈希 Bug (1项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T1-S1-01 | USER-09 | users | 修复密码哈希 Bug (旧密码覆盖新密码) | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 修正 ChangePasswordAsync 中哈希赋值顺序 | 单元测试: 修改后新密码可登录 |

### 专项 S2: 权限矩阵修复 (9项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T1-S2-01 | USER-01 | users | CanManageUser 补充 Receptionist 角色 | `Server/Services/LYBT.WebAPI/Authorization/` | 权限矩阵中为 Receptionist 增加用户管理权限 | 单元测试 |
| T1-S2-02 | USER-03 | users | 角色变更时 CanManageUser 补充 Receptionist | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 角色变更权限检查包含 Receptionist | 单元测试 |
| T1-S2-03 | USER-04 | users | 单条删除添加"不能删除自己"检查 | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | DeleteAsync 中比对 currentUserId != targetUserId | 单元测试 |
| T1-S2-04 | USER-07 | users | ChangePasswordAsync 调用 PasswordPolicyValidator | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 在修改密码前调用 PasswordPolicyValidator 验证新密码 | 单元测试 |
| T1-S2-05 | USER-11 | users | 修改密码解除 AdminOnly 限制 | `Server/Services/LYBT.WebAPI/Controllers/UsersController.cs` | 修改密码端点允许已认证用户修改自己的密码 | 集成测试 |
| T1-S2-06 | USER-12 | users | 修改个人资料解除 AdminOnly 限制 | `Server/Services/LYBT.WebAPI/Controllers/UsersController.cs` | 个人资料端点允许已认证用户修改自己的信息 | 集成测试 |
| T1-S2-07 | USER-13 | users | ToggleStatus 添加最后管理员保护 | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 禁用前检查是否为最后一个 Admin 角色用户 | 单元测试 |
| T1-S2-08 | USER-15 | users | BatchUpdateStatus 添加权限检查和最后管理员保护 | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 批量操作中复用单条 ToggleStatus 的权限和保护逻辑 | 单元测试 |
| T1-S2-09 | USER-16 | users | GetCurrentUser 解除 AdminOnly 继承 | `Server/Services/LYBT.WebAPI/Controllers/UsersController.cs` | 在 GetCurrentUser 端点添加 [AllowAuthenticated] 覆盖控制器级别 [AdminOnly] | 集成测试 |

### 专项 S3: EditReason 强制校验 (4项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T1-S3-01 | MC-04 | medical-cases | EditReason 在写操作 (Update) 中强制校验 | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 在 UpdateAsync 中检查需要 EditReason 的场景 | 单元测试 |
| T1-S3-02 | MC-06 | medical-cases | EditReason 在编辑操作中强制校验 (同源修复) | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseRules.cs` | RequiresEditReason 方法完善场景覆盖 | 单元测试 |
| T1-S3-03 | MC-14 | medical-cases | 审计日志中传递 EditReason | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseAuditService.cs` | 在审计日志写入时携带 EditReason 字段 | 单元测试 |
| T1-S3-04 | MC-20 | medical-cases | RequiresEditReason 补充"非本人"和"当天已完成"场景 | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseRules.cs` | 在条件判断中增加非创建者编辑和当天已完成状态的检查 | 单元测试 |

---

## Tier 2: 核心功能修复 (42项)

### X8: 打印层级重构 (12项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T2-X8-01 | MC-02 | medical-cases | 实现打印保护逻辑 (已打印医案编辑限制) | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 在写操作前检查 IsPrinted 标记并要求 EditReason | 单元测试 |
| T2-X8-02 | MC-03 | medical-cases | MedicalCase 实体添加 IsPrinted/PrintVersion 字段 | `Server/Core/LYBT.Entities/MedicalCases/` | 新增 IsPrinted、PrintVersion、PrintCount、LastPrintedAt 字段 + Migration | 单元测试 |
| T2-X8-03 | MC-07 | medical-cases | PrescriptionPrintLog 重构为 MedicalCasePrintLog | `Server/Core/LYBT.Entities/Prescriptions/PrescriptionPrintLog.cs` | 重命名实体，增加 PrintType、MedicalCaseId 字段 + Migration | 单元测试 |
| T2-X8-04 | MC-21 | medical-cases | PrintHandler 打印后设置 IsPrinted=true | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 打印成功后回调 MedicalCase 更新 IsPrinted=true | 集成测试 |
| T2-X8-05 | MC-22 | medical-cases | 打印后更新 PrintCount++ 和 LastPrintedAt | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 打印成功后同步更新 PrintCount 和 LastPrintedAt | 集成测试 |
| T2-X8-06 | PRINT-01 | printing | PrintCount 递增逻辑实现 | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 在 PrintAsync 成功路径中递增 PrintCount | 单元测试 |
| T2-X8-07 | PRINT-02 | printing | IsPrinted=true 回写逻辑实现 | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 首次打印成功后设置 IsPrinted=true | 单元测试 |
| T2-X8-08 | PRINT-03 | printing | LastPrintedAt 时间戳更新实现 | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 每次打印成功后记录 LastPrintedAt=DateTime.UtcNow | 单元测试 |
| T2-X8-09 | PRINT-04 | printing | 打印层级从处方层迁移到医案层 | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 重构打印入口从 Prescription 改为 MedicalCase | 集成测试 |
| T2-X8-10 | PRINT-05 | printing | PrintVersion 递增逻辑实现 | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 打印后编辑再打印时 PrintVersion++ | 单元测试 |
| T2-X8-11 | PRINT-06 | printing | 打印版本号快照记录 | `Server/Core/LYBT.Entities/MedicalCases/MedicalCaseAuditLog.cs` | 打印时在审计日志中记录当前版本快照 | 单元测试 |
| T2-X8-12 | PRINT-07 | printing | 创建 MedicalCasePrintLog 实体 | `Server/Core/LYBT.Entities/MedicalCases/` | 新建 MedicalCasePrintLog 实体 + EF 配置 + Migration | 单元测试 |

### X5: 字段验证值对齐 (CODE项) (15项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T2-X5-01 | AUTH-12 | auth | 密码最小长度 6->8 | `Shared/LYBT.Shared.Validators/Auth/LoginRequestValidator.cs`, `Shared/LYBT.Shared.Configuration/Options/Server/PasswordPolicyOptions.cs` | 修改 MinLength 默认值为 8 | 单元测试 |
| T2-X5-02 | USER-19 | users | 用户密码最小长度 6->8 | `Shared/LYBT.Shared.Validators/Users/` | 修改 Validator 中密码 MinLength 为 8 | 单元测试 |
| T2-X5-03 | PAT-20 | patients | IdNumber/PhoneNumber/Address DTO 改为 Required | `Shared/LYBT.Shared.Models/Contracts/Patients/` | DTO 字段从 nullable 改为 non-nullable | 单元测试 |
| T2-X5-04 | HERB-17 | herbs | Effect 字段 DTO 1000->500 对齐实体 | `Shared/LYBT.Shared.Models/Contracts/Herbs/` | DTO MaxLength 从 1000 改为 500 | 单元测试 |
| T2-X5-05 | HERB-18 | herbs | Usage 字段 Validator 200->500 对齐 PRD | `Shared/LYBT.Shared.Validators/Herbs/` | Validator MaxLength 从 200 改为 500 | 单元测试 |
| T2-X5-06 | HERB-23 | herbs | Spec 字段 DTO 50->100 对齐 PRD | `Shared/LYBT.Shared.Models/Contracts/Herbs/` | DTO MaxLength 从 50 改为 100 | 单元测试 |
| T2-X5-07 | HERB-24 | herbs | Unit 字段 DTO 20->10 对齐实体 | `Shared/LYBT.Shared.Models/Contracts/Herbs/` | DTO MaxLength 从 20 改为 10 | 单元测试 |
| T2-X5-08 | FORM-04 | formulas | Effect DTO=200->500 对齐 PRD/Entity | `Shared/LYBT.Shared.Models/Contracts/Formula/` | DTO MaxLength 从 200 改为 500 | 单元测试 |
| T2-X5-09 | FORM-12 | formulas | Desktop Validator 功效/用法改为选填 | `Shared/LYBT.Shared.Validators/Formula/` | 移除 Effect/Usage 的 NotEmpty 规则 | 单元测试 |
| T2-X5-10 | FORM-13 | formulas | Usage DTO=200 改为 500 对齐 FluentValidation | `Shared/LYBT.Shared.Models/Contracts/Formula/` | DTO MaxLength 从 200 改为 500 | 单元测试 |
| T2-X5-11 | MC-32 | medical-cases | 添加 DosageCount>0 校验 | `Shared/LYBT.Shared.Validators/MedicalCase/` | 添加 DosageCount GreaterThan(0) 规则 | 单元测试 |
| T2-X5-12 | MC-35 | medical-cases | OperatorName MaxLength 50->100 | `Server/Core/LYBT.Entities/MedicalCases/MedicalCaseAuditLog.cs` | 修改 MaxLength 从 50 为 100 + Migration | 单元测试 |
| T2-X5-13 | CFG-01 | configuration | DefaultRole "Staff"->"Doctor" | `Shared/LYBT.Shared.Configuration/Options/Server/UserManagementOptions.cs` | 修改 DefaultRole 默认值为 "Doctor" | 单元测试 |
| T2-X5-14 | CFG-02 | configuration | ClientSessionOptions InactivityTimeout 5->15 分钟 | `Shared/LYBT.Shared.Configuration/Options/Client/ClientSessionOptions.cs` | 修改 InactivityTimeout 默认值为 15 (同时修复 SHELL-05/NFR-03 同源问题) | 单元测试 |
| T2-X5-15 | SHELL-05 | desktop-shell | InactivityTimeout Shell 端确认读取正确值 | `Client/Desktop/Shell/Services/Session/SessionLifecycleManager.cs` | 确保 Shell 从 ClientSessionOptions 读取 15 分钟配置 | 手动测试 |

### 专项 S4: 功能Bug/审计/患者状态/系统/打印辅助 (15项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T2-S4-01 | FORM-01 | formulas | FormulaMapper 补充 Herbs 列表映射 | `Server/Modules/LYBT.Module.Formula/Mapping/` | 在 Mapper 中添加 Herbs 集合的映射配置 | 单元测试 |
| T2-S4-02 | FORM-05 | formulas | 修复 TotalPrice 始终为 0 | `Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs` | 在创建/更新时计算 Sum(Herbs.Price * Quantity) | 单元测试 |
| T2-S4-03 | MC-30 | medical-cases | 修复 PrescriptionItem.Usage 错误赋值 | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 修正 Usage 字段赋值为正确的源字段 | 单元测试 |
| T2-S4-04 | LOG-01 | logging | 审计日志保留期 30->365 天 | `Server/Services/LYBT.WebAPI/BackgroundServices/SecurityAuditCleanupService.cs` | 修改清理阈值为 365 天 | 单元测试 |
| T2-S4-05 | LOG-02 | logging | 修复 SensitiveDataAttribute 两份定义冲突 | `Shared/LYBT.Shared.Logging/Masking/`, `Server/Core/LYBT.Entities/Attributes/` | 移除重复定义，保留单一版本 | 单元测试 |
| T2-S4-06 | LOG-03 | logging | CleanupService 使用 Options 配置替代硬编码 | `Shared/LYBT.Shared.Configuration/Options/Server/LoggingOptions.cs`, `Server/Services/LYBT.WebAPI/BackgroundServices/SecurityAuditCleanupService.cs` | 添加 AuditRetentionDays 配置项并注入 | 单元测试 |
| T2-S4-07 | LOG-04 | logging | CleanupService 改为分批删除 | `Server/Services/LYBT.WebAPI/BackgroundServices/SecurityAuditCleanupService.cs` | 用 Take(1000) 分批删除替代全量加载 | 单元测试 |
| T2-S4-08 | PAT-01 | patients | 实现患者状态管理功能 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`, `Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs` | 添加 ToggleStatus/BatchUpdateStatus 方法和端点 | 集成测试 |
| T2-S4-09 | SYS-01 | health-diagnostics | Unhealthy 映射修正 (不再映射为 "Degraded") | `Server/Services/LYBT.WebAPI/HealthCheck/SqlServerHealthCheck.cs` | 修正健康检查状态映射 Unhealthy -> Unhealthy | 单元测试 |
| T2-S4-10 | SYS-02 | health-diagnostics | 健康检查详细响应补充缺失字段 | `Shared/LYBT.Shared.Models/Contracts/Common/HealthCheckResponse.cs` | 添加 Duration、Description 等缺失字段 | 单元测试 |
| T2-S4-11 | ERR-03 | error-handling | 实现异常到通知类型映射 | `Shared/LYBT.Shared.ExceptionHandling/Mappers/ClientErrorMessageMapper.cs` | 根据异常类型映射到 Error/Warning/Info 通知级别 | 单元测试 |
| T2-S4-12 | PRINT-08 | printing | 创建 PrintType 枚举 | `Shared/LYBT.Shared.Models/Enums/` | 新建 PrintType 枚举 (Prescription, MedicalCase, Summary) | 编译通过 |
| T2-S4-13 | PRINT-09 | printing | 实现打印日志写入 | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 打印成功/失败时写入 MedicalCasePrintLog | 集成测试 |
| T2-S4-14 | NFR-03 | nfr | 不活跃超时确认 NFR 引用点 (同源 CFG-02) | `Shared/LYBT.Shared.Configuration/Options/Client/ClientSessionOptions.cs` | 确保 NFR 安全需求引用点读取到 15 分钟 | 手动测试 |
| T2-S4-15 | NFR-04 | nfr | 密码过期配置统一 30->90 天，消除内部矛盾 | `Shared/LYBT.Shared.Configuration/Options/Server/PasswordPolicyOptions.cs` | 统一 ExpirationDays 默认值为 90，移除冲突配置 | 单元测试 |

---

## Tier 3: 体系统一 (26项)

### X1: 错误码 MCCEE 统一 (15项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T3-X1-01 | AUTH-05 | auth | Auth 错误码迁移到 5 位 MCCEE 格式 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs`, `Server/Modules/LYBT.Module.Auth/Services/AuthService.cs` | 替换 3 位错误码为 ERR-1xxxx 格式 | 单元测试 |
| T3-X1-02 | PAT-15 | patients | 实现 ERR-20002 错误码 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs` | 添加 ERR-20002 到 ErrorCode 枚举并在 PatientService 中使用 | 单元测试 |
| T3-X1-03 | PAT-16 | patients | 实现 ERR-20004 错误码 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs` | 添加 ERR-20004 | 单元测试 |
| T3-X1-04 | PAT-17 | patients | 实现 ERR-20005 错误码 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs` | 添加 ERR-20005 | 单元测试 |
| T3-X1-05 | PAT-18 | patients | 实现 ERR-20006 错误码 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs` | 添加 ERR-20006 | 单元测试 |
| T3-X1-06 | PAT-22 | patients | 删除失败返回 422 而非 404 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | 引用冲突时返回 422 + 对应错误码 | 集成测试 |
| T3-X1-07 | HERB-15 | herbs | Herbs 错误码编号对齐 MCCEE 5 位 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs`, `Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` | 添加 ERR-5xxxx 系列并替换旧错误码 | 单元测试 |
| T3-X1-08 | HERB-19 | herbs | 实现 ERR-50106 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs` | 添加分类筛选相关错误码 | 单元测试 |
| T3-X1-09 | HERB-20 | herbs | 实现 ERR-50104 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs` | 添加批量删除相关错误码 | 单元测试 |
| T3-X1-10 | HERB-21 | herbs | 实现 ERR-50202 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs` | 添加导入相关错误码 | 单元测试 |
| T3-X1-11 | FORM-02 | formulas | Formulas 17 个错误码对齐实现 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs`, `Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs` | 补全缺失的 11 个 ERR-6xxxx 错误码 | 单元测试 |
| T3-X1-12 | MC-10 | medical-cases | MedicalCase 错误码迁移到 ERR-3xxxx | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs`, `Server/Modules/LYBT.Module.MedicalCase/Services/` | 添加 ERR-3xxxx 系列并替换旧错误码 | 单元测试 |
| T3-X1-13 | SYNC-14 | sync | 同步模块 20 个 PRD 错误码全部实现 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs`, `Server/Modules/LYBT.Module.Sync/Services/SyncService.cs` | 添加 ERR-8xxxx 系列同步错误码 | 单元测试 |
| T3-X1-14 | ERR-01 | error-handling | ErrorCode 7xxxx 语义重新对应 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs` | 调整 7xxxx 系列错误码的语义映射 | 单元测试 |
| T3-X1-15 | ERR-02 | error-handling | 修复 ClientErrorMessageMapper 无法解析 ERR-10004 | `Shared/LYBT.Shared.ExceptionHandling/Mappers/ClientErrorMessageMapper.cs` | 修复解析逻辑支持 5 位错误码 | 单元测试 |

### X4: Service层ErrorCode替代 (5项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T3-X4-01 | USER-17 | users | UserService 硬编码字符串替换为 ErrorCode 枚举 | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 替换所有硬编码错误消息为 ErrorCode.Xxx | 单元测试 |
| T3-X4-02 | USER-18 | users | 用户名重复返回 409 Conflict | `Server/Modules/LYBT.Module.Users/Services/UserService.cs`, `Server/Services/LYBT.WebAPI/Controllers/UsersController.cs` | 重复检查返回 409 + ErrorCode 枚举 | 集成测试 |
| T3-X4-03 | HERB-16 | herbs | HerbService 硬编码字符串替换为 ErrorCode 枚举 | `Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` | 替换所有硬编码错误消息 | 单元测试 |
| T3-X4-04 | FORM-11 | formulas | FormulaService 硬编码字符串替换为 ErrorCode 枚举 | `Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs` | 替换所有硬编码错误消息 | 单元测试 |
| T3-X4-05 | AUTH-14 | auth | TokenRevoked 提示语义精确化 + ErrorCode 使用 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorMessages.cs` | 区分 Expired vs Revoked 错误消息并使用 ErrorCode | 单元测试 |

### X6: 分页筛选迁移Repository (6项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T3-X6-01 | USER-20 | users | role/status 筛选移到 Repository 的 IQueryable 链 | `Server/Modules/LYBT.Module.Users/Repositories/UserRepository.cs` | 将 Service 层内存过滤改为 Repository Where 条件 | 单元测试 |
| T3-X6-02 | HERB-07 | herbs | 分类筛选移到 Repository 的 IQueryable 链 | `Server/Modules/LYBT.Module.Herbs/Repositories/HerbRepository.cs` | 将 Service 层内存过滤改为 Repository Where 条件 | 单元测试 |
| T3-X6-03 | FORM-15 | formulas | 分类筛选移到 Repository 的 IQueryable 链 | `Server/Modules/LYBT.Module.Formula/Repositories/FormulaRepository.cs` | 将 Service 层内存过滤改为 Repository Where 条件 | 单元测试 |
| T3-X6-04 | FORM-17 | formulas | 待验证列表改为分页查询 | `Server/Modules/LYBT.Module.Formula/Repositories/FormulaRepository.cs` | 添加分页参数替代全量加载 | 单元测试 |
| T3-X6-05 | MC-18 | medical-cases | GetListDtoAsync 筛选移到 Repository | `Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs` | 将内存过滤改为 IQueryable Where 链 | 单元测试 |
| T3-X6-06 | ERR-04 | error-handling | HTTP 429 映射到错误码 | `Shared/LYBT.Shared.ExceptionHandling/Mappers/ClientErrorMessageMapper.cs` | 添加 429 -> "请求过于频繁" 映射 | 单元测试 |

---

## Tier 4: 本地模式补齐 (37项)

### X2: IDataSource+导入导出 (22项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T4-X2-01 | USER-10 | users | Desktop ChangePasswordAsync 实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalUserDataSource.cs` | 实现本地密码修改逻辑 (哈希+SQLite 更新) | 单元测试 |
| T4-X2-02 | USER-21 | users | LocalUserDataSource 删除保护完善 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalUserDataSource.cs` | 添加"不能删除自己"和最后管理员保护 | 单元测试 |
| T4-X2-03 | USER-22 | users | IUserDataSource 添加 RestoreAsync | `Client/Desktop/Core/LYBT.Desktop.Contracts/DataSources/IUserDataSource.cs`, `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalUserDataSource.cs` | 接口+本地实现软删除恢复 | 单元测试 |
| T4-X2-04 | USER-23 | users | IUserDataSource 添加 BatchDeleteAsync | `Client/Desktop/Core/LYBT.Desktop.Contracts/DataSources/IUserDataSource.cs`, `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalUserDataSource.cs` | 接口+本地实现批量删除 | 单元测试 |
| T4-X2-05 | USER-25 | users | IUserDataSource 添加 ResetPasswordAsync | `Client/Desktop/Core/LYBT.Desktop.Contracts/DataSources/IUserDataSource.cs`, `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalUserDataSource.cs` | 接口+本地实现密码重置 | 单元测试 |
| T4-X2-06 | USER-27 | users | LocalUserDataSource 状态切换保护完善 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalUserDataSource.cs` | 添加最后管理员保护逻辑 | 单元测试 |
| T4-X2-07 | USER-28 | users | IUserDataSource 添加批量启用/禁用 | `Client/Desktop/Core/LYBT.Desktop.Contracts/DataSources/IUserDataSource.cs`, `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalUserDataSource.cs` | 接口+本地实现 BatchUpdateStatus | 单元测试 |
| T4-X2-08 | USER-29 | users | 本地模式 GetCurrentUser 实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalUserDataSource.cs` | 从本地会话获取当前用户信息 | 单元测试 |
| T4-X2-09 | PAT-07 | patients | 本地模式批量导入实现 (替代 null) | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalPatientDataSource.cs` | 实现 Excel 批量导入到 SQLite | 集成测试 |
| T4-X2-10 | PAT-08 | patients | 本地模式导出实现 (替代 null) | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalPatientDataSource.cs` | 实现导出到 Excel | 集成测试 |
| T4-X2-11 | PAT-23 | patients | Desktop 端实现引用检查 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalPatientDataSource.cs` | 本地 SQLite 查询 MedicalCase 引用 | 单元测试 |
| T4-X2-12 | PAT-24 | patients | Desktop 端批量引用检查实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalPatientDataSource.cs` | 本地批量查询 MedicalCase 引用 | 单元测试 |
| T4-X2-13 | HERB-10 | herbs | 本地模式批量启用/禁用实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalHerbDataSource.cs` | 实现 BatchUpdateStatus 本地操作 | 单元测试 |
| T4-X2-14 | HERB-11 | herbs | 本地模式 Excel 导入实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalHerbDataSource.cs` | 实现 ImportFromExcelAsync | 集成测试 |
| T4-X2-15 | HERB-12 | herbs | 本地模式 JSON 导入实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalHerbDataSource.cs` | 实现 ImportFromJsonAsync | 集成测试 |
| T4-X2-16 | HERB-13 | herbs | 本地模式导出实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalHerbDataSource.cs` | 实现 ExportAsync (Excel/JSON) | 集成测试 |
| T4-X2-17 | HERB-14 | herbs | 本地模式引用检查实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalHerbDataSource.cs` | 本地 SQLite 查询 PrescriptionItem 引用 | 单元测试 |
| T4-X2-18 | HERB-22 | herbs | 本地模式导入模板下载 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalHerbDataSource.cs` | 实现 GetImportTemplateAsync | 单元测试 |
| T4-X2-19 | FORM-06 | formulas | Desktop 端恢复延迟绑定验证方法 | `Client/Desktop/Modules/LYBT.Desktop.Formula/Services/` | 重新实现已删除的 ValidateDelayedBindingAsync | 单元测试 |
| T4-X2-20 | FORM-07 | formulas | Desktop 端恢复待验证列表方法 | `Client/Desktop/Modules/LYBT.Desktop.Formula/Services/` | 重新实现已删除的 GetPendingValidationListAsync | 单元测试 |
| T4-X2-21 | FORM-08 | formulas | Desktop 端本地批量导入实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalFormulaDataSource.cs` | 实现 ImportAsync 本地操作 | 集成测试 |
| T4-X2-22 | FORM-10 | formulas | Desktop 端本地导出实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalFormulaDataSource.cs` | 实现 ExportAsync 本地操作 | 集成测试 |

### 专项 S5: 打印模板完善 (11项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T4-S5-01 | PRINT-10 | printing | 实现打印失败日志记录 | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 在 catch 块中写入失败日志 | 单元测试 |
| T4-S5-02 | PRINT-11 | printing | 远程模式打印日志 API 实现 | `Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs` | 添加 POST /api/medical-cases/{id}/print-logs 端点 | 集成测试 |
| T4-S5-03 | PRINT-12 | printing | 本地模式打印日志存储实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalMedicalCaseDataSource.cs` | 添加 PrintLog 本地 SQLite 表和写入 | 集成测试 |
| T4-S5-04 | PRINT-13 | printing | 模板字体楷体改为宋体 | `Client/Desktop/Core/LYBT.Desktop.Printing/Templates/PrescriptionPrintTemplate.xaml` | 修改 FontFamily 为 SimSun | 手动测试 |
| T4-S5-05 | PRINT-14 | printing | 模板边距 15mm 改为 8mm | `Client/Desktop/Core/LYBT.Desktop.Printing/Templates/PrescriptionPrintTemplate.xaml` | 修改 Margin 值 | 手动测试 |
| T4-S5-06 | PRINT-15 | printing | 添加诊所信息区 | `Client/Desktop/Core/LYBT.Desktop.Printing/Templates/PrescriptionPrintTemplate.xaml`, `Client/Desktop/Core/LYBT.Desktop.Printing/Models/PrescriptionPrintModel.cs` | 模板顶部添加诊所名称/地址/电话 | 手动测试 |
| T4-S5-07 | PRINT-16 | printing | 完善诊断信息区 | `Client/Desktop/Core/LYBT.Desktop.Printing/Templates/PrescriptionPrintTemplate.xaml` | 添加完整中医诊断信息绑定 | 手动测试 |
| T4-S5-08 | PRINT-17 | printing | 渲染煎法标注 | `Client/Desktop/Core/LYBT.Desktop.Printing/Templates/PrescriptionPrintTemplate.xaml` | 在药材列表下方添加煎法说明区域 | 手动测试 |
| T4-S5-09 | PRINT-18 | printing | 实现分页规则 (>12味药材分页) | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 添加分页逻辑，超过 12 味自动分页 | 手动测试 |
| T4-S5-10 | PRINT-20 | printing | DoctorName 自动绑定当前医生 | `Client/Desktop/Core/LYBT.Desktop.Printing/Models/PrescriptionPrintModel.cs` | 从当前用户会话获取 DoctorName | 单元测试 |
| T4-S5-11 | PRINT-21 | printing | 费用计算纳入 Discount | `Client/Desktop/Core/LYBT.Desktop.Printing/Models/PrescriptionPrintModel.cs` | TotalPrice 计算时乘以 Discount 系数 | 单元测试 |

### 专项 S6: Desktop-shell 功能 (4项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T4-S6-01 | SHELL-01 | desktop-shell | 实现菜单可见性矩阵 | `Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` | 根据当前用户角色控制菜单项可见性 | 手动测试 |
| T4-S6-02 | SHELL-07 | desktop-shell | 本地模式菜单不可用逻辑实现 | `Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` | 本地模式下禁用仅远程可用的菜单项 (如同步) | 手动测试 |
| T4-S6-03 | SHELL-06 | desktop-shell | 导航历史添加 20 条上限 | `Client/Desktop/Shell/Services/` | 导航历史列表超过 20 条时移除最旧记录 | 单元测试 |
| T4-S6-04 | SHELL-10 | desktop-shell | 本地模式账户设置分支处理 | `Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs` | 根据运行模式切换本地/远程账户设置逻辑 | 手动测试 |

---

## Tier 5: 细节完善 (66项)

### P2 功能完善 (45项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T5-P2-01 | AUTH-03 | auth | 实现远程模式 FailedLoginCount | `Server/Modules/LYBT.Module.Auth/Services/AuthService.cs` | 登录失败时递增计数，达阈值时锁定账户 | 单元测试 |
| T5-P2-02 | AUTH-04 | auth | UserDisabled 返回 403 替代 401 | `Server/Modules/LYBT.Module.Auth/Services/AuthService.cs` | 禁用用户登录时返回 403 Forbidden | 单元测试 |
| T5-P2-03 | AUTH-06 | auth | HMAC 校验失败时清除篡改凭据文件 | `Client/Desktop/Core/LYBT.Desktop.Foundation/Security/CredentialVault.cs` | HMAC 校验失败时删除本地凭据文件 | 单元测试 |
| T5-P2-04 | AUTH-07 | auth | 实现 30 天绝对过期 | `Server/Modules/LYBT.Module.Auth/Services/JwtService.cs` | 在 Token 生成时记录创建时间，刷新时检查绝对过期 | 单元测试 |
| T5-P2-05 | AUTH-09 | auth | TokenExpired 时尝试 AutoLogin 降级 | `Client/Desktop/Core/LYBT.Desktop.Foundation/Security/TokenLifecycleService.cs` | Token 过期时检查本地凭据，尝试自动登录 | 集成测试 |
| T5-P2-06 | AUTH-17 | auth | 过期 Token 错误码区分 Expired vs Invalid | `Server/Modules/LYBT.Module.Auth/Services/JwtService.cs` | Token 验证失败时细分错误类型 | 单元测试 |
| T5-P2-07 | AUTH-18 | auth | "记住密码"自动勾选"记住用户名" | `Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs` | RememberPassword 勾选时自动设置 RememberUsername=true | 单元测试 |
| T5-P2-08 | AUTH-20 | auth | 本地模式简化版状态机实现 | `Client/Desktop/Core/LYBT.Desktop.Foundation/Security/AuthenticationStateMachine.cs` | 本地模式使用简化的认证状态流 | 单元测试 |
| T5-P2-09 | MC-01 | medical-cases | 创建医案时检查患者状态 (禁用患者不可创建) | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 在 CreateAsync 中查询患者状态 | 单元测试 |
| T5-P2-10 | MC-05 | medical-cases | 添加 TcmDiagnosis 非空校验 | `Shared/LYBT.Shared.Validators/Consultation/` | 完成操作时要求 TcmDiagnosis 非空 | 单元测试 |
| T5-P2-11 | MC-09 | medical-cases | 实现医案编号自动生成 | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 在 CreateAsync 中生成 MC-YYYYMMDD-XXXX 格式编号 | 单元测试 |
| T5-P2-12 | MC-11 | medical-cases | HasPrescription=false 时清除已有处方数据 | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 在 UpdateAsync 中检测 HasPrescription 变更并清除 | 单元测试 |
| T5-P2-13 | MC-12 | medical-cases | 实现处方编号自动生成 | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 在处方创建时生成 RX-YYYYMMDD-XXXX 格式编号 | 单元测试 |
| T5-P2-14 | MC-13 | medical-cases | 添加处方 Items 为空时的验证 | `Shared/LYBT.Shared.Validators/Prescriptions/` | 添加 Items.Count > 0 验证规则 | 单元测试 |
| T5-P2-15 | MC-15 | medical-cases | 完成操作中验证 Items 非空 | `Shared/LYBT.Shared.Validators/Prescriptions/` | 在完成流程验证中也检查 Items.Count > 0 | 单元测试 |
| T5-P2-16 | MC-17 | medical-cases | 非当天本人取消需要 Reason 检查 | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseStateService.cs` | 在 CancelAsync 中检查是否非本人或非当天，要求 Reason | 单元测试 |
| T5-P2-17 | MC-23 | medical-cases | 验方导入过滤 ValidationStatus | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 导入验方时仅选择 ValidationStatus=Validated 的验方 | 单元测试 |
| T5-P2-18 | MC-24 | medical-cases | 验方导入过滤 Status=Enabled | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 导入验方时仅选择 Status=Enabled 的验方 | 单元测试 |
| T5-P2-19 | MC-25 | medical-cases | 验方导入跳过禁用药材 | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 导入时过滤 Herb.Status=Disabled 的药材 | 单元测试 |
| T5-P2-20 | MC-26 | medical-cases | 验方导入价格实时获取 | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 从药材库当前价格替代验方中的快照价格 | 单元测试 |
| T5-P2-21 | MC-27 | medical-cases | 历史复制跳过禁用药材 | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 复制历史处方时过滤已禁用药材 | 单元测试 |
| T5-P2-22 | MC-28 | medical-cases | 历史复制价格实时获取 | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 复制时从药材库获取当前价格 | 单元测试 |
| T5-P2-23 | MC-29 | medical-cases | 历史复制记录 ReferencedFormulas | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 复制时记录来源验方ID到 ReferencedFormulas 字段 | 单元测试 |
| T5-P2-24 | PAT-02 | patients | 实现身份证号必填+唯一性检查 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`, `Shared/LYBT.Shared.Validators/Patients/` | Validator 添加 NotEmpty + Service 添加唯一性查询 | 单元测试 |
| T5-P2-25 | PAT-03 | patients | 更新时添加手机号唯一性检查 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | UpdateAsync 中排除自身后查重 | 单元测试 |
| T5-P2-26 | PAT-04 | patients | 更新时添加身份证号唯一性检查 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | UpdateAsync 中排除自身后查重 | 单元测试 |
| T5-P2-27 | PAT-13 | patients | Receptionist 查询过滤 Disabled 患者 | `Server/Modules/LYBT.Module.Patients/Repositories/PatientRepository.cs` | Receptionist 角色查询时自动过滤 Status=Disabled | 单元测试 |
| T5-P2-28 | PAT-14 | patients | 导入时添加身份证号唯一性检查 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | 导入前批量查重 IdNumber | 单元测试 |
| T5-P2-29 | PAT-19 | patients | 创建 API 返回 201 替代 200 | `Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs` | CreateAsync 返回 CreatedAtAction (201) | 集成测试 |
| T5-P2-30 | PAT-21 | patients | Receptionist 添加 CRU 权限 | `Server/Services/LYBT.WebAPI/Authorization/` | 权限矩阵中为 Receptionist 增加患者 CRU 权限 | 集成测试 |
| T5-P2-31 | USER-24 | users | MustChangeOnNextLogin 标记实现 | `Server/Modules/LYBT.Module.Users/Services/UserService.cs`, `Server/Modules/LYBT.Module.Auth/Services/AuthService.cs` | 重置密码后设标记，登录时检查并强制跳转修改密码 | 集成测试 |
| T5-P2-32 | USER-26 | users | ChangeProfileAsync 重新生成 PinYinCode | `Server/Modules/LYBT.Module.Users/Services/UserService.cs` | 姓名修改后调用 PinYinHelper 重新生成拼音码 | 单元测试 |
| T5-P2-33 | HERB-04 | herbs | CreateAsync 添加拼音码自动生成 | `Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` | 在 CreateAsync 中调用 PinYinHelper 生成 | 单元测试 |
| T5-P2-34 | HERB-08 | herbs | 名称变更时重新生成拼音码 | `Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` | 在 UpdateAsync 中检测名称变更并重新生成 | 单元测试 |
| T5-P2-35 | FORM-03 | formulas | Server 端校验 Herbs 列表非空 | `Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs` | 创建验方时检查 Herbs.Count > 0 | 单元测试 |
| T5-P2-36 | FORM-09 | formulas | 导出 Excel 包含药材组成详情 | `Server/Modules/LYBT.Module.Formula/Services/FormulaImportExportService.cs` | 导出时 Include Herbs 列表写入 Sheet | 集成测试 |
| T5-P2-37 | FORM-16 | formulas | 本地模式批量启用/禁用实现 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalFormulaDataSource.cs` | 实现 BatchUpdateStatus 本地操作 | 单元测试 |
| T5-P2-38 | FORM-18 | formulas | 本地模式内置导入模板 | `Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalFormulaDataSource.cs` | 实现 GetImportTemplateAsync | 单元测试 |
| T5-P2-39 | SYNC-06 | sync | SyncMetadataDto 补充缺失字段 | `Shared/LYBT.Shared.Models/Contracts/Sync/SyncMetadataDto.cs` | 添加 PRD 定义的缺失字段 | 单元测试 |
| T5-P2-40 | SYNC-07 | sync | GetMetadataAsync 使用 IgnoreQueryFilters | `Server/Modules/LYBT.Module.Sync/Services/SyncService.cs` | 查询时添加 IgnoreQueryFilters 以包含软删除记录 | 单元测试 |
| T5-P2-41 | SYNC-09 | sync | OverwriteConflicts 改为配置项 | `Server/Modules/LYBT.Module.Sync/Services/SyncService.cs` | 从配置读取 OverwriteConflicts 替代硬编码 false | 单元测试 |
| T5-P2-42 | SYNC-10 | sync | 同步前添加网络/Token 检查 | `Client/Desktop/Modules/LYBT.Desktop.Sync/ViewModels/SyncViewModel.cs` | 同步前检查网络连通性和 Token 有效性 | 手动测试 |
| T5-P2-43 | SYNC-12 | sync | 完善同步结果汇总 | `Client/Desktop/Modules/LYBT.Desktop.Sync/ViewModels/SyncViewModel.cs` | 显示成功/失败/跳过数量和失败原因 | 手动测试 |
| T5-P2-44 | CFG-03 | configuration | 添加 FeatureToggle CardReaderEnabled | `Shared/LYBT.Shared.Configuration/Options/Client/FeatureToggleOptions.cs` | 添加 CardReaderEnabled 属性 | 单元测试 |
| T5-P2-45 | CFG-04 | configuration | JWT SecretKey 验证增强 | `Shared/LYBT.Shared.Configuration/Validation/` | 除字符串长度外增加密钥强度校验 | 单元测试 |

### P3 细节修复 (21项)

| 任务ID | 偏差ID | 模块 | 描述 | 修改文件 | 修改方式 | 验证方法 |
|--------|--------|------|------|----------|----------|----------|
| T5-P3-01 | CFG-05 | configuration | Important 配置缺失改为警告 (不阻止启动) | `Server/Services/LYBT.WebAPI/Extensions/UnifiedApplicationInitialization.cs` | 非必要配置缺失时日志 Warning 而非 throw | 集成测试 |
| T5-P3-02 | ERR-05 | error-handling | Token 相关错误码消息映射 | `Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorMessages.cs` | 添加 Token Expired/Invalid/Revoked 消息映射 | 单元测试 |
| T5-P3-03 | ERR-06 | error-handling | 追踪码与 Severity 自动关联 | `Shared/LYBT.Shared.ExceptionHandling/Handlers/` | 根据异常 Severity 自动决定是否生成追踪码 | 单元测试 |
| T5-P3-04 | NFR-02 | nfr | 审计日志保留 365 天 NFR 合规确认 (同源 LOG-01) | `Server/Services/LYBT.WebAPI/BackgroundServices/SecurityAuditCleanupService.cs` | 验证 NFR-SEC-005 引用点与 LOG-01 修复一致 | 单元测试 |
| T5-P3-05 | NFR-05 | nfr | Server 端缓存失效映射实现 | `Shared/LYBT.Shared.Utilities/Extensions/ServiceCollection/CacheExtensions.cs` | 实体变更时清除对应缓存键 | 单元测试 |
| T5-P3-06 | NFR-06 | nfr | Desktop 端写后缓存失效实现 | `Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/` | 写操作后通知缓存层失效 | 单元测试 |
| T5-P3-07 | MC-36 | medical-cases | 审计字段补充 Prescription.Usage | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseAuditService.cs` | 审计日志中记录处方 Usage 变更 | 单元测试 |
| T5-P3-08 | MC-37 | medical-cases | pending 端点添加 doctorId 参数 | `Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs` | 添加 doctorId 查询参数过滤 | 集成测试 |
| T5-P3-09 | MC-38 | medical-cases | 历史复制包含 DosageCount/Discount | `Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` | 复制时携带 DosageCount 和 Discount 字段 | 单元测试 |
| T5-P3-10 | PAT-25 | patients | CreateAsync (DTO版) 添加手机号唯一性检查 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | DTO 入口也执行手机号查重 | 单元测试 |
| T5-P3-11 | PAT-26 | patients | 导入行数限制修复 off-by-one | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | 修正行数比较条件 (> 改为 >=，或反之) | 单元测试 |
| T5-P3-12 | PAT-27 | patients | 导入模板 IdNumber 列标记必填 | `Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | 导出模板时 IdNumber 列添加必填标注 | 手动测试 |
| T5-P3-13 | PAT-28 | patients | PatientStatus 复用 CommonStatus | `Shared/LYBT.Shared.Models/Enums/` | 移除独立 PatientStatus，使用 CommonStatus | 单元测试 |
| T5-P3-14 | PRINT-22 | printing | A4/A5 排版差异处理 | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 根据纸张类型切换排版参数 | 手动测试 |
| T5-P3-15 | PRINT-24 | printing | 药材名称过长截断处理 | `Client/Desktop/Core/LYBT.Desktop.Printing/Templates/PrescriptionPrintTemplate.xaml` | 添加 TextTrimming=CharacterEllipsis | 手动测试 |
| T5-P3-16 | PRINT-25 | printing | 空处方打印校验 | `Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs` | 打印前检查 Items 非空，否则弹出提示 | 手动测试 |
| T5-P3-17 | SHELL-02 | desktop-shell | 登出时清除导航历史 | `Client/Desktop/Shell/Services/Session/SessionLifecycleManager.cs` | 在 LogoutAsync 中清空 NavigationJournal | 单元测试 |
| T5-P3-18 | SHELL-03 | desktop-shell | 模块加载增加角色粒度 | `Client/Desktop/Core/LYBT.Desktop.Foundation/Modules/` | 区分 Admin/Doctor/Receptionist 模块加载 | 手动测试 |
| T5-P3-19 | SHELL-09 | desktop-shell | 账户设置添加 Email 编辑支持 | `Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs`, `Client/Desktop/Shell/Controls/AccountSettingsControl.xaml` | 添加 Email 字段绑定和保存逻辑 | 手动测试 |
| T5-P3-20 | SYNC-17 | sync | Checksum 字段类型/长度对齐 PRD | `Shared/LYBT.Shared.Models/Contracts/Sync/SyncMetadataDto.cs` | 修改 Checksum 字段类型和长度匹配 PRD 规格 | 单元测试 |
| T5-P3-21 | SYNC-18 | sync | 状态栏同步标识实现 | `Client/Desktop/Modules/LYBT.Desktop.Sync/ViewModels/SyncViewModel.cs` | 在状态栏显示最近同步时间/状态图标 | 手动测试 |

---

## 附录A: 按模块索引

### auth (12项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| AUTH-01 | T1-X3-01 | 1 |
| AUTH-03 | T5-P2-01 | 5 |
| AUTH-04 | T5-P2-02 | 5 |
| AUTH-05 | T3-X1-01 | 3 |
| AUTH-06 | T5-P2-03 | 5 |
| AUTH-07 | T5-P2-04 | 5 |
| AUTH-09 | T5-P2-05 | 5 |
| AUTH-12 | T2-X5-01 | 2 |
| AUTH-14 | T3-X4-05 | 3 |
| AUTH-17 | T5-P2-06 | 5 |
| AUTH-18 | T5-P2-07 | 5 |
| AUTH-20 | T5-P2-08 | 5 |

### users (29项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| USER-01 | T1-S2-01 | 1 |
| USER-02 | T1-X3-02 | 1 |
| USER-03 | T1-S2-02 | 1 |
| USER-04 | T1-S2-03 | 1 |
| USER-05 | T1-X3-03 | 1 |
| USER-06 | T1-X3-04 | 1 |
| USER-07 | T1-S2-04 | 1 |
| USER-08 | T1-X3-05 | 1 |
| USER-09 | T1-S1-01 | 1 |
| USER-10 | T4-X2-01 | 4 |
| USER-11 | T1-S2-05 | 1 |
| USER-12 | T1-S2-06 | 1 |
| USER-13 | T1-S2-07 | 1 |
| USER-14 | T1-X3-06 | 1 |
| USER-15 | T1-S2-08 | 1 |
| USER-16 | T1-S2-09 | 1 |
| USER-17 | T3-X4-01 | 3 |
| USER-18 | T3-X4-02 | 3 |
| USER-19 | T2-X5-02 | 2 |
| USER-20 | T3-X6-01 | 3 |
| USER-21 | T4-X2-02 | 4 |
| USER-22 | T4-X2-03 | 4 |
| USER-23 | T4-X2-04 | 4 |
| USER-24 | T5-P2-31 | 5 |
| USER-25 | T4-X2-05 | 4 |
| USER-26 | T5-P2-32 | 5 |
| USER-27 | T4-X2-06 | 4 |
| USER-28 | T4-X2-07 | 4 |
| USER-29 | T4-X2-08 | 4 |

### patients (28项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| PAT-01 | T2-S4-08 | 2 |
| PAT-02 | T5-P2-24 | 5 |
| PAT-03 | T5-P2-25 | 5 |
| PAT-04 | T5-P2-26 | 5 |
| PAT-05 | T1-X7-01 | 1 |
| PAT-06 | T1-X7-02 | 1 |
| PAT-07 | T4-X2-09 | 4 |
| PAT-08 | T4-X2-10 | 4 |
| PAT-09 | T1-X7-03 | 1 |
| PAT-10 | T1-X7-04 | 1 |
| PAT-11 | T1-X7-05 | 1 |
| PAT-12 | T1-X7-06 | 1 |
| PAT-13 | T5-P2-27 | 5 |
| PAT-14 | T5-P2-28 | 5 |
| PAT-15 | T3-X1-02 | 3 |
| PAT-16 | T3-X1-03 | 3 |
| PAT-17 | T3-X1-04 | 3 |
| PAT-18 | T3-X1-05 | 3 |
| PAT-19 | T5-P2-29 | 5 |
| PAT-20 | T2-X5-03 | 2 |
| PAT-21 | T5-P2-30 | 5 |
| PAT-22 | T3-X1-06 | 3 |
| PAT-23 | T4-X2-11 | 4 |
| PAT-24 | T4-X2-12 | 4 |
| PAT-25 | T5-P3-10 | 5 |
| PAT-26 | T5-P3-11 | 5 |
| PAT-27 | T5-P3-12 | 5 |
| PAT-28 | T5-P3-13 | 5 |

### herbs (22项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| HERB-01 | T1-X7-07 | 1 |
| HERB-02 | T1-X7-08 | 1 |
| HERB-03 | T1-X7-09 | 1 |
| HERB-04 | T5-P2-33 | 5 |
| HERB-07 | T3-X6-02 | 3 |
| HERB-08 | T5-P2-34 | 5 |
| HERB-09 | T1-X7-10 | 1 |
| HERB-10 | T4-X2-13 | 4 |
| HERB-11 | T4-X2-14 | 4 |
| HERB-12 | T4-X2-15 | 4 |
| HERB-13 | T4-X2-16 | 4 |
| HERB-14 | T4-X2-17 | 4 |
| HERB-15 | T3-X1-07 | 3 |
| HERB-16 | T3-X4-03 | 3 |
| HERB-17 | T2-X5-04 | 2 |
| HERB-18 | T2-X5-05 | 2 |
| HERB-19 | T3-X1-08 | 3 |
| HERB-20 | T3-X1-09 | 3 |
| HERB-21 | T3-X1-10 | 3 |
| HERB-22 | T4-X2-18 | 4 |
| HERB-23 | T2-X5-06 | 2 |
| HERB-24 | T2-X5-07 | 2 |

### formulas (17项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| FORM-01 | T2-S4-01 | 2 |
| FORM-02 | T3-X1-11 | 3 |
| FORM-03 | T5-P2-35 | 5 |
| FORM-04 | T2-X5-08 | 2 |
| FORM-05 | T2-S4-02 | 2 |
| FORM-06 | T4-X2-19 | 4 |
| FORM-07 | T4-X2-20 | 4 |
| FORM-08 | T4-X2-21 | 4 |
| FORM-09 | T5-P2-36 | 5 |
| FORM-10 | T4-X2-22 | 4 |
| FORM-11 | T3-X4-04 | 3 |
| FORM-12 | T2-X5-09 | 2 |
| FORM-13 | T2-X5-10 | 2 |
| FORM-15 | T3-X6-03 | 3 |
| FORM-16 | T5-P2-37 | 5 |
| FORM-17 | T3-X6-04 | 3 |
| FORM-18 | T5-P2-38 | 5 |

### medical-cases (32项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| MC-01 | T5-P2-09 | 5 |
| MC-02 | T2-X8-01 | 2 |
| MC-03 | T2-X8-02 | 2 |
| MC-04 | T1-S3-01 | 1 |
| MC-05 | T5-P2-10 | 5 |
| MC-06 | T1-S3-02 | 1 |
| MC-07 | T2-X8-03 | 2 |
| MC-09 | T5-P2-11 | 5 |
| MC-10 | T3-X1-12 | 3 |
| MC-11 | T5-P2-12 | 5 |
| MC-12 | T5-P2-13 | 5 |
| MC-13 | T5-P2-14 | 5 |
| MC-14 | T1-S3-03 | 1 |
| MC-15 | T5-P2-15 | 5 |
| MC-17 | T5-P2-16 | 5 |
| MC-18 | T3-X6-05 | 3 |
| MC-20 | T1-S3-04 | 1 |
| MC-21 | T2-X8-04 | 2 |
| MC-22 | T2-X8-05 | 2 |
| MC-23 | T5-P2-17 | 5 |
| MC-24 | T5-P2-18 | 5 |
| MC-25 | T5-P2-19 | 5 |
| MC-26 | T5-P2-20 | 5 |
| MC-27 | T5-P2-21 | 5 |
| MC-28 | T5-P2-22 | 5 |
| MC-29 | T5-P2-23 | 5 |
| MC-30 | T2-S4-03 | 2 |
| MC-32 | T2-X5-11 | 2 |
| MC-35 | T2-X5-12 | 2 |
| MC-36 | T5-P3-07 | 5 |
| MC-37 | T5-P3-08 | 5 |
| MC-38 | T5-P3-09 | 5 |

### printing (23项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| PRINT-01 | T2-X8-06 | 2 |
| PRINT-02 | T2-X8-07 | 2 |
| PRINT-03 | T2-X8-08 | 2 |
| PRINT-04 | T2-X8-09 | 2 |
| PRINT-05 | T2-X8-10 | 2 |
| PRINT-06 | T2-X8-11 | 2 |
| PRINT-07 | T2-X8-12 | 2 |
| PRINT-08 | T2-S4-12 | 2 |
| PRINT-09 | T2-S4-13 | 2 |
| PRINT-10 | T4-S5-01 | 4 |
| PRINT-11 | T4-S5-02 | 4 |
| PRINT-12 | T4-S5-03 | 4 |
| PRINT-13 | T4-S5-04 | 4 |
| PRINT-14 | T4-S5-05 | 4 |
| PRINT-15 | T4-S5-06 | 4 |
| PRINT-16 | T4-S5-07 | 4 |
| PRINT-17 | T4-S5-08 | 4 |
| PRINT-18 | T4-S5-09 | 4 |
| PRINT-20 | T4-S5-10 | 4 |
| PRINT-21 | T4-S5-11 | 4 |
| PRINT-22 | T5-P3-14 | 5 |
| PRINT-24 | T5-P3-15 | 5 |
| PRINT-25 | T5-P3-16 | 5 |

### sync (8项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| SYNC-06 | T5-P2-39 | 5 |
| SYNC-07 | T5-P2-40 | 5 |
| SYNC-09 | T5-P2-41 | 5 |
| SYNC-10 | T5-P2-42 | 5 |
| SYNC-12 | T5-P2-43 | 5 |
| SYNC-14 | T3-X1-13 | 3 |
| SYNC-17 | T5-P3-20 | 5 |
| SYNC-18 | T5-P3-21 | 5 |

### desktop-shell (8项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| SHELL-01 | T4-S6-01 | 4 |
| SHELL-02 | T5-P3-17 | 5 |
| SHELL-03 | T5-P3-18 | 5 |
| SHELL-05 | T2-X5-15 | 2 |
| SHELL-06 | T4-S6-03 | 4 |
| SHELL-07 | T4-S6-02 | 4 |
| SHELL-09 | T5-P3-19 | 5 |
| SHELL-10 | T4-S6-04 | 4 |

### configuration (5项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| CFG-01 | T2-X5-13 | 2 |
| CFG-02 | T2-X5-14 | 2 |
| CFG-03 | T5-P2-44 | 5 |
| CFG-04 | T5-P2-45 | 5 |
| CFG-05 | T5-P3-01 | 5 |

### error-handling (6项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| ERR-01 | T3-X1-14 | 3 |
| ERR-02 | T3-X1-15 | 3 |
| ERR-03 | T2-S4-11 | 2 |
| ERR-04 | T3-X6-06 | 3 |
| ERR-05 | T5-P3-02 | 5 |
| ERR-06 | T5-P3-03 | 5 |

### logging (4项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| LOG-01 | T2-S4-04 | 2 |
| LOG-02 | T2-S4-05 | 2 |
| LOG-03 | T2-S4-06 | 2 |
| LOG-04 | T2-S4-07 | 2 |

### health-diagnostics (2项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| SYS-01 | T2-S4-09 | 2 |
| SYS-02 | T2-S4-10 | 2 |

### nfr (5项)

| 偏差ID | 任务ID | Tier |
|--------|--------|------|
| NFR-02 | T5-P3-04 | 5 |
| NFR-03 | T2-S4-14 | 2 |
| NFR-04 | T2-S4-15 | 2 |
| NFR-05 | T5-P3-05 | 5 |
| NFR-06 | T5-P3-06 | 5 |

---

## 附录B: 依赖关系

### 跨任务依赖

| 依赖源 | 依赖目标 | 依赖类型 | 说明 |
|--------|----------|----------|------|
| T2-X8-02 (MC-03) | T2-X8-01/04~12 | 数据模型依赖 | IsPrinted/PrintVersion 字段必须先创建，打印回写才有目标 |
| T2-X8-12 (PRINT-07) | T2-X8-09~11, T4-S5-01~03 | 实体依赖 | MedicalCasePrintLog 实体必须先创建，日志写入才有表 |
| T2-S4-12 (PRINT-08) | T2-X8-12, T4-S5-01~03 | 枚举依赖 | PrintType 枚举先创建，日志记录才能分类 |
| T3-X1-01~15 | T3-X4-01~05 | 错误码依赖 | MCCEE 错误码先注册，Service 层才能引用 |
| T1-X3-01~06 | T1-S2-01~09 | 建议顺序 | Token 撤销先实现，权限修复才能完整验证 |
| T1-X7-01~06 | T1-X7-07~10 | 模式一致 | 患者引用检查和药材引用检查可复用相同模式 |
| T2-X5-14 (CFG-02) | T2-X5-15 (SHELL-05) | 同源配置 | ClientSessionOptions 修改一次，Shell 端自动生效 |
| T2-S4-04 (LOG-01) | T5-P3-04 (NFR-02) | 同源修复 | 审计日志保留期修改一处，NFR 合规同时满足 |
| T5-P2-24~28 (PAT-02~04,14) | T4-X2-09 (PAT-07) | 逻辑依赖 | 身份证唯一性先实现，导入时才能正确查重 |
| T4-X2-14~16 (HERB-11~13) | T4-X2-18 (HERB-22) | 功能依赖 | 导入导出先实现，模板下载才有意义 |
| T4-X2-21 (FORM-08) | T5-P2-38 (FORM-18) | 功能依赖 | 本地导入先实现，导入模板才有意义 |

### 建议执行顺序

```
Sprint 1 (Tier 1, 30项):
  第1周: T1-X3 (Token撤销 6项) + T1-S1 (密码哈希 1项)
  第2周: T1-S2 (权限矩阵 9项) + T1-X7 (引用检查 10项)
  第3周: T1-S3 (EditReason 4项) + 回归测试

Sprint 2 (Tier 2, 42项):
  第1周: T2-X8-02, T2-X8-12, T2-S4-12 (数据模型先行)
  第2周: T2-X8 其余 (打印回写链) + T2-S4 (功能Bug 15项)
  第3周: T2-X5 (字段验证值 15项)

Sprint 3 (Tier 3, 26项):
  第1周: T3-X1 (错误码注册 15项)
  第2周: T3-X4 (Service层替换 5项) + T3-X6 (分页筛选 6项)

Sprint 4 (Tier 4, 37项):
  第1周: T4-X2-01~08 (Users 本地模式 8项)
  第2周: T4-X2-09~18 (Patients+Herbs 本地模式 10项)
  第3周: T4-X2-19~22 (Formulas 4项) + T4-S5 (打印模板 11项) + T4-S6 (Shell 4项)

Sprint 5+ (Tier 5, 66项):
  按 P2 优先处理功能完善 (45项)，P3 细节修复 (21项) 排后
```

---

## 附录C: 任务计数验证

| 分组 | 任务ID 范围 | 计数 |
|------|------------|------|
| T1-X3 | T1-X3-01 ~ T1-X3-06 | 6 |
| T1-X7 | T1-X7-01 ~ T1-X7-10 | 10 |
| T1-S1 | T1-S1-01 | 1 |
| T1-S2 | T1-S2-01 ~ T1-S2-09 | 9 |
| T1-S3 | T1-S3-01 ~ T1-S3-04 | 4 |
| **Tier 1 小计** | | **30** |
| T2-X8 | T2-X8-01 ~ T2-X8-12 | 12 |
| T2-X5 | T2-X5-01 ~ T2-X5-15 | 15 |
| T2-S4 | T2-S4-01 ~ T2-S4-15 | 15 |
| **Tier 2 小计** | | **42** |
| T3-X1 | T3-X1-01 ~ T3-X1-15 | 15 |
| T3-X4 | T3-X4-01 ~ T3-X4-05 | 5 |
| T3-X6 | T3-X6-01 ~ T3-X6-06 | 6 |
| **Tier 3 小计** | | **26** |
| T4-X2 | T4-X2-01 ~ T4-X2-22 | 22 |
| T4-S5 | T4-S5-01 ~ T4-S5-11 | 11 |
| T4-S6 | T4-S6-01 ~ T4-S6-04 | 4 |
| **Tier 4 小计** | | **37** |
| T5-P2 | T5-P2-01 ~ T5-P2-45 | 45 |
| T5-P3 | T5-P3-01 ~ T5-P3-21 | 21 |
| **Tier 5 小计** | | **66** |
| **总计** | | **201** |

### 偏差ID 覆盖率核对

以下确认 201 个 CODE 偏差全部覆盖且无重复 (按模块):

| 模块 | CODE 总数 | 已覆盖偏差ID |
|------|-----------|-------------|
| auth (12) | 12 | AUTH-01,03,04,05,06,07,09,12,14,17,18,20 |
| users (29) | 29 | USER-01~09,10~16,17~20,21~29 |
| patients (28) | 28 | PAT-01~14,15~22,23~28 |
| herbs (22) | 22 | HERB-01~04,07~24 (跳过 HERB-05,06=PRD) |
| formulas (17) | 17 | FORM-01~13,15~18 (跳过 FORM-14=PRD) |
| medical-cases (32) | 32 | MC-01~07,09~15,17,18,20~30,32,35~38 |
| printing (23) | 23 | PRINT-01~18,20~22,24,25 |
| sync (8) | 8 | SYNC-06,07,09,10,12,14,17,18 |
| desktop-shell (8) | 8 | SHELL-01~03,05~07,09,10 |
| configuration (5) | 5 | CFG-01~05 |
| error-handling (6) | 6 | ERR-01~06 |
| logging (4) | 4 | LOG-01~04 |
| health-diagnostics (2) | 2 | SYS-01,02 |
| nfr (5) | 5 | NFR-02~06 |
| **合计** | **201** | **201 个偏差ID 全部覆盖，无重复** |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-21 | v1.0 | 初始版本: 201 项 CODE 偏差修复任务清单，5 Tier / 8 横切面 / 6 专项 |
