using LYBT.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// SecurityAuditLog 实体 EF Core 配置 - 审计表复合查询索引
/// </summary>
public class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
    {
        // EventType 作为复合索引键列，必须有界长度（SQL Server 不允许索引 nvarchar(max)）
        builder.Property(e => e.EventType).HasMaxLength(50);

        builder.HasIndex(e => new { e.EventType, e.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_SecurityAuditLogs_EventType_CreatedAt");

        builder.HasIndex(e => new { e.UserId, e.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_SecurityAuditLogs_UserId_CreatedAt");
    }
}
