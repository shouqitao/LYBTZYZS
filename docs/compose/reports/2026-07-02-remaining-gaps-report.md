# 剩余功能缺口与优化项

> 日期: 2026-07-02 | 来源: 全模块代码扫描 + 测试覆盖分析 + DI审计

## 一、死代码清理 (优先级: 高)

| 文件 | 问题 | 建议 |
|------|------|------|
| `PatientsModule.cs` 中 `PatientsDbContext` 注册 | 已切换到 AppDbContext，此注册无效 | 删除 |
| `PatientsDbContext.cs` 整个文件 | 无任何 handler/repo 引用 | 删除文件 |
| `RegistrationDbContext.cs` 整个文件 | 无任何 handler/repo 引用 | 删除文件 |
| `IFormulaService` + `FormulaService` (393行) | FormulasController 已全部迁移至 MediatR，此服务无注入方 | 删除接口+实现，清理 DI 注册 |
| `PatientService` (600行) | 从未注册 DI，无控制器注入 | 删除 |
| `HerbsModule.cs` 注释掉的 `IHerbCategoryRepository` | 完全无引用 | 删除注释 |
| `SystemLog` DbSet | Serilog 直写 SQL，EF DbSet 从未被 handler 使用 | 保留（迁移兼容），标记低优先级 |

## 二、未使用 ErrorCode (优先级: 中)

| 模块 | 未使用错误码 | 数量 |
|------|-------------|------|
| Registration | 80001-80008 全部 | 8 |
| Prescription | 40001-40007 全部 | 7 |
| Herbs | 50003-50006 | 4 |
| Formula | 60003, 60005, 60006 | 3 |
| Users | 10003, 10005, 10008-10010, 10012, 10014-10015 | 8 |
| MedicalCase (旧) | 30003-30008 | 6 |

**建议**: Registration 和 Prescription 的 ErrorCode 应在 handler 中使用替代字符串错误。其余可在后续迭代中逐步接入。

## 三、测试覆盖缺口 (优先级: 高)

### 完全无测试的模块

| 模块 | Handler 数 | 测试数 | 缺口 |
|------|-----------|--------|------|
| Reports | 3 queries | 0 | **整个模块无测试** |

### 部分缺口

| 模块 | 缺测试的 Handler | 严重度 |
|------|-----------------|--------|
| Users | RestoreUserCommandHandler | 高 |
| Users | BatchEnableUsersCommandHandler | 高 |
| Users | BatchDisableUsersCommandHandler | 高 |
| Herbs | RestoreHerbCommandHandler (仅403路径) | 中 |
| Formula | BatchDeleteFormulasCommandHandler | 中 |
| Formula | ValidateFormulaHerbCommandHandler | 中 |
| Formula | RestoreFormulaCommandHandler | 中 |
| MedicalCase | SuspendMedicalCaseCommandHandler | 高 |
| MedicalCase | CompleteMedicalCaseCommandHandler | 高 |
| MedicalCase | GetMedicalCasePermissionsQueryHandler | 中 |
| MedicalCase | GetMedicalCaseAuditLogsQueryHandler | 中 |
| MedicalCase | GetMedicalCasesBatchQueryHandler | 中 |
| MedicalCase | GetPatientConsultationsQueryHandler | 中 |
| MedicalCase | GetPatientPrescriptionsQueryHandler | 中 |
| Registration | QuickVisitCommandHandler (仅间接覆盖) | 低 |

**总计**: 78 个 handler 中 15 个无直接测试覆盖 (19.2%)

## 四、Desktop Stub/Null 实现 (优先级: 中)

| 文件 | 行 | 代码 | 说明 |
|------|---|------|------|
| `PatientSelectionWorkspaceContext.cs` | 31 | `CurrentPatient => null` | 临床工作台无患者绑定 |
| `PatientSelectionWorkspaceContext.cs` | 34 | `SessionManager => null` | 会话管理不可用 |
| `ApiHealthMonitor.cs` | 57,75 | `#pragma warning disable CS1998` | async 方法无 await |
| `SessionManager.cs` | 17 | `#pragma warning disable CS0067` | 事件声明但未触发 |
| `StartupOptimizationService.cs` | 14 | `#pragma warning disable CS0067` | 事件声明但未触发 |

## 五、v1.0 待实现功能 (优先级: 按业务价值)

| US | 功能 | 说明 | 工作量估计 |
|----|------|------|-----------|
| US-AUTH-006 | 令牌族旋转撤销 | ITokenRevocationService 需实现 | 4-6 人日 |
| US-AUTH-007 | 安全审计日志 | ISecurityAuditService 需实现 | 4-6 人日 |
| US-AUTH-013 | 本地限流 5次/分 | LocalWebAPI 限流中间件 | 2-3 人日 |
| US-MC-017 | 审计日志实体 | AuditLog 实体 + 完整字段 diff | 3-5 人日 |
| US-PRINT-004 | 打印记录回写 | PrintLog 实体 + 回写端点 | 2-3 人日 |
| US-REG-008 | SignalR 实时推送 | Hub + 客户端降级 | 5-8 人日 |
| US-SHELL-010 | Velopack 打包 | Desktop 安装包 | 3-5 人日 |
| US-SHELL-011 | 首次初始化向导 | FirstRunSetup 扩展 | 2-3 人日 |
| US-SHELL-013 | 备份恢复 UI | ILocalDbBackupService + UI | 3-5 人日 |
| US-SHELL-018 | sysadmin 配置中心 | SysadminHomeView 扩展 | 3-5 人日 |

## 六、建议执行顺序

### 第一批: 死代码清理 (1天)
1. 删除 PatientsDbContext / RegistrationDbContext
2. 删除 IFormulaService / FormulaService
3. 删除 PatientService
4. 清理 HerbsModule 注释

### 第二批: 测试补全 (2-3天)
1. Reports 模块 3 个测试 (最高优先)
2. Users Restore/BatchEnable/BatchDisable 3 个测试
3. MedicalCase Suspend/Complete 2 个测试
4. Formula BatchDelete/ValidateHerb/Restore 3 个测试

### 第三批: ErrorCode 对齐 (1天)
1. Registration handler 使用 ErrorCode 替代字符串
2. 清理完全未使用的 ErrorCode 枚举值

### 第四批: v1.0 功能 (按业务优先级)
1. US-PRINT-004 打印回写 (最简单)
2. US-AUTH-013 本地限流
3. US-MC-017 审计日志实体
4. US-AUTH-006/007 安全服务
5. US-REG-008 SignalR
