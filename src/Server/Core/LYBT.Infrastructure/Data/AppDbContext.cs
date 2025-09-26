using LYBT.Entities.Auth;
using LYBT.Entities.Common;
using LYBT.Entities.Consultation;
using LYBT.Entities.Formula;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

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

        // 管理员密钥
        public DbSet<AdminSecretModel> AdminSecrets { get; set; }

        // 认证管理
        public DbSet<AuthSession> AuthSessions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // public DbSet<LoginAttempt> LoginAttempts { get; set; } // UltraThink简化：暂时移除
        // public DbSet<SecurityLog> SecurityLogs { get; set; } // UltraThink简化：暂时移除

        // 安全审计 - UltraThink重构安全架构 (已移除过度设计的SecurityAuditLog)

        // JWT令牌存储 - UltraThink安全优化 P8-01B (已移除过度设计的令牌实体存储)

        // 患者管理
        public DbSet<Patient> Patients { get; set; }

        // 医疗案例
        public DbSet<MedicalCase> MedicalCases { get; set; }

        // 诊断
        public DbSet<Consultation> Consultations { get; set; }

        // 处方管理
        public DbSet<Prescription> Prescriptions { get; set; }

        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }

        // 药材管理
        public DbSet<Herb> Herbs { get; set; }

        // 验方管理
        public DbSet<Formula> Formulas { get; set; }

        // 系统日志
        public DbSet<SystemLog> SystemLogs { get; set; }

        // 配伍管理 - 移除：HerbCompatibilityNote实体已删除

        // ==================== 事务协调器相关实体 ====================
        // UltraThink简化：移除未使用的分布式事务日志实体

        // ==================== 日志相关实体 - UltraThink重构：简化 ====================

        // ==================== 配置相关实体 ====================

        // UltraThink v2.0简化：配置相关实体已移除，使用简化的配置管理

        /// <summary>
        /// 治疗目录
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置各个实体的映射关系
            ConfigureUsers(modelBuilder);
            ConfigureAdminSecrets(modelBuilder);
            ConfigureAuth(modelBuilder);
            ConfigurePatients(modelBuilder);

            // ConfigureRegistrations(modelBuilder); // 模块已删除
            ConfigureMedicalCases(modelBuilder);
            ConfigureConsultations(modelBuilder);
            ConfigurePrescriptions(modelBuilder);
            ConfigureHerbs(modelBuilder);
            ConfigureFormulas(modelBuilder);
            ConfigureSystemLogs(modelBuilder);

            // ConfigureCompatibilityNotes(modelBuilder); // 移除：HerbCompatibilityNote实体已删除

            // UltraThink简化：移除未使用的分布式事务日志配置

            // ConfigurePharmacies(modelBuilder); // 模块已删除
            // ConfigurePharmacyHerbs(modelBuilder); // 模块已删除
            // ConfigureCashiers(modelBuilder); // 模块已删除
            // ConfigureTreatmentTasks(modelBuilder); // 模块已删除
            // ConfigureSyncs(modelBuilder); // MVP阶段暂不需要

            // ConfigureConfigurationModels(modelBuilder); // UltraThink v2.0简化：配置实体已移除
            ConfigureSecurityAudit(modelBuilder);

            // ConfigureTokenStore(modelBuilder); // UltraThink安全优化 P8-01B (已移除过度设计的令牌存储)
        }

        private static void ConfigureUsers(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<User>();
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);

            // 明确配置字段映射以解决命名冲突 - 统一为Username列名
            entity.HasIndex(u => u.UsernName).IsUnique();
            entity.Property(u => u.UsernName).HasMaxLength(50).HasColumnName("Username");
            entity.Property(u => u.RealName).HasMaxLength(50);
            entity.Property(u => u.PasswordHash).HasMaxLength(256);

            // CreateTime字段已删除（UltraThink v2.0简化）
            entity.Property(u => u.PinYinCode).HasMaxLength(50);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);

            // UltraThink v2.0: Remark字段已删除（简化用户管理）
            // 配置枚举字段
            entity.Property(u => u.Status).HasConversion<int>();
            entity.Property(u => u.Role).HasConversion<int>();

            // 配置并发控制字段
            entity.Property(u => u.RowVersion).IsRowVersion().IsConcurrencyToken();
        }

        private static void ConfigureAdminSecrets(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<AdminSecretModel>();
            entity.ToTable("AdminSecrets");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.PasswordHash).HasMaxLength(500).IsRequired();

            // 添加默认的超级管理员种子数据
            // 使用固定ID，密码从配置文件指定的默认密码生成
            entity.HasData(new AdminSecretModel
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                PasswordHash = "AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ=="
            });
        }

        /// <summary>
        /// 配置认证相关实体
        /// </summary>
        private static void ConfigureAuth(ModelBuilder modelBuilder)
        {
            // 认证会话配置
            modelBuilder.Entity<AuthSession>(entity =>
            {
                entity.ToTable("AuthSessions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TokenHash).HasMaxLength(256);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.LoginTime);
                entity.HasIndex(e => e.Status);
            });

            // RefreshToken配置
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Jti).IsRequired().HasMaxLength(128);
                entity.Property(e => e.ClientIp).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.RevokedReason).HasMaxLength(200);
                entity.Property(e => e.ReplacedByToken).HasMaxLength(500);
                entity.Property(e => e.FamilyId).HasMaxLength(128);
                entity.Property(e => e.DeviceId).HasMaxLength(128);
                entity.Property(e => e.DeviceName).HasMaxLength(200);
                
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsRevoked);
                entity.HasIndex(e => new { e.UserId, e.IsRevoked });
                
                // 与用户的关系
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigurePatients(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Patient>();
            entity.ToTable("Patients");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(100);

            entity.Property(p => p.PinYinCode).HasMaxLength(20);

            // CreateTime字段已删除（UltraThink v2.0简化）
            entity.Property(p => p.PhoneNumber).HasMaxLength(20);
            entity.Property(p => p.Address).HasMaxLength(256);
            entity.Property(p => p.IdType).HasMaxLength(20);
            entity.Property(p => p.IdNumber).HasMaxLength(50);

            // Occupation、MaritalStatus、Ethnicity、Education字段已删除
            entity.Property(p => p.AllergyHistory).HasMaxLength(500);

            // 配置Status枚举字段
            entity.Property(p => p.Status).HasConversion<int>();

            // 配置并发控制字段
            entity.Property(p => p.RowVersion).IsRowVersion().IsConcurrencyToken();
        }

        private static void ConfigureMedicalCases(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<MedicalCase>();
            entity.ToTable("MedicalCases");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Status).HasConversion<string>();
            entity.Property(m => m.Remark).HasMaxLength(500);
            entity.HasIndex(m => m.PatientId);
            entity.HasIndex(m => m.DoctorId);
            entity.HasIndex(m => m.Status);
            entity.HasIndex(m => m.CreatedAt);

            // 添加并发控制
            entity.Property(m => m.RowVersion).IsRowVersion().IsConcurrencyToken();

            // 添加审计字段
            entity.Property(m => m.CreatedBy).IsRequired();
            entity.Property(m => m.CreatedAt).IsRequired();

            // 根据文档要求：单患者仅一条未完成病案 - 过滤唯一索引
            // Status枚举值：Active=1, Completed=2, Cancelled=3
            entity.HasIndex(m => m.PatientId)
                  .HasDatabaseName("UX_MedicalCases_Patient_ActiveOnly")
                  .IsUnique()
                  .HasFilter("[Status] = 'Active' OR [Status] = 'Draft'");

            // 删除PrescriptionId外键关系，改为通过Prescription.MedicalCaseId关联
            // 不再需要下面这行
            // entity.HasOne(m => m.Prescription).WithOne().HasForeignKey<MedicalCase>(m => m.PrescriptionId).IsRequired(false);
        }

        private static void ConfigureConsultations(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Consultation>();
            entity.ToTable("Consultations");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ChiefComplaint).HasMaxLength(500);
            entity.Property(c => c.PresentIllness).HasMaxLength(1000);

            entity.Property(c => c.Inspection).HasMaxLength(500);
            entity.Property(c => c.AuscultationOlfaction).HasMaxLength(500);
            entity.Property(c => c.Inquiry).HasMaxLength(1000);
            entity.Property(c => c.Palpation).HasMaxLength(500);

            entity.Property(c => c.TCMDiagnosis).HasMaxLength(500);

            entity.Property(c => c.TreatmentPrinciple).HasMaxLength(500);
            entity.Property(c => c.MedicalAdvice).HasMaxLength(500);
            entity.Property(c => c.Remark).HasMaxLength(1000);
            entity.HasIndex(c => c.PatientId);
            entity.HasIndex(c => c.UserId);

            // 根据文档要求：一病案一诊断 - 唯一索引
            entity.HasIndex(c => c.MedicalCaseId)
                  .HasDatabaseName("UX_Consultations_MedicalCaseId")
                  .IsUnique();

            // 添加并发控制
            entity.Property(c => c.RowVersion).IsRowVersion().IsConcurrencyToken();

            // 添加审计字段
            entity.Property(c => c.CreatedBy).IsRequired();

            // 明确配置外键属性和导航属性关系
            entity.Property(c => c.MedicalCaseId).HasColumnName("MedicalCaseId").IsRequired();

            // 配置与MedicalCase的一对一关系
            entity.HasOne(c => c.MedicalCase)
                  .WithOne(m => m.Consultation)
                  .HasForeignKey<Consultation>(c => c.MedicalCaseId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Cascade); // 级联删除
        }

        private static void ConfigurePrescriptions(ModelBuilder modelBuilder)
        {
            var prescriptionEntity = modelBuilder.Entity<Prescription>();
            prescriptionEntity.ToTable("Prescriptions");
            prescriptionEntity.HasKey(p => p.Id);

            // 根据文档要求：折扣精度为(3,2)，例如0.80表示八折
            prescriptionEntity.Property(p => p.Discount).HasPrecision(3, 2);

            // 根据文档要求：一病案至多一处方 - 唯一索引
            prescriptionEntity.HasIndex(p => p.MedicalCaseId)
                             .HasDatabaseName("UX_Prescriptions_MedicalCaseId")
                             .IsUnique();

            // 添加审计字段
            prescriptionEntity.Property(p => p.CreatedBy).IsRequired();

            // 配置并发控制字段
            prescriptionEntity.Property(p => p.RowVersion).IsRowVersion().IsConcurrencyToken();

            // 配置与MedicalCase的一对一关系
            prescriptionEntity.HasOne<MedicalCase>()
                             .WithOne(m => m.Prescription)
                             .HasForeignKey<Prescription>(p => p.MedicalCaseId)
                             .IsRequired()
                             .OnDelete(DeleteBehavior.Cascade);

            var itemEntity = modelBuilder.Entity<PrescriptionItem>();
            itemEntity.ToTable("PrescriptionItems");
            itemEntity.HasKey(i => i.Id);

            // 根据文档要求：剂量为整数，不需要小数
            // Quantity已改为int类型，不需要HasPrecision配置

            // 单价精度配置
            itemEntity.Property(i => i.UnitPrice).HasPrecision(18, 2);

            // 配置与Prescription的关系
            itemEntity.HasOne<Prescription>()
                     .WithMany(p => p.Items)
                     .HasForeignKey(i => i.PrescriptionId)
                     .IsRequired()
                     .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureHerbs(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Herb>();
            entity.ToTable("Herbs");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Name).HasMaxLength(100);
            entity.Property(h => h.PinYinCode).HasMaxLength(50);
            entity.Property(h => h.Origin).HasMaxLength(100);
            entity.Property(h => h.Spec).HasMaxLength(100);
            entity.Property(h => h.Unit).HasMaxLength(10);
            entity.Property(h => h.Effect).HasMaxLength(500);
            entity.Property(h => h.Usage).HasMaxLength(500);
            
            // P1 Batch1: 统一使用 HasPrecision 配置 decimal 精度
            entity.Property(h => h.Price).HasPrecision(18, 2);
            entity.Property(h => h.CostPrice).HasPrecision(18, 2);

            // 配置Status枚举字段
            entity.Property(h => h.Status).HasConversion<int>();
            entity.HasIndex(h => h.Name);
            entity.HasIndex(h => h.PinYinCode);
        }

        private static void ConfigureFormulas(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Formula>();
            entity.ToTable("Formulas");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).HasMaxLength(200);
            entity.Property(f => f.Effect).HasMaxLength(500);
            entity.Property(f => f.Usage).HasMaxLength(500);
            entity.Property(f => f.Property).HasMaxLength(300);
            entity.Property(f => f.Remark).HasMaxLength(500);

            // 配置Status枚举字段
            entity.Property(f => f.Status).HasConversion<int>();
            entity.Property(f => f.IsShared).HasDefaultValue(false);

            // 简化配置，忽略子实体以避免复杂的配置问题
            entity.Ignore(f => f.Herbs);
        }

        private static void ConfigureSystemLogs(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<SystemLog>();
            entity.ToTable("SystemLogs");
            entity.HasKey(sl => sl.Id);
            
            // 配置字段
            entity.Property(sl => sl.Timestamp).IsRequired();
            entity.Property(sl => sl.Level).HasMaxLength(50).IsRequired();
            entity.Property(sl => sl.Message).IsRequired();
            entity.Property(sl => sl.Exception);
            entity.Property(sl => sl.LoggerName).HasMaxLength(255);
            entity.Property(sl => sl.UserId);
            entity.Property(sl => sl.RequestId).HasMaxLength(36);
            entity.Property(sl => sl.MachineName).HasMaxLength(100);
            entity.Property(sl => sl.ThreadId);
            entity.Property(sl => sl.Properties);
            
            // 添加索引以提高查询性能
            entity.HasIndex(sl => sl.Timestamp).HasDatabaseName("IX_SystemLogs_Timestamp");
            entity.HasIndex(sl => sl.Level).HasDatabaseName("IX_SystemLogs_Level");
            entity.HasIndex(sl => sl.LoggerName).HasDatabaseName("IX_SystemLogs_LoggerName");
            entity.HasIndex(sl => sl.UserId).HasDatabaseName("IX_SystemLogs_UserId");
            entity.HasIndex(sl => new { sl.Timestamp, sl.Level }).HasDatabaseName("IX_SystemLogs_Timestamp_Level");
        }

        // ConfigureCompatibilityNotes方法已删除 - HerbCompatibilityNote实体不再存在

        // UltraThink简化：ConfigureTransactions方法已删除（对应实体已清理）

        // 药房模块已删除
        /*        private static void ConfigurePharmacies(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<PharmacyModel>();
            entity.ToTable("Pharmacies");
            entity.HasKey(p => p.Id);

            // 配置药房与药材的一对多关系
            entity.HasMany(p => p.Herbs)
                  .WithOne(ph => ph.Pharmacy)
                  .HasForeignKey(ph => ph.PharmacyId);
        }*/

        // 药房模块已删除
        /*
        private static void ConfigurePharmacyHerbs(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<PharmacyHerbModel>();
            entity.ToTable("PharmacyHerbs");
            entity.HasKey(ph => new { ph.PharmacyId, ph.HerbId });

            entity.HasOne(ph => ph.Pharmacy)
                  .WithMany(p => p.Herbs)
                  .HasForeignKey(ph => ph.PharmacyId);

            entity.HasOne(ph => ph.Herb)
                  .WithMany()
                  .HasForeignKey(ph => ph.HerbId);
        }
        */

        // 收银模块已删除
        /*
        private static void ConfigureCashiers(ModelBuilder modelBuilder) {
            // CashierRecord 配置
            var cashierEntity = modelBuilder.Entity<CashierRecord>();
            cashierEntity.HasKey(c => c.Id);
            cashierEntity.Property(c => c.TotalAmount).HasColumnType("decimal(18,2)");
            cashierEntity.Property(c => c.PaidAmount).HasColumnType("decimal(18,2)");
            cashierEntity.Property(c => c.RefundAmount).HasColumnType("decimal(18,2)");

            // CashierItem 配置
            var itemEntity = modelBuilder.Entity<CashierItem>();
            itemEntity.ToTable("CashierItems");
            itemEntity.HasKey(ci => ci.Id);
            itemEntity.Property(ci => ci.UnitPrice).HasColumnType("decimal(18,2)");
            itemEntity.Property(ci => ci.Amount).HasColumnType("decimal(18,2)");

            // CashierPayment 配置
            var paymentEntity = modelBuilder.Entity<CashierPayment>();
            paymentEntity.ToTable("CashierPayments");
            paymentEntity.HasKey(cp => cp.Id);
            paymentEntity.Property(cp => cp.Amount).HasColumnType("decimal(18,2)");

            // DailySettlement 配置
            var settlementEntity = modelBuilder.Entity<DailySettlement>();
            settlementEntity.ToTable("DailySettlements");
            settlementEntity.HasKey(ds => ds.Id);
            settlementEntity.Property(ds => ds.TotalAmount).HasColumnType("decimal(18,2)");
            settlementEntity.Property(ds => ds.RefundAmount).HasColumnType("decimal(18,2)");
            settlementEntity.Property(ds => ds.NetAmount).HasColumnType("decimal(18,2)");

            // Invoice 配置
            var invoiceEntity = modelBuilder.Entity<Invoice>();
            invoiceEntity.ToTable("Invoices");
            invoiceEntity.HasKey(i => i.Id);
            invoiceEntity.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
        }
        */

        // 治疗室模块已删除
        /*        private static void ConfigureTreatmentTasks(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<TreatmentTaskModel>();
            entity.ToTable("TreatmentTasks");
            entity.HasKey(t => t.Id);
        }*/

        // Sync模块MVP阶段暂不需要

        /// <summary>
        /// 配置安全审计实体 - UltraThink重构安全审计架构
        /// 注意：SecurityAuditLog已在深度清理中移除，简化为日志记录
        /// </summary>
        private static void ConfigureSecurityAudit(ModelBuilder modelBuilder)
        {
            // SecurityAuditLog实体已被移除，转为简化的日志记录方式
        }

        // ConfigureTokenStore方法已移除 - UltraThink简化架构，移除过度设计的令牌存储实体

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
            var timestamp = DateTime.Now;

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = timestamp;
                    entity.CreatedBy = userId;
                }

                if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = timestamp;
                    entity.UpdatedBy = userId;
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
