using LYBT.Entities.Auth;
using LYBT.Entities.Common;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Identity.Infrastructure;

/// <summary>
/// 认证用户模块数据库上下文（A-31-C3a 合并 AuthDbContext + UsersDbContext）。
/// 使用共享连接、同库不同 DbContext 的方式实现模块数据隔离（ADR-0017 方案 A）。
/// 物理表由 AppDbContext 单一迁移链管理，本上下文仅做逻辑隔离。
/// </summary>
public class IdentityDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    /// <summary>认证会话集</summary>
    public DbSet<AuthSession> AuthSessions { get; set; } = null!;

    /// <summary>安全审计日志集</summary>
    public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; } = null!;

    /// <summary>系统日志集</summary>
    public DbSet<SystemLog> SystemLogs { get; set; } = null!;

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── 来自 AuthDbContext ──
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

        // IDENTITY-DBCONTEXT-FIX (P0): 应用 UserConfiguration——ApplicationUser.LastLoginTime
        // 映射到 Users 表 LastLoginAt 列；漏挂时 EF 按属性名查 LastLoginTime → SqlException → 登录 500
        modelBuilder.ApplyConfiguration(new UserConfiguration());

        // 软删除全局查询过滤器（与 AppDbContext ApplyOptimizations 保持一致）
        modelBuilder.Entity<SecurityAuditLog>().HasQueryFilter(e => !e.IsDeleted);

        // ── 来自 UsersDbContext ──
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(e => e.UserName).HasMaxLength(32).IsRequired();
            entity.Property(e => e.RealName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PinYinCode).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.Role).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();

            // 索引
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.PinYinCode);
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.Status);

            // 软删除全局查询过滤器（与 AppDbContext 保持一致）
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}
