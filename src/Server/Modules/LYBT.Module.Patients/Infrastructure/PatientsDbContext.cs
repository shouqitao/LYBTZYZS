using System.Security.Claims;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Data.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Infrastructure;

/// <summary>
/// 患者模块数据库上下文。使用共享连接、同库不同 DbContext 的方式实现模块数据隔离。
/// 物理表由 AppDbContext 单一迁移链管理（ADR-0017 方案 A），本上下文仅做逻辑隔离。
/// </summary>
public class PatientsDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>患者集</summary>
    public DbSet<Patient> Patients { get; set; } = null!;

    public PatientsDbContext(DbContextOptions<PatientsDbContext> options) : base(options)
    {
    }

    public PatientsDbContext(
        DbContextOptions<PatientsDbContext> options,
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

        // 复用 Infrastructure 的实体配置类（与 AppDbContext 保持一致）
        modelBuilder.ApplyConfiguration(new PatientConfiguration());

        // 软删除全局查询过滤器（统一走 Infrastructure 扩展，与 AppDbContext ApplyOptimizations 保持一致）
        modelBuilder.ApplySoftDeleteFilters<Patient>();
    }
}
