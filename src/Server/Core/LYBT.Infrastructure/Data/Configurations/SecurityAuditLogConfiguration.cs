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

        // 字段配置
        entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
        entity.Property(e => e.UserType).HasMaxLength(50);
        entity.Property(e => e.UserName).HasMaxLength(256);
        entity.Property(e => e.IpAddress).HasMaxLength(50);
        entity.Property(e => e.UserAgent).HasMaxLength(500);
        entity.Property(e => e.ErrorMessage).HasMaxLength(500);
        entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
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
