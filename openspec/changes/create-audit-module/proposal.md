# Proposal: create-audit-module

## Summary

创建独立的审计模块(LYBT.Module.Audit)，提供统一的审计日志服务，支持医案、患者、用户等多实体的修改追溯。

## Motivation

1. **合规需求**: 医疗系统需要完整的修改追溯能力，满足监管要求
2. **多实体审计**: 医案、患者、用户等多个实体都需要审计，需要统一方案
3. **跨模块查询**: 管理后台需要统一查询所有审计记录
4. **避免重复**: 各模块分别实现审计会导致代码重复、格式不一致

## Goals

1. **统一审计服务**: 提供IAuditService接口，所有模块共用
2. **整体快照存储**: 采用JSON快照方式记录修改前后状态
3. **统一存储**: 单一AuditLogs表，通过EntityType区分实体类型
4. **查询API**: 提供按实体、时间、操作人等维度的查询接口
5. **UI集成**: 提供审计日志查看对话框，支持diff高亮

## Non-Goals

1. 不实现字段级别的变更追踪（通过快照对比在UI层实现）
2. 不实现审计数据归档（后期扩展）
3. 不实现审计报表导出（后期扩展）

## Design Overview

### 模块结构

```
src/Server/Modules/LYBT.Module.Audit/
├── Interfaces/
│   └── IAuditService.cs
├── Services/
│   └── AuditService.cs
├── Repositories/
│   └── AuditRepository.cs
└── AuditModule.cs

src/Shared/LYBT.Shared.Models/Contracts/Audit/
├── AuditLogDto.cs
├── AuditLogListDto.cs
├── AuditLogCreateDto.cs
└── AuditQueryDto.cs
```

### 核心接口

```csharp
public interface IAuditService
{
    /// <summary>
    /// 记录实体变更
    /// </summary>
    Task LogChangeAsync<T>(
        string entityType,
        Guid entityId,
        string operationType,
        T beforeSnapshot,
        T afterSnapshot,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询实体审计日志
    /// </summary>
    Task<List<AuditLogDto>> GetEntityLogsAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);
}
```

### 数据库设计

```sql
CREATE TABLE AuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    EntityType NVARCHAR(50) NOT NULL,
    EntityId UNIQUEIDENTIFIER NOT NULL,
    OperationType NVARCHAR(20) NOT NULL,
    OperatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    OperatedBy UNIQUEIDENTIFIER NOT NULL,
    OperatorName NVARCHAR(50) NOT NULL,
    Reason NVARCHAR(500),
    BeforeSnapshot NVARCHAR(MAX),
    AfterSnapshot NVARCHAR(MAX),

    INDEX IX_AuditLogs_Entity (EntityType, EntityId),
    INDEX IX_AuditLogs_Time (OperatedAt DESC)
);
```

## Risks and Mitigations

| 风险 | 缓解措施 |
|------|----------|
| JSON快照存储空间大 | 仅记录关键字段，定期归档历史数据 |
| 审计影响性能 | 异步写入，不阻塞主业务 |
| 跨模块依赖 | 通过DI注入，保持松耦合 |

## Success Criteria

1. 编译通过，无错误
2. 医案模块成功集成审计服务
3. 审计日志查看对话框正常工作
4. 单元测试覆盖核心逻辑

## Related

- **依赖于**: `optimize-medicalcase-api` - 医案保存逻辑
- **被依赖**: 后续Patient、User模块审计集成
