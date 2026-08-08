using LYBT.Entities.Auth;
using LYBT.Entities.Common;
using LYBT.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Auth.Infrastructure;

/// <summary>
/// 认证模块数据库上下文。使用共享连接、同库不同 DbContext 的方式实现模块数据隔离。
/// ADR-0017: 认证会话 + 安全审计日志 + 系统日志
/// 物理表由 AppDbContext 单一迁移链管理（方案 A），本上下文仅做逻辑隔离。
/// </summary>
public class AuthDbContext : DbContext
{
    /// <summary>认证会话集</summary>
    public DbSet<AuthSession> AuthSessions { get; set; } = null!;

    /// <summary>安全审计日志集</summary>
    public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; } = null!;

    /// <summary>系统日志集</summary>
    public DbSet<SystemLog> SystemLogs { get; set; } = null!;

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.ToTable("AuthSessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45).IsRequired();
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Status);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.ExpiryTime);
        });

        // 复用 Infrastructure 的审计日志/系统日志配置类（与 AppDbContext 保持一致）
        modelBuilder.ApplyConfiguration(new SecurityAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new SystemLogConfiguration());

        // 软删除全局查询过滤器（与 AppDbContext ApplyOptimizations 保持一致）
        modelBuilder.Entity<SecurityAuditLog>().HasQueryFilter(e => !e.IsDeleted);
    }
}
