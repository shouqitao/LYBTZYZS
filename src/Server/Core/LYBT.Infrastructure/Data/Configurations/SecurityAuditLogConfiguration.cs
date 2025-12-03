using LYBT.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// SecurityAuditLog 实体 EF Core 配置
/// </summary>
public class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> entity)
    {
        entity.ToTable("SecurityAuditLogs");
        entity.HasKey(e => e.Id);

        // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
        entity.Property(e => e.EventType).IsRequired();
        entity.Property(e => e.CreatedAt).IsRequired();

        // Issue #1869: 按EventType和时间查询优化索引
        entity.HasIndex(e => new { e.EventType, e.CreatedAt })
            .HasDatabaseName("IX_SecurityAuditLogs_EventType_CreatedAt")
            .IsDescending(false, true); // EventType升序，CreatedAt降序

        // Issue #1869: 按UserId和时间查询优化索引
        entity.HasIndex(e => new { e.UserId, e.CreatedAt })
            .HasDatabaseName("IX_SecurityAuditLogs_UserId_CreatedAt")
            .IsDescending(false, true); // UserId升序，CreatedAt降序
    }
}
