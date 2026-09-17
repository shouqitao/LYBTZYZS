using System.Security.Claims;
using LYBT.Entities.Auth;
using LYBT.Entities.Common;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Data.Configurations;
using Microsoft.AspNetCore.Http;
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
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>认证会话集</summary>
    public DbSet<AuthSession> AuthSessions { get; set; } = null!;

    /// <summary>安全审计日志集</summary>
    public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; } = null!;

    /// <summary>系统日志集</summary>
    public DbSet<SystemLog> SystemLogs { get; set; } = null!;

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// R-12：模块级 DbContext 接入审计扩展（S-5）——与 AppDbContext 同构。
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.SetAuditFields(GetCurrentUserId());
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc cref="SaveChangesAsync(CancellationToken)"/>
    public override int SaveChanges()
    {
        this.SetAuditFields(GetCurrentUserId());
        return base.SaveChanges();
    }

    private Guid? GetCurrentUserId()
    {
        try
        {
            var userIdClaim = _httpContextAccessor?.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;
        }
        catch
        {
            // 非 HTTP 上下文（后台/测试）无用户归属
        }
        return null;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── 来自 AuthDbContext ──
        // 复用 Infrastructure 的 AuthSession 配置类（与 AppDbContext 保持一致；StringLength 由实体特性定义）
        modelBuilder.ApplyConfiguration(new AuthSessionConfiguration());

        // 复用 Infrastructure 的审计日志/系统日志配置类（与 AppDbContext 保持一致）
        modelBuilder.ApplyConfiguration(new SecurityAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new SystemLogConfiguration());

        // IDENTITY-DBCONTEXT-FIX (P0): 应用 UserConfiguration——ApplicationUser.LastLoginTime
        // 映射到 Users 表 LastLoginAt 列；漏挂时 EF 按属性名查 LastLoginTime → SqlException → 登录 500
        modelBuilder.ApplyConfiguration(new UserConfiguration());

        // 软删除全局查询过滤器（与 AppDbContext ApplyOptimizations 保持一致）
        modelBuilder.Entity<SecurityAuditLog>().HasQueryFilter(e => !e.IsDeleted);

        // ApplicationUser 业务字段/表名/软删除由 UserConfiguration 统一维护（上方已 ApplyConfiguration）；
        // Identity 特有配置（UserName 唯一索引、Normalized* 等）由 base.OnModelCreating 提供，此处不重复覆盖。
    }
}
