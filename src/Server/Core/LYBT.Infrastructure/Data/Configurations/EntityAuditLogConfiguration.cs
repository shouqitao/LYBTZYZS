using LYBT.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// EntityAuditLog 实体 EF Core 配置
/// OpenSpec: add-global-audit-system
/// </summary>
public class EntityAuditLogConfiguration : IEntityTypeConfiguration<EntityAuditLog>
{
    public void Configure(EntityTypeBuilder<EntityAuditLog> entity)
    {
        entity.ToTable("EntityAuditLogs");
        entity.HasKey(e => e.Id);

        // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
        entity.Property(e => e.EntityType).IsRequired();
        entity.Property(e => e.EntityId).IsRequired();
        entity.Property(e => e.OperatorId).IsRequired();
        entity.Property(e => e.OperatorName).IsRequired();
        entity.Property(e => e.OperatorRole).IsRequired();
        entity.Property(e => e.OperationType).IsRequired();
        entity.Property(e => e.CreatedAt).IsRequired();

        // JSON字段 - 不指定HasColumnType以支持跨数据库兼容（SQLite测试 + SQL Server生产）
        entity.Property(e => e.ChangedFields);
        entity.Property(e => e.OldValues);
        entity.Property(e => e.NewValues);

        // 按EntityType+EntityId查询优化索引（最常用查询）
        entity.HasIndex(e => new { e.EntityType, e.EntityId, e.CreatedAt })
            .HasDatabaseName("IX_EntityAuditLogs_EntityType_EntityId_CreatedAt")
            .IsDescending(false, false, true); // EntityType和EntityId升序，CreatedAt降序

        // 按OperatorId查询优化索引
        entity.HasIndex(e => new { e.OperatorId, e.CreatedAt })
            .HasDatabaseName("IX_EntityAuditLogs_OperatorId_CreatedAt")
            .IsDescending(false, true);

        // 按CreatedAt查询优化索引（清理归档场景）
        entity.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_EntityAuditLogs_CreatedAt")
            .IsDescending(true);
    }
}
