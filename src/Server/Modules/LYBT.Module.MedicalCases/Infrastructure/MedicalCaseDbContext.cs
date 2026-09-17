using System.Security.Claims;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Data.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.MedicalCases.Infrastructure;

/// <summary>
/// 医案模块数据库上下文。使用共享连接、同库不同 DbContext 的方式实现模块数据隔离。
/// 物理表由 AppDbContext 单一迁移链管理（ADR-0017 方案 A），本上下文仅做逻辑隔离。
/// </summary>
public class MedicalCaseDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>医案集</summary>
    public DbSet<MedicalCase> MedicalCases { get; set; } = null!;

    /// <summary>诊断集</summary>
    public DbSet<Consultation> Consultations { get; set; } = null!;

    /// <summary>处方集</summary>
    public DbSet<Prescription> Prescriptions { get; set; } = null!;

    /// <summary>处方项集</summary>
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;

    /// <summary>医案打印日志集</summary>
    public DbSet<MedicalCasePrintLog> MedicalCasePrintLogs { get; set; } = null!;

    /// <summary>医案审计日志集</summary>
    public DbSet<MedicalCaseAuditLog> MedicalCaseAuditLogs { get; set; } = null!;

    public MedicalCaseDbContext(DbContextOptions<MedicalCaseDbContext> options) : base(options)
    {
    }

    public MedicalCaseDbContext(
        DbContextOptions<MedicalCaseDbContext> options,
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
        modelBuilder.ApplyConfiguration(new MedicalCaseConfiguration());
        modelBuilder.ApplyConfiguration(new ConsultationConfiguration());
        modelBuilder.ApplyConfiguration(new PrescriptionConfiguration());
        modelBuilder.ApplyConfiguration(new PrescriptionItemConfiguration());
        modelBuilder.ApplyConfiguration(new MedicalCaseAuditLogConfiguration());

        // 跨模块 FK 引用实体：MedicalCaseConfiguration 定义了到 Patient/ApplicationUser 的外键，
        // 需注册正确表名（Patients/Users）以便待看诊列表 Join 与模型一致
        modelBuilder.ApplyConfiguration(new PatientConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());

        // 软删除全局查询过滤器（统一走 Infrastructure 扩展，与 AppDbContext ApplyOptimizations 保持一致）
        modelBuilder
            .ApplySoftDeleteFilters<MedicalCase>()
            .ApplySoftDeleteFilters<Consultation>()
            .ApplySoftDeleteFilters<Prescription>()
            .ApplySoftDeleteFilters<MedicalCasePrintLog>()
            .ApplySoftDeleteFilters<MedicalCaseAuditLog>()
            .ApplySoftDeleteFilters<Patient>();
    }
}
