using System.Security.Claims;
using LYBT.Entities.Auth;
using LYBT.Entities.Common;
using LYBT.Entities.Consultations;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Entities.Registrations;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Data
{

    /// <summary>
    /// 统一应用数据库上下文 - 整个项目使用单一数据库LYBTDB
    /// 集成审计字段自动化功能
    /// P2-3-7 评估：迁移历史 11 个含 SimplifyDataModel/RecreateDroppedAuditTables 中间态，未 Squash 以保 __EFMigrationsHistory 连续性，v2.0 备选 Squash（需全量备份）。
    /// P2-3-1 评估（2026-08-21）：单 Context 20+实体 ChangeTracker 在批量导入 500 条时成本显著；
    /// 评估拆只读 Context 方案：保持单库单 Context（单迁移链 LYBTDB），读路径已用 AsNoTracking/IMemoryCache + OutputCache 缓解；
    /// 拆分为 ReadOnlyAppDbContext（QueryTrackingBehavior.NoTracking）收益 < 双迁移链维护成本，v2.0 备选。
    /// </summary>
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // 用户管理 - 使用 IdentityDbContext<ApplicationUser> 提供的 Users DbSet
        // Issue #1909: AdminSecrets表已移除，超级管理员已统一到Users表（Role=SuperAdmin）

        // 认证管理
        public DbSet<AuthSession> AuthSessions { get; set; }

        // JWT令牌存储 - UltraThink安全优化 P8-01B (已移除过度设计的令牌实体存储)

        // 患者管理
        public DbSet<Patient> Patients { get; set; }

        // 医疗案例
        public DbSet<MedicalCase> MedicalCases { get; set; }

        // 医案打印日志
        public DbSet<MedicalCasePrintLog> MedicalCasePrintLogs { get; set; }

        // 诊断
        public DbSet<Consultation> Consultations { get; set; }

        // 处方管理
        public DbSet<Prescription> Prescriptions { get; set; }

        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }

        // 药材管理
        public DbSet<Herb> Herbs { get; set; }

        // 验方管理
        public DbSet<Formula> Formulas { get; set; }

        // 挂号管理
        public DbSet<Registration> Registrations { get; set; }

        // 安全审计日志
        public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }

        // 系统日志
        public DbSet<SystemLog> SystemLogs { get; set; }

        // 医案审计日志
        public DbSet<MedicalCaseAuditLog> MedicalCaseAuditLogs { get; set; }

        // 配伍管理 - 移除：HerbCompatibilityNote实体已删除

        // ==================== 事务协调器相关实体 ====================
        // UltraThink简化：移除未使用的分布式事务日志实体

        // ==================== 日志相关实体 - UltraThink重构：简化 ====================

        // ==================== 配置相关实体 ====================

        // UltraThink v2.0简化：配置相关实体已移除，使用简化的配置管理

        /// <summary>
        /// 配置实体映射关系
        /// 使用 EF Core Code First 标准方式：ApplyConfigurationsFromAssembly
        /// 所有实体配置已迁移至独立的 IEntityTypeConfiguration 类
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 应用查询优化配置（索引、全局过滤器含软删除）
            modelBuilder.ApplyOptimizations();

            // 自动发现并应用所有 IEntityTypeConfiguration<T> 配置类
            // 符合 Microsoft EF Core 官方最佳实践
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        #region 审计字段自动化

        /// <summary>
        /// 重写SaveChangesAsync以实现审计字段自动填充
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 重写SaveChanges以实现审计字段自动填充
        /// </summary>
        public override int SaveChanges()
        {
            SetAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// 设置审计字段 — 委托给 DbContextAuditExtensions.SetAuditFields（S-5 提取，供模块 DbContext 复用）
        /// </summary>
        private void SetAuditFields()
        {
            // 同时处理 BaseEntity 实体（业务实体）与 ApplicationUser（Identity 实体）
            this.SetAuditFields(GetCurrentUserId());
        }

        /// <summary>
        /// 获取当前用户ID
        /// P1-3：非 HTTP 上下文（如种子/后台清理）归属 System 用户（SecurityOptions.SystemUserId 占位），文档补“系统操作归属 System 用户”
        /// </summary>
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
                // 在某些情况下（如单元测试、后台服务等），可能无法获取HttpContext
            }
            // P1-3：非 HTTP 上下文归属 System 用户，便于审计追溯（种子数据等）
            // 若需可注入 IOptions<SecurityOptions> 读取 SystemUserId，此处先用固定占位（与 SecurityOptions.SystemUserId 默认一致）
            try
            {
                // 尝试从 HttpContext 请求服务解析（若可用）
                var opts = _httpContextAccessor?.HttpContext?.RequestServices.GetService(typeof(Microsoft.Extensions.Options.IOptions<LYBT.Shared.Configuration.Options.Server.SecurityOptions>)) as Microsoft.Extensions.Options.IOptions<LYBT.Shared.Configuration.Options.Server.SecurityOptions>;
                if (opts != null && opts.Value.SystemUserId != Guid.Empty)
                    return opts.Value.SystemUserId;
            }
            catch { }
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }

        #endregion
    }
}


