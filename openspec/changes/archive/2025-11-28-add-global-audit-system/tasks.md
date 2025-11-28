# Tasks: 添加全局统一审计系统

## Phase 1: 基础架构

- [ ] 1.1 创建 `EntityAuditLog` 实体
  - 位置: `LYBT.Entities/Common/EntityAuditLog.cs`
  - 字段: Id, EntityType, EntityId, OperatorId, OperatorName, OperatorRole, OperationType, ChangedFields, OldValues, NewValues, Reason, CreatedAt

- [ ] 1.2 创建 `IAuditService<TEntity>` 接口
  - 位置: `LYBT.Infrastructure/Services/IAuditService.cs`
  - 方法: LogCreateAsync, LogUpdateAsync, LogDeleteAsync, GetLogsAsync

- [ ] 1.3 实现 `EntityAuditService<TEntity>` 服务
  - 位置: `LYBT.Infrastructure/Services/EntityAuditService.cs`
  - 实现字段差异检测
  - 实现JSON序列化

- [ ] 1.4 创建数据库迁移
  - 添加 EntityAuditLogs 表
  - 添加索引: EntityType+EntityId, OperatorId, CreatedAt

- [ ] 1.5 添加 `EntityAuditLogDto` 和 API响应模型
  - 位置: `LYBT.Shared.Models/Contracts/Common/`

## Phase 2: Patient审计集成

- [ ] 2.1 在 `PatientService` 集成审计
  - CreateAsync 调用 LogCreateAsync
  - UpdateAsync 调用 LogUpdateAsync
  - DeleteAsync 调用 LogDeleteAsync

- [ ] 2.2 添加 `PatientController.GetAuditLogs` 端点
  - GET /api/patients/{id}/audit-logs

- [ ] 2.3 前端 `IPatientApi` 添加审计接口
  - GetAuditLogsAsync(Guid patientId, int page, int pageSize)

## Phase 3: Prescription审计集成

- [ ] 3.1 在 `PrescriptionService` 集成审计
- [ ] 3.2 添加 `PrescriptionController.GetAuditLogs` 端点
- [ ] 3.3 前端 `IPrescriptionApi` 添加审计接口

## Phase 4: Herb审计集成

- [ ] 4.1 在 `HerbService` 集成审计
- [ ] 4.2 添加 `HerbController.GetAuditLogs` 端点
- [ ] 4.3 前端 `IHerbApi` 添加审计接口

## Phase 5: Formula审计集成

- [ ] 5.1 在 `FormulaService` 集成审计
- [ ] 5.2 添加 `FormulaController.GetAuditLogs` 端点
- [ ] 5.3 前端 `IFormulaApi` 添加审计接口

## Phase 6: User审计集成

- [ ] 6.1 在 `UserService` 集成审计
- [ ] 6.2 添加 `UserController.GetAuditLogs` 端点
- [ ] 6.3 前端 `IUserApi` 添加审计接口

## Phase 7: Consultation审计集成

- [ ] 7.1 在 `ConsultationService` 集成审计
- [ ] 7.2 添加 `ConsultationController.GetAuditLogs` 端点
- [ ] 7.3 前端 `IConsultationApi` 添加审计接口

## Phase 8: 前端通用审计对话框

- [ ] 8.1 创建 `EntityAuditLogDialog.xaml`
  - 位置: `LYBT.Desktop.Infrastructure/Dialogs/`
  - 支持通过EntityType参数区分显示

- [ ] 8.2 创建 `EntityAuditLogDialogViewModel.cs`
  - 通过依赖注入获取对应API
  - 支持分页加载

- [ ] 8.3 在Infrastructure模块注册对话框

## Phase 9: 各管理界面集成审计入口

- [ ] 9.1 PatientManagementView 添加"变更记录"按钮
- [ ] 9.2 PrescriptionManagementView 添加"变更记录"按钮
- [ ] 9.3 HerbManagementView 添加"变更记录"按钮
- [ ] 9.4 FormulaManagementView 添加"变更记录"按钮
- [ ] 9.5 UserManagementView 添加"变更记录"按钮

## Phase 10: 测试与验证

- [ ] 10.1 EntityAuditService 单元测试
- [ ] 10.2 各实体审计集成测试
- [ ] 10.3 前端审计对话框功能验证
- [ ] 10.4 全解决方案编译验证

## Phase 11: 文档与归档

- [ ] 11.1 更新 global-audit spec
- [ ] 11.2 归档此 OpenSpec 变更
