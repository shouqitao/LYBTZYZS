# Design: 全局统一审计系统

## Context

凌隐宝堂中医诊所系统需要完整的审计追踪能力，用于:
- 医疗数据合规要求
- 操作追溯与问题排查
- 管理员监控与报表

### 现有实现分析

| 审计类型 | 实体 | 特点 | 位置 |
|---------|------|------|------|
| MedicalCaseAuditLog | 医案 | 字段级变更追踪(JSON) | LYBT.Entities.MedicalCase |
| SecurityAuditLog | 认证事件 | 事件级记录(IP/UA) | LYBT.Entities.Auth |

### 技术约束
- 现有WPF+Prism MVVM架构
- EF Core数据访问
- Refit HTTP客户端
- 各模块独立部署(Module化)

## Goals / Non-Goals

### Goals
- 建立统一审计架构，支持字段级变更追踪和事件级记录
- 覆盖所有关键业务实体(Patient, Prescription, Herb, Formula, User, Consultation)
- 前端提供一致的审计日志查看体验
- 保持与现有MedicalCase审计的兼容性

### Non-Goals
- 不替换现有SecurityAuditLog(认证审计保持独立)
- 不实现实时审计通知
- 不实现审计数据归档策略(Phase 2考虑)

## Decisions

### 1. 采用双表策略

**Decision**: 新建 `EntityAuditLogs` 通用表 + 保留现有 `MedicalCaseAuditLogs`

**Rationale**:
- 避免大规模数据迁移风险
- MedicalCase审计已稳定运行
- 新实体统一使用通用表

**Alternatives**:
- (A) 全部迁移到统一表 - 风险高，需数据迁移
- (B) 每个实体独立审计表 - 代码重复，维护成本高

### 2. 泛型审计服务接口

```csharp
public interface IAuditService<TEntity> where TEntity : BaseEntity
{
    Task LogCreateAsync(TEntity entity, Guid operatorId, string operatorName, UserRole role);
    Task LogUpdateAsync(TEntity? before, TEntity after, Guid operatorId, string operatorName, UserRole role, string? reason = null);
    Task LogDeleteAsync(TEntity entity, Guid operatorId, string operatorName, UserRole role, string? reason = null);
    Task<(List<EntityAuditLog> Logs, int TotalCount)> GetLogsAsync(Guid entityId, int page = 1, int pageSize = 20);
}
```

**Rationale**:
- 泛型设计减少重复代码
- 与现有IMedicalCaseAuditService保持签名兼容
- 支持依赖注入的模块化使用

### 3. EntityAuditLog 统一实体

```csharp
public class EntityAuditLog
{
    public Guid Id { get; set; }
    public string EntityType { get; set; }    // "Patient", "Prescription", etc.
    public Guid EntityId { get; set; }
    public Guid OperatorId { get; set; }
    public string OperatorName { get; set; }
    public UserRole OperatorRole { get; set; }
    public AuditOperationType OperationType { get; set; }
    public string? ChangedFields { get; set; }  // JSON
    public string? OldValues { get; set; }      // JSON
    public string? NewValues { get; set; }      // JSON
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 4. 前端通用审计对话框

**Decision**: 创建 `EntityAuditLogDialog` 通用组件

**Structure**:
```
LYBT.Desktop.Infrastructure/
└── Dialogs/
    ├── EntityAuditLogDialog.xaml
    └── EntityAuditLogDialogViewModel.cs
```

**Rationale**:
- 放置在Infrastructure层供所有Module复用
- 通过参数区分实体类型
- 统一UI风格和交互

### 5. 分阶段实施

**Phase 1**: 基础架构 + Patient审计
**Phase 2**: Prescription + Herb审计
**Phase 3**: Formula + User审计
**Phase 4**: Consultation审计
**Phase 5**: 前端统一入口

## Risks / Trade-offs

| Risk | Impact | Mitigation |
|------|--------|------------|
| 性能影响(审计写入) | Medium | 异步写入，批量提交 |
| JSON字段大小膨胀 | Low | 仅记录变更字段，不记录完整实体 |
| 前端对话框复用性 | Low | 通过EntityType参数化处理 |

## Migration Plan

1. 创建 `EntityAuditLogs` 表 (新增迁移)
2. 实现 `EntityAuditService<T>` 基础服务
3. 逐个实体集成审计调用
4. 前端添加审计查看入口
5. 测试验证各实体审计功能

**Rollback**: 各Phase独立部署，可单独回滚

## Open Questions

1. 是否需要支持审计日志导出功能? (建议Phase 2)
2. 审计数据保留期限策略? (建议Phase 2)
3. 是否需要批量操作审计聚合? (当前每条记录独立)
