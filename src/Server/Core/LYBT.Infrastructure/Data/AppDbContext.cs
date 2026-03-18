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
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Data
{

    /// <summary>
    /// 统一应用数据库上下文 - 整个项目使用单一数据库LYBTDB
    /// 集成审计字段自动化功能
    /// </summary>
    public class AppDbContext : DbContext
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

        // 用户管理
        public DbSet<User> Users { get; set; }

        // Issue #1909: AdminSecrets表已移除，超级管理员已统一到Users表（Role=SuperAdmin）

        // 认证管理
        public DbSet<AuthSession> AuthSessions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<AutoLoginToken> AutoLoginTokens { get; set; } // OpenSpec: refactor-login-authentication

        // 安全审计 - Issue #1869: Token认证安全重构，记录认证相关安全事件
        public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }

        // JWT令牌存储 - UltraThink安全优化 P8-01B (已移除过度设计的令牌实体存储)

        // 患者管理
        public DbSet<Patient> Patients { get; set; }

        // 医疗案例
        public DbSet<MedicalCase> MedicalCases { get; set; }

        // 医案审计日志 - OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        public DbSet<MedicalCaseAuditLog> MedicalCaseAuditLogs { get; set; }

        // 诊断
        public DbSet<Consultation> Consultations { get; set; }

        // 处方管理
        public DbSet<Prescription> Prescriptions { get; set; }

        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }

        // 医案打印日志 - T2-X8-12: 打印日志从 Prescription 层级迁移到 MedicalCase 层级
        public DbSet<MedicalCasePrintLog> MedicalCasePrintLogs { get; set; }

        // 药材管理
        public DbSet<Herb> Herbs { get; set; }

        // 验方管理
        public DbSet<Formula> Formulas { get; set; }

        // 挂号管理
        public DbSet<Registration> Registrations { get; set; }

        // 系统日志
        public DbSet<SystemLog> SystemLogs { get; set; }

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

            // 应用查询优化配置（索引、全局过滤器等）
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
        /// 设置审计字段
        /// </summary>
        private void SetAuditFields()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            var userId = GetCurrentUserId();
            var timestamp = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    // 强制设置 CreatedAt 和 UpdatedAt（统一由 DbContext 负责）
                    entity.CreatedAt = timestamp;
                    entity.UpdatedAt = timestamp;

                    // 只在有用户上下文时设置 CreatedBy
                    if (userId.HasValue)
                    {
                        entity.CreatedBy = userId;
                    }
                }

                if (entry.State == EntityState.Modified)
                {
                    // 强制设置 UpdatedAt
                    entity.UpdatedAt = timestamp;

                    // 只在有用户上下文时设置 UpdatedBy
                    if (userId.HasValue)
                    {
                        entity.UpdatedBy = userId;
                    }
                }
            }
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        private Guid? GetCurrentUserId()
        {
            try
            {
                var userIdClaim = _httpContextAccessor?.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
            }
            catch
            {
                // 在某些情况下（如单元测试、后台服务等），可能无法获取HttpContext
                return null;
            }
        }

        #endregion
    }
}
