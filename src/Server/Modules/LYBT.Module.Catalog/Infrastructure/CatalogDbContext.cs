using System.Security.Claims;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Data.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Catalog.Infrastructure;

/// <summary>
/// 药材方剂目录模块数据库上下文（A-31-C3b 由 HerbsDbContext 升级合并 FormulaDbContext）。
/// 使用共享连接、同库不同 DbContext 的方式实现模块数据隔离（ADR-0017）。
/// 包含 Herb（药材）/ Formula（验方）/ FormulaHerbItem（验方明细）三类业务实体；
/// 另注册引用检查所需实体（处方项/处方/医案/患者/验方项），供 HerbReferenceRepository
/// 检查药材被哪些处方/验方引用（物理表由 AppDbContext 单一迁移链管理，无迁移障碍）。
/// </summary>
public class CatalogDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>药材集</summary>
    public DbSet<Herb> Herbs { get; set; } = null!;

    /// <summary>验方集</summary>
    public DbSet<Formula> Formulas { get; set; } = null!;

    // 引用检查实体（只读查询用，非本模块业务实体）
    /// <summary>处方项集（引用检查）</summary>
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;

    /// <summary>处方集（引用检查）</summary>
    public DbSet<Prescription> Prescriptions { get; set; } = null!;

    /// <summary>医案集（引用检查）</summary>
    public DbSet<MedicalCase> MedicalCases { get; set; } = null!;

    /// <summary>患者集（引用检查）</summary>
    public DbSet<Patient> Patients { get; set; } = null!;

    /// <summary>验方项集（引用检查）</summary>
    public DbSet<FormulaHerbItem> FormulaHerbItems { get; set; } = null!;

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public CatalogDbContext(
        DbContextOptions<CatalogDbContext> options,
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

        // 复用 Infrastructure 的实体配置类（与 AppDbContext 保持一致，仅逻辑隔离）
        modelBuilder.ApplyConfiguration(new HerbConfiguration());
        modelBuilder.ApplyConfiguration(new PrescriptionItemConfiguration());
        modelBuilder.ApplyConfiguration(new PrescriptionConfiguration());
        modelBuilder.ApplyConfiguration(new MedicalCaseConfiguration());
        modelBuilder.ApplyConfiguration(new PatientConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new FormulaConfiguration());
        modelBuilder.ApplyConfiguration(new FormulaHerbItemConfiguration());

        // 软删除全局查询过滤器（统一走 Infrastructure 扩展，与 AppDbContext ApplyOptimizations 保持一致）
        modelBuilder
            .ApplySoftDeleteFilters<Herb>()
            .ApplySoftDeleteFilters<Prescription>()
            .ApplySoftDeleteFilters<MedicalCase>()
            .ApplySoftDeleteFilters<Patient>()
            .ApplySoftDeleteFilters<Formula>();

        // FormulaHerbItem 为弱实体（无 IsDeleted），过滤基于关联 Formula.IsDeleted
        modelBuilder.Entity<FormulaHerbItem>().HasQueryFilter(fh => fh.Formula == null || !fh.Formula.IsDeleted);
    }
}
