using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Security;
using LYBT.Entities.Auth;
using LYBT.Entities.Consultation;
using LYBT.Entities.Formula;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;


namespace LYBT.Infrastructure.Data
{

    /// <summary>
    /// 统一应用数据库上下文 - 整个项目使用单一数据库LYBTDB
    /// </summary>
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // 用户管理
        public DbSet<UserModel> Users { get; set; }

        // 管理员密钥
        public DbSet<AdminSecretModel> AdminSecrets { get; set; }

        // 认证管理
        public DbSet<AuthSessionModel> AuthSessions { get; set; }
        public DbSet<LoginAttemptModel> LoginAttempts { get; set; }
        public DbSet<SecurityLogModel> SecurityLogs { get; set; }

        // 安全审计 - UltraThink重构安全架构
        public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }

        // 患者管理
        public DbSet<PatientModel> Patients { get; set; }

        // 医疗案例
        public DbSet<MedicalCaseModel> MedicalCases { get; set; }

        // 看诊
        public DbSet<ConsultationModel> Consultations { get; set; }

        // 处方管理
        public DbSet<PrescriptionModel> Prescriptions { get; set; }
        public DbSet<PrescriptionItemModel> PrescriptionItems { get; set; }

        // 药材管理
        public DbSet<HerbModel> Herbs { get; set; }

        // 验方管理
        public DbSet<FormulaModel> Formulas { get; set; }


        // ==================== 日志相关实体 - UltraThink重构：简化 ====================

        /// <summary>
        /// 简化日志模型 - 仅保留必要功能
        /// </summary>
        public DbSet<SimpleLog> Logs { get; set; }

        // ==================== 配置相关实体 ====================

        /// <summary>
        /// 全局设置
        /// </summary>
        public DbSet<GlobalSettingsModel> GlobalSettings { get; set; }

        /// <summary>
        /// 系统设置
        /// </summary>
        public DbSet<SettingsModel> Settings { get; set; }

        /// <summary>
        /// 诊断目录
        /// </summary>
        public DbSet<DiagnosisCatalogModel> DiagnosisCatalogs { get; set; }

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
            // ConfigurePharmacies(modelBuilder); // 模块已删除
            // ConfigurePharmacyHerbs(modelBuilder); // 模块已删除
            // ConfigureCashiers(modelBuilder); // 模块已删除
            // ConfigureTreatmentTasks(modelBuilder); // 模块已删除
            // ConfigureSyncs(modelBuilder); // MVP阶段暂不需要
            ConfigureLogModels(modelBuilder);
            ConfigureConfigurationModels(modelBuilder);
            ConfigureSecurityAudit(modelBuilder);
        }

        private static void ConfigureUsers(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<UserModel>();
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            // 明确配置字段映射以解决命名冲突
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(50).HasColumnName("UserName");
            entity.Property(u => u.RealName).HasMaxLength(100);
            entity.Property(u => u.PasswordHash).HasMaxLength(255);
            entity.Property(u => u.CreateTime).HasColumnName("CreatedTime");
            entity.Property(u => u.PinYinCode).HasMaxLength(20);
            entity.Property(u => u.WuBiCode).HasMaxLength(20);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            entity.Property(u => u.Remark).HasMaxLength(500);
            // 配置Status枚举字段
            entity.Property(u => u.Status).HasConversion<int>();
            // 医生专属字段配置
            entity.Property(u => u.Specialty).HasMaxLength(200);
            entity.Property(u => u.RegistrationFee).HasColumnType("decimal(18,2)");
            entity.Property(u => u.LicenseNumber).HasMaxLength(50);
            entity.Property(u => u.Introduction).HasMaxLength(1000);
        }

        private static void ConfigureAdminSecrets(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<AdminSecretModel>();
            entity.ToTable("AdminSecrets");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Username).IsUnique();
            entity.Property(a => a.Username).HasMaxLength(50).IsRequired();
            entity.Property(a => a.PasswordHash).HasMaxLength(500).IsRequired();

            // 添加默认的 sysadmin 种子数据
            // 密码: Admin@123456
            entity.HasData(new AdminSecretModel
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Username = "sysadmin",
                PasswordHash = "AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ=="
            });
        }

        /// <summary>
        /// 配置认证相关实体
        /// </summary>
        private static void ConfigureAuth(ModelBuilder modelBuilder)
        {
            // 认证会话配置
            modelBuilder.Entity<AuthSessionModel>(entity =>
            {
                entity.ToTable("AuthSessions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(32).IsRequired();
                entity.Property(e => e.LoginType).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.ClientIp).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(512);
                entity.Property(e => e.JwtTokenHash).HasMaxLength(256);
                entity.Property(e => e.RevokeReason).HasMaxLength(200);
                entity.Property(e => e.RefreshTokenHash).HasMaxLength(256);
                entity.Property(e => e.ExtendedData).HasMaxLength(1000);
                entity.Property(e => e.ServerInfo).HasMaxLength(100);
                entity.Property(e => e.GeoLocation).HasMaxLength(200);
                entity.Property(e => e.DeviceInfo).HasMaxLength(200);
                entity.Property(e => e.AnomaliesDescription).HasMaxLength(500);
                entity.HasIndex(e => e.Username);
                entity.HasIndex(e => e.LoginTime);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.UserId);
            });

            // 登录尝试配置
            modelBuilder.Entity<LoginAttemptModel>(entity =>
            {
                entity.ToTable("LoginAttempts");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(32).IsRequired();
                entity.Property(e => e.FailureReason).HasMaxLength(200);
                entity.Property(e => e.ClientIp).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(512);
                entity.Property(e => e.LoginType).HasConversion<string>();
                entity.Property(e => e.RiskLevel).HasConversion<string>();
                entity.Property(e => e.Location).HasMaxLength(100);
                entity.Property(e => e.DeviceFingerprint).HasMaxLength(100);
                entity.Property(e => e.ServerInfo).HasMaxLength(100);
                entity.Property(e => e.ProcessingNode).HasMaxLength(50);
                entity.Property(e => e.DetailedError).HasMaxLength(1000);
                entity.Property(e => e.AdditionalData).HasMaxLength(2000);
                entity.Property(e => e.RequestId).HasMaxLength(64);
                entity.Property(e => e.GeoLocationDetails).HasMaxLength(300);
                entity.Property(e => e.ThreatIndicators).HasMaxLength(500);
                entity.Property(e => e.BlockReason).HasMaxLength(200);
                entity.Property(e => e.UserAgentParsed).HasMaxLength(300);
                entity.Property(e => e.ReviewNotes).HasMaxLength(500);
                entity.HasIndex(e => e.Username);
                entity.HasIndex(e => e.AttemptTime);
                entity.HasIndex(e => e.IsSuccess);
                entity.HasIndex(e => e.ClientIp);
                entity.HasIndex(e => e.RiskLevel);
            });

            // 安全日志配置
            modelBuilder.Entity<SecurityLogModel>(entity =>
            {
                entity.ToTable("SecurityLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).HasConversion<string>();
                entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Username).HasMaxLength(32);
                entity.Property(e => e.ClientIp).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(512);
                entity.Property(e => e.Level).HasConversion<string>();
                entity.Property(e => e.AffectedResource).HasMaxLength(200);
                entity.Property(e => e.Result).HasConversion<string>();
                entity.Property(e => e.Details).HasMaxLength(2000);
                entity.Property(e => e.StackTrace).HasMaxLength(4000);
                entity.Property(e => e.RequestData).HasMaxLength(2000);
                entity.Property(e => e.ResponseData).HasMaxLength(2000);
                entity.Property(e => e.HttpMethod).HasMaxLength(10);
                entity.Property(e => e.RequestPath).HasMaxLength(500);
                entity.Property(e => e.RequestId).HasMaxLength(64);
                entity.Property(e => e.ProcessingNotes).HasMaxLength(1000);
                entity.Property(e => e.NotificationMethod).HasMaxLength(50);
                entity.Property(e => e.CategoryTags).HasMaxLength(200);
                entity.Property(e => e.AutoAnalysisResult).HasMaxLength(1000);
                entity.Property(e => e.RemediationSuggestions).HasMaxLength(1000);
                entity.Property(e => e.ComplianceFlags).HasMaxLength(200);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.EventTime);
                entity.HasIndex(e => e.Level);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.RequiresNotification);
                entity.HasIndex(e => e.IsProcessed);
            });
        }

        private static void ConfigurePatients(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<PatientModel>();
            entity.ToTable("Patients");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(100);
            entity.Property(p => p.WuBiCode).HasMaxLength(20);
            entity.Property(p => p.PinYinCode).HasMaxLength(20);
            entity.Property(p => p.CreateTime).HasColumnName("CreatedAt");
            entity.Property(p => p.PhoneNumber).HasMaxLength(20);
            entity.Property(p => p.Address).HasMaxLength(256);
            entity.Property(p => p.IdType).HasMaxLength(20);
            entity.Property(p => p.IdNumber).HasMaxLength(50);
            // Occupation、MaritalStatus、Ethnicity、Education字段已删除
            entity.Property(p => p.AllergyHistory).HasMaxLength(500);
            entity.Property(p => p.DisableReason).HasMaxLength(128);
            // 配置Status枚举字段
            entity.Property(p => p.Status).HasConversion<int>();
        }

        // 医生功能已整合到Users
        /*

        */


        // 挂号模块已删除
        /*        private static void ConfigureRegistrations(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<RegistrationModel>();
            entity.ToTable("Registrations");
            entity.HasKey(r => r.Id);
        }*/

        // 排队模块已删除


        private static void ConfigureMedicalCases(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<MedicalCaseModel>();
            entity.ToTable("MedicalCases");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Status).HasConversion<string>();
            entity.Property(m => m.Remark).HasMaxLength(500);
            entity.HasIndex(m => m.PatientId);
            entity.HasIndex(m => m.UserId);
            entity.HasIndex(m => m.CreateTime);
            entity.HasIndex(m => m.Status);

            // 配置关联关系
            // entity.HasOne(m => m.Registration).WithMany().HasForeignKey(m => m.RegistrationId); // 模块已删除
            entity.HasOne(m => m.Consultation).WithMany().HasForeignKey(m => m.ConsultationId);
        }

        private static void ConfigureConsultations(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ConsultationModel>();
            entity.ToTable("Consultations");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ChiefComplaint).HasMaxLength(500);
            entity.Property(c => c.PresentIllness).HasMaxLength(1000);
            entity.Property(c => c.PastHistory).HasMaxLength(500);
            entity.Property(c => c.AllergyHistory).HasMaxLength(200);
            entity.Property(c => c.PhysicalExamination).HasMaxLength(1000);
            entity.Property(c => c.Inspection).HasMaxLength(500);
            entity.Property(c => c.AuscultationOlfaction).HasMaxLength(500);
            entity.Property(c => c.Inquiry).HasMaxLength(1000);
            entity.Property(c => c.Palpation).HasMaxLength(500);
            entity.Property(c => c.TongueInspection).HasMaxLength(200);
            entity.Property(c => c.PulseCondition).HasMaxLength(200);
            entity.Property(c => c.TCMDiagnosis).HasMaxLength(500);
            entity.Property(c => c.WesternDiagnosis).HasMaxLength(500);
            entity.Property(c => c.Diagnosis).HasMaxLength(500);
            entity.Property(c => c.TreatmentPrinciple).HasMaxLength(200);
            entity.Property(c => c.MedicalAdvice).HasMaxLength(500);
            entity.Property(c => c.Remark).HasMaxLength(1000);
            entity.HasIndex(c => c.MedicalCaseId);
            entity.HasIndex(c => c.PatientId);
            entity.HasIndex(c => c.UserId);
            entity.HasIndex(c => c.ConsultationTime);
        }

        private static void ConfigurePrescriptions(ModelBuilder modelBuilder)
        {
            var prescriptionEntity = modelBuilder.Entity<PrescriptionModel>();
            prescriptionEntity.ToTable("Prescriptions");
            prescriptionEntity.HasKey(p => p.Id);

            var itemEntity = modelBuilder.Entity<PrescriptionItemModel>();
            itemEntity.ToTable("PrescriptionItems");
            itemEntity.HasKey(i => i.Id);
        }

        private static void ConfigureHerbs(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<HerbModel>();
            entity.ToTable("Herbs");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Name).HasMaxLength(100);
            entity.Property(h => h.PinYinCode).HasMaxLength(20);
            entity.Property(h => h.Origin).HasMaxLength(50);
            entity.Property(h => h.Spec).HasMaxLength(50);
            entity.Property(h => h.Unit).HasMaxLength(10);
            entity.Property(h => h.Effect).HasMaxLength(256);
            entity.Property(h => h.Usage).HasMaxLength(256);
            entity.Property(h => h.Price).HasColumnType("decimal(18,2)");
            // 配置Status枚举字段
            entity.Property(h => h.Status).HasConversion<int>();
            entity.HasIndex(h => h.Name);
            entity.HasIndex(h => h.PinYinCode);
        }

        private static void ConfigureFormulas(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<FormulaModel>();
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
        /// 配置日志相关实体 - UltraThink重构：极简版
        /// </summary>
        private static void ConfigureLogModels(ModelBuilder modelBuilder)
        {
            // 简化日志配置 - 只保留核心字段
            modelBuilder.Entity<SimpleLog>(entity =>
            {
                entity.ToTable("SimpleLogs");
                entity.HasKey(e => new { e.Time, e.Level }); // 使用复合主键避免Guid
                entity.Property(e => e.Level).HasMaxLength(20);
                entity.Property(e => e.Message).HasMaxLength(1000);
                entity.Property(e => e.Exception).HasMaxLength(2000);
                entity.Property(e => e.UserId).HasMaxLength(50);
                entity.HasIndex(e => e.Time);
                entity.HasIndex(e => e.Level);
            });
        }

        /// <summary>
        /// 配置配置相关实体
        /// </summary>
        private static void ConfigureConfigurationModels(ModelBuilder modelBuilder)
        {
            // 全局设置配置
            modelBuilder.Entity<GlobalSettingsModel>(entity =>
            {
                entity.ToTable("GlobalSettings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SystemName).HasMaxLength(100);
                entity.Property(e => e.SystemVersion).HasMaxLength(20);
                entity.Property(e => e.SystemLogo).HasMaxLength(255);
                entity.Property(e => e.DefaultRecordSharing).HasMaxLength(20);
                entity.Property(e => e.SyncMode).HasMaxLength(20);
                entity.Property(e => e.UpdatedByName).HasMaxLength(50);
            });

            // 系统设置配置
            modelBuilder.Entity<SettingsModel>(entity =>
            {
                entity.ToTable("Settings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).HasMaxLength(128).IsRequired();
                entity.Property(e => e.Value).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.ValueType).HasMaxLength(20);
                entity.Property(e => e.Group).HasMaxLength(50);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasIndex(e => e.Key).IsUnique();
                entity.HasIndex(e => e.Group);
                entity.HasIndex(e => e.IsEnabled);
            });

            // 诊断目录配置
            modelBuilder.Entity<DiagnosisCatalogModel>(entity =>
            {
                entity.ToTable("DiagnosisCatalogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(20);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IcdCode).HasMaxLength(20);
                entity.Property(e => e.TcmSyndrome).HasMaxLength(100);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasIndex(e => e.Code);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.ParentId);
                entity.HasIndex(e => e.IsEnabled);
                entity.HasIndex(e => e.IsCommon);

                // 自引用关系
                entity.HasOne<DiagnosisCatalogModel>()
                      .WithMany()
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 治疗目录配置 - 模块已删除
            /*
            modelBuilder.Entity<TreatmentCatalogModel>(entity => {
                entity.ToTable("TreatmentCatalogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Indications).HasMaxLength(500);
                entity.Property(e => e.Contraindications).HasMaxLength(500);
                entity.Property(e => e.Precautions).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(50);
                entity.Property(e => e.UpdatedBy).HasMaxLength(50);
                entity.HasIndex(e => e.Code);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.IsActive);
            });
            */
        }

        /// <summary>
        /// 配置安全审计实体 - UltraThink重构安全审计架构
        /// </summary>
        private static void ConfigureSecurityAudit(ModelBuilder modelBuilder)
        {
            // 安全审计日志配置
            modelBuilder.Entity<SecurityAuditLog>(entity =>
            {
                entity.ToTable("SecurityAuditLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).HasConversion<string>();
                entity.Property(e => e.UserName).HasMaxLength(100);
                entity.Property(e => e.ClientIP).HasMaxLength(45).IsRequired();
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.EventData).HasColumnType("nvarchar(max)");
                entity.Property(e => e.SessionId).HasMaxLength(100);
                entity.Property(e => e.ThreatLevel).HasConversion<string>();
                entity.Property(e => e.GeoLocation).HasMaxLength(200);
                entity.Property(e => e.EventHash).HasMaxLength(64);
                
                // 索引配置
                entity.HasIndex(e => new { e.UserId, e.CreatedAt })
                      .HasDatabaseName("IX_SecurityAuditLogs_UserId_CreatedAt");
                entity.HasIndex(e => new { e.ClientIP, e.CreatedAt })
                      .HasDatabaseName("IX_SecurityAuditLogs_ClientIP_CreatedAt");
                entity.HasIndex(e => new { e.EventType, e.CreatedAt })
                      .HasDatabaseName("IX_SecurityAuditLogs_EventType_CreatedAt");
                entity.HasIndex(e => e.IsSuccess);
                entity.HasIndex(e => e.ThreatLevel);
            });
        }
    }
}
