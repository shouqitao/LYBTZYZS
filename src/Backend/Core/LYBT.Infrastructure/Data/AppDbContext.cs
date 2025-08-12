using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Security;
using LYBT.Models.Auth;
using LYBT.Models.Consultation;
using LYBT.Models.Formula;
using LYBT.Models.Herbs;
using LYBT.Models.MedicalCase;
using LYBT.Models.Patients;
using LYBT.Models.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

// DDD聚合根引用
using LYBT.Domain.Aggregates.PatientAggregate;
using LYBT.Domain.Aggregates.ConsultationAggregate;
using LYBT.Domain.Aggregates.HerbAggregate;
using LYBT.Domain.Aggregates.FormulaAggregate;
using LYBT.Domain.Aggregates.MedicalCaseAggregate;
using LYBT.Domain.Aggregates.PrescriptionAggregate;
using LYBT.Domain.Aggregates.UserAggregate;

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

        // ==================== DDD聚合根实体 ====================
        // 注意：这些是新的DDD聚合根，与上面的Model类并存但使用不同表名

        /// <summary>
        /// DDD聚合根 - 用户
        /// </summary>
        public DbSet<User> DomainUsers { get; set; }

        /// <summary>
        /// DDD聚合根 - 患者
        /// </summary>
        public DbSet<Patient> DomainPatients { get; set; }

        /// <summary>
        /// DDD聚合根 - 看诊
        /// </summary>
        public DbSet<Consultation> DomainConsultations { get; set; }

        /// <summary>
        /// DDD聚合根 - 中药材
        /// </summary>
        public DbSet<Herb> DomainHerbs { get; set; }

        /// <summary>
        /// DDD聚合根 - 验方
        /// </summary>
        public DbSet<Formula> DomainFormulas { get; set; }

        /// <summary>
        /// DDD聚合根 - 病案
        /// </summary>
        public DbSet<MedicalCase> DomainMedicalCases { get; set; }

        /// <summary>
        /// DDD聚合根 - 处方
        /// </summary>
        public DbSet<Prescription> DomainPrescriptions { get; set; }

        // ==================== 日志相关实体 ====================

        /// <summary>
        /// 统一日志
        /// </summary>
        public DbSet<LogModel> Logs { get; set; }

        /// <summary>
        /// 系统日志
        /// </summary>
        public DbSet<SystemLogModel> SystemLogs { get; set; }

        /// <summary>
        /// 用户操作日志
        /// </summary>
        public DbSet<UserActionLogModel> UserActionLogs { get; set; }

        /// <summary>
        /// 错误日志
        /// </summary>
        public DbSet<ErrorLogModel> ErrorLogs { get; set; }

        /// <summary>
        /// 审计日志
        /// </summary>
        public DbSet<AuditLogModel> AuditLogs { get; set; }

        /// <summary>
        /// 性能日志
        /// </summary>
        public DbSet<PerformanceLogModel> PerformanceLogs { get; set; }

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
            
            // ==================== DDD聚合根配置 ====================
            ConfigureDomainUsers(modelBuilder);
            ConfigureDomainPatients(modelBuilder);
            ConfigureDomainConsultations(modelBuilder);
            ConfigureDomainHerbs(modelBuilder);
            ConfigureDomainFormulas(modelBuilder);
            ConfigureDomainMedicalCases(modelBuilder);
            ConfigureDomainPrescriptions(modelBuilder);
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
        /// 配置日志相关实体
        /// </summary>
        private static void ConfigureLogModels(ModelBuilder modelBuilder)
        {
            // 统一日志配置
            modelBuilder.Entity<LogModel>(entity =>
            {
                entity.ToTable("InfrastructureLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LogType).HasConversion<string>();
                entity.Property(e => e.ObjectType).HasConversion<string>();
                entity.Property(e => e.ActionType).HasConversion<string>();
                entity.Property(e => e.OperatorName).HasMaxLength(50);
                entity.Property(e => e.Content).HasMaxLength(500);
                entity.Property(e => e.IP).HasMaxLength(45);
                entity.Property(e => e.Remark).HasMaxLength(1000);
                entity.HasIndex(e => e.LogTime);
                entity.HasIndex(e => e.OperatorId);
                entity.HasIndex(e => e.LogType);
            });

            // 系统日志配置
            modelBuilder.Entity<SystemLogModel>(entity =>
            {
                entity.ToTable("SystemLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Level).HasConversion<string>();
                entity.Property(e => e.Source).HasMaxLength(100);
                entity.Property(e => e.Message).HasMaxLength(2000);
                entity.Property(e => e.ServerInfo).HasMaxLength(100);
                entity.Property(e => e.RequestId).HasMaxLength(50);
                entity.HasIndex(e => e.LogTime);
                entity.HasIndex(e => e.Level);
                entity.HasIndex(e => e.Source);
            });

            // 用户操作日志配置
            modelBuilder.Entity<UserActionLogModel>(entity =>
            {
                entity.ToTable("UserActionLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ActionType).HasConversion<string>();
                entity.Property(e => e.UserName).HasMaxLength(50);
                entity.Property(e => e.Module).HasMaxLength(50);
                entity.Property(e => e.Function).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.RequestPath).HasMaxLength(500);
                entity.Property(e => e.HttpMethod).HasMaxLength(10);
                entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
                entity.Property(e => e.ClientIP).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.HasIndex(e => e.ActionTime);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ActionType);
            });

            // 错误日志配置
            modelBuilder.Entity<ErrorLogModel>(entity =>
            {
                entity.ToTable("ErrorLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
                entity.Property(e => e.ExceptionType).HasMaxLength(200);
                entity.Property(e => e.RequestPath).HasMaxLength(500);
                entity.Property(e => e.HttpMethod).HasMaxLength(10);
                entity.Property(e => e.ClientIP).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.Environment).HasMaxLength(50);
                entity.Property(e => e.Severity).HasMaxLength(20);
                entity.Property(e => e.ResolutionNotes).HasMaxLength(1000);
                entity.HasIndex(e => e.OccurredAt);
                entity.HasIndex(e => e.IsResolved);
                entity.HasIndex(e => e.Severity);
            });

            // 审计日志配置
            modelBuilder.Entity<AuditLogModel>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).HasMaxLength(50);
                entity.Property(e => e.ResourceType).HasMaxLength(50);
                entity.Property(e => e.ResourceId).HasMaxLength(50);
                entity.Property(e => e.UserName).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ClientIP).HasMaxLength(45);
                entity.Property(e => e.SessionId).HasMaxLength(50);
                entity.Property(e => e.RequestId).HasMaxLength(50);
                entity.Property(e => e.Result).HasMaxLength(50);
                entity.Property(e => e.RiskLevel).HasMaxLength(20);
                entity.Property(e => e.ComplianceFlags).HasMaxLength(200);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.ResourceType);
            });

            // 性能日志配置
            modelBuilder.Entity<PerformanceLogModel>(entity =>
            {
                entity.ToTable("PerformanceLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OperationName).HasMaxLength(100);
                entity.Property(e => e.ModuleName).HasMaxLength(50);
                entity.Property(e => e.MethodName).HasMaxLength(100);
                entity.Property(e => e.ClientIP).HasMaxLength(45);
                entity.Property(e => e.RequestPath).HasMaxLength(500);
                entity.Property(e => e.PerformanceLevel).HasMaxLength(20);
                entity.Property(e => e.AdditionalData).HasColumnType("text");
                entity.HasIndex(e => e.StartTime);
                entity.HasIndex(e => e.Duration);
                entity.HasIndex(e => e.PerformanceLevel);
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

        // ==================== DDD聚合根配置方法 ====================

        /// <summary>
        /// 配置DDD用户聚合根
        /// </summary>
        private static void ConfigureDomainUsers(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<User>();
            entity.ToTable("DomainUsers");
            entity.HasKey(u => u.Id);
            
            // 基础值对象配置 - 映射为owned entities
            entity.OwnsOne(u => u.UserName, userName =>
            {
                userName.Property(un => un.Value)
                    .HasColumnName("UserName")
                    .HasMaxLength(50)
                    .IsRequired();
            });
            
            entity.OwnsOne(u => u.RealName, realName =>
            {
                realName.Property(rn => rn.Value)
                    .HasColumnName("RealName")
                    .HasMaxLength(100)
                    .IsRequired();
            });
            
            entity.OwnsOne(u => u.Email, email =>
            {
                email.Property(e => e.Value)
                    .HasColumnName("Email")
                    .HasMaxLength(100)
                    .IsRequired();
            });
            
            entity.OwnsOne(u => u.PhoneNumber, phoneNumber =>
            {
                phoneNumber.Property(pn => pn.Value)
                    .HasColumnName("PhoneNumber")
                    .HasMaxLength(20);
            });
            
            // 用户角色值对象配置
            entity.OwnsOne(u => u.Role, role =>
            {
                role.Property(r => r.Value)
                    .HasColumnName("Role")
                    .IsRequired();
                    
                role.Property(r => r.Name)
                    .HasColumnName("RoleName")
                    .HasMaxLength(50)
                    .IsRequired();
            });
            
            // 直接属性配置
            entity.Property(u => u.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();
                
            entity.Property(u => u.IsActive)
                .IsRequired();
                
            entity.Property(u => u.LastLoginAt);
            
            entity.Property(u => u.FailedLoginAttempts)
                .HasDefaultValue(0);
                
            entity.Property(u => u.LockedUntil);
            
            // 继承自AggregateRoot的属性
            entity.Property(u => u.CreatedAt)
                .IsRequired();
                
            entity.Property(u => u.CreatedBy);
            
            entity.Property(u => u.UpdatedAt);
            
            entity.Property(u => u.UpdatedBy);
            
            // 索引配置
            entity.HasIndex("UserName_Value")
                .IsUnique()
                .HasDatabaseName("IX_DomainUsers_UserName");
                
            entity.HasIndex("Email_Value")
                .HasDatabaseName("IX_DomainUsers_Email");
                
            entity.HasIndex("PhoneNumber_Value")
                .HasDatabaseName("IX_DomainUsers_PhoneNumber");
                
            entity.HasIndex(u => u.IsActive)
                .HasDatabaseName("IX_DomainUsers_IsActive");
                
            entity.HasIndex(u => u.CreatedAt)
                .HasDatabaseName("IX_DomainUsers_CreatedAt");
        }

        /// <summary>
        /// 配置DDD患者聚合根
        /// </summary>
        private static void ConfigureDomainPatients(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Patient>();
            entity.ToTable("DomainPatients");
            entity.HasKey(p => p.Id);
            
            // 基本属性配置
            entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
            entity.Property(p => p.PhoneNumber).HasMaxLength(20);
            entity.Property(p => p.Address).HasMaxLength(256);
            
            // 身份信息值对象配置
            entity.OwnsOne(p => p.Identity, identity =>
            {
                identity.Property(i => i.IdType).HasMaxLength(20);
                identity.Property(i => i.IdNumber).HasMaxLength(50);
                identity.Property(i => i.Gender).HasConversion<string>();
                identity.Property(i => i.DateOfBirth).IsRequired(false);
            });
            
            // 联系信息值对象配置
            entity.OwnsOne(p => p.ContactInfo, contact =>
            {
                contact.Property(c => c.EmergencyContact).HasMaxLength(100);
                contact.Property(c => c.EmergencyPhone).HasMaxLength(20);
                contact.Property(c => c.Email).HasMaxLength(100);
                contact.Property(c => c.Relationship).HasMaxLength(50);
            });
            
            // 过敏史值对象配置
            entity.OwnsOne(p => p.AllergyHistory, allergy =>
            {
                allergy.Property(a => a.AllergyDescription).HasMaxLength(500);
                allergy.Property(a => a.LastUpdated);
            });
            
            // 个人信息值对象配置
            entity.OwnsOne(p => p.PersonalInfo, personal =>
            {
                personal.Property(pi => pi.PinYinCode).HasMaxLength(20);
                personal.Property(pi => pi.WuBiCode).HasMaxLength(20);
                personal.Property(pi => pi.Remark).HasMaxLength(500);
            });
            
            // 索引配置
            entity.HasIndex(p => p.Name);
            entity.HasIndex(p => p.PhoneNumber);
            entity.HasIndex("Identity_IdNumber");
        }

        /// <summary>
        /// 配置DDD看诊聚合根
        /// </summary>
        private static void ConfigureDomainConsultations(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Consultation>();
            entity.ToTable("DomainConsultations");
            entity.HasKey(c => c.Id);
            
            // 基本属性配置
            entity.Property(c => c.PatientId).IsRequired();
            entity.Property(c => c.DoctorId).IsRequired();
            entity.Property(c => c.ConsultationTime).IsRequired();
            entity.Property(c => c.DoctorName).HasMaxLength(50);
            entity.Property(c => c.ConsultationNo).HasMaxLength(50);
            entity.Property(c => c.MedicalAdvice).HasMaxLength(1000);
            
            // 望诊信息值对象配置
            entity.OwnsOne(c => c.Inspection, inspection =>
            {
                inspection.Property(i => i.Observations).HasMaxLength(500);
                // 枚举类型会自动映射为整数
            });
            
            // 闻诊信息值对象配置
            entity.OwnsOne(c => c.AuscultationOlfaction, ao =>
            {
                ao.Property(a => a.Observations).HasMaxLength(500);
                // 枚举类型会自动映射为整数
            });
            
            // 问诊信息值对象配置
            entity.OwnsOne(c => c.Inquiry, inquiry =>
            {
                inquiry.Property(i => i.ChiefComplaint).HasMaxLength(500);
                inquiry.Property(i => i.PresentIllness).HasMaxLength(1000);
                inquiry.Property(i => i.PastHistory).HasMaxLength(500);
                inquiry.Property(i => i.Menstruation).HasMaxLength(200);
                inquiry.Property(i => i.Observations).HasMaxLength(500);
                // 枚举类型会自动映射为整数
            });
            
            // 切诊信息值对象配置
            entity.OwnsOne(c => c.Palpation, palpation =>
            {
                palpation.Property(p => p.PulseDetails).HasMaxLength(300);
                palpation.Property(p => p.AbdominalPalpation).HasMaxLength(300);
                palpation.Property(p => p.MeridianPalpation).HasMaxLength(300);
                palpation.Property(p => p.Observations).HasMaxLength(500);
                // 枚举类型会自动映射为整数
            });
            
            // 索引配置
            entity.HasIndex(c => c.PatientId);
            entity.HasIndex(c => c.DoctorId);
            entity.HasIndex(c => c.ConsultationTime);
        }

        /// <summary>
        /// 配置DDD中药材聚合根
        /// </summary>
        private static void ConfigureDomainHerbs(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Herb>();
            entity.ToTable("DomainHerbs");
            entity.HasKey(h => h.Id);
            
            // 基本属性配置
            entity.Property(h => h.Name).HasMaxLength(100).IsRequired();
            entity.Property(h => h.LatinName).HasMaxLength(200);
            entity.Property(h => h.CommonName).HasMaxLength(100);
            entity.Property(h => h.Code).HasMaxLength(50);
            entity.Property(h => h.Pinyin).HasMaxLength(100);
            entity.Property(h => h.EnglishName).HasMaxLength(200);
            entity.Property(h => h.Origin).HasMaxLength(100);
            entity.Property(h => h.Unit).HasMaxLength(10);
            entity.Property(h => h.Notes).HasMaxLength(1000);
            
            // 基础信息值对象配置
            entity.OwnsOne(h => h.BasicInfo, basicInfo =>
            {
                basicInfo.Property(bi => bi.PinYinCode).HasMaxLength(20);
                basicInfo.Property(bi => bi.WuBiCode).HasMaxLength(20);
                basicInfo.Property(bi => bi.CategoryName).HasMaxLength(50);
            });
            
            // 药性信息值对象配置
            entity.OwnsOne(h => h.Properties, properties =>
            {
                properties.Property(p => p.Meridians).HasMaxLength(100);
                // 枚举类型会自动映射为整数: Nature, Flavor, Toxicity
            });
            
            // 功效信息值对象配置
            entity.OwnsOne(h => h.Efficacy, efficacy =>
            {
                efficacy.Property(e => e.MainEffects).HasMaxLength(500);
                efficacy.Property(e => e.Indications).HasMaxLength(1000);
            });
            
            // 价格信息值对象配置
            entity.OwnsOne(h => h.PriceInfo, priceInfo =>
            {
                priceInfo.Property(pi => pi.UnitPrice).HasColumnType("decimal(18,2)");
                priceInfo.Property(pi => pi.Unit).HasMaxLength(10);
            });
            
            // 索引配置
            entity.HasIndex(h => h.Name);
            entity.HasIndex(h => h.Code);
            entity.HasIndex("BasicInfo_PinYinCode");
        }

        /// <summary>
        /// 配置DDD验方聚合根
        /// </summary>
        private static void ConfigureDomainFormulas(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Formula>();
            entity.ToTable("DomainFormulas");
            entity.HasKey(f => f.Id);
            
            // 基本属性配置
            entity.Property(f => f.Name).HasMaxLength(200).IsRequired();
            entity.Property(f => f.Code).HasMaxLength(50);
            entity.Property(f => f.Pinyin).HasMaxLength(100);
            entity.Property(f => f.Source).HasMaxLength(100);
            entity.Property(f => f.Indication).HasMaxLength(1000);
            entity.Property(f => f.Contraindication).HasMaxLength(500);
            entity.Property(f => f.Preparation).HasMaxLength(500);
            entity.Property(f => f.Usage).HasMaxLength(300);
            entity.Property(f => f.Modification).HasMaxLength(500);
            entity.Property(f => f.Notes).HasMaxLength(1000);
            entity.Property(f => f.CreatorName).HasMaxLength(100);
            
            // 验方信息值对象配置
            entity.OwnsOne(f => f.FormulaInfo, info =>
            {
                info.Property(i => i.ChineseName).HasMaxLength(200);
                info.Property(i => i.EnglishName).HasMaxLength(200);
                info.Property(i => i.PinYinCode).HasMaxLength(50);
                info.Property(i => i.WuBiCode).HasMaxLength(50);
            });
            
            // 功效信息值对象配置
            entity.OwnsOne(f => f.Efficacy, efficacy =>
            {
                efficacy.Property(e => e.MainEffects).HasMaxLength(500);
                efficacy.Property(e => e.Indications).HasMaxLength(1000);
                efficacy.Property(e => e.Mechanism).HasMaxLength(500);
            });
            
            // 审批信息值对象配置
            entity.OwnsOne(f => f.Approval, approval =>
            {
                approval.Property(a => a.ApproverName).HasMaxLength(100);
                approval.Property(a => a.ApprovalComments).HasMaxLength(500);
                // Boolean 和 DateTime? 类型会自动映射
            });
            
            // 药材列表配置 - 使用Owned Entity
            entity.OwnsMany(f => f.Herbs, herb =>
            {
                herb.ToTable("DomainFormulaHerbs");
                herb.WithOwner().HasForeignKey("FormulaId");
                herb.Property(h => h.HerbId).IsRequired();
                herb.Property(h => h.HerbName).HasMaxLength(100).IsRequired();
                herb.Property(h => h.Dosage).HasColumnType("decimal(10,2)");
                herb.Property(h => h.Sequence);
                herb.Property(h => h.Role).HasConversion<string>();
                herb.Property(h => h.ProcessingMethod).HasConversion<string>();
                herb.Property(h => h.SpecialInstructions).HasMaxLength(200);
                herb.HasKey("FormulaId", "HerbId");
            });
            
            // 索引配置
            entity.HasIndex(f => f.Name);
            entity.HasIndex(f => f.Code);
            entity.HasIndex("FormulaInfo_PinYinCode");
        }

        /// <summary>
        /// 配置DDD病案聚合根
        /// </summary>
        private static void ConfigureDomainMedicalCases(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<MedicalCase>();
            entity.ToTable("DomainMedicalCases");
            entity.HasKey(mc => mc.Id);
            
            // 基本属性配置
            entity.Property(mc => mc.CaseNo).HasMaxLength(50).IsRequired();
            entity.Property(mc => mc.PatientId).IsRequired();
            entity.Property(mc => mc.PatientName).HasMaxLength(100);
            entity.Property(mc => mc.PatientAge);
            entity.Property(mc => mc.PatientGender).HasConversion<string>();
            entity.Property(mc => mc.DoctorId).IsRequired();
            entity.Property(mc => mc.DoctorName).HasMaxLength(100);
            entity.Property(mc => mc.AdmissionDate);
            entity.Property(mc => mc.DischargeDate);
            entity.Property(mc => mc.Status).HasConversion<string>();
            entity.Property(mc => mc.Type).HasConversion<string>();
            entity.Property(mc => mc.Notes).HasMaxLength(1000);
            entity.Property(mc => mc.IsEmergency);
            entity.Property(mc => mc.Prognosis).HasMaxLength(500);
            entity.Property(mc => mc.ReferredFromDoctorId);
            
            // 病案信息值对象配置
            entity.OwnsOne(mc => mc.CaseInfo, caseInfo =>
            {
                caseInfo.Property(ci => ci.CaseNumber).HasMaxLength(50);
                caseInfo.Property(ci => ci.CaseType).HasConversion<string>();
                caseInfo.Property(ci => ci.IsEmergency);
            });
            
            // 主诉值对象配置
            entity.OwnsOne(mc => mc.ChiefComplaint, cc =>
            {
                cc.Property(x => x.Description).HasMaxLength(500);
                cc.Property(x => x.Duration).HasMaxLength(100);
                cc.Property(x => x.Severity).HasMaxLength(50);
            });
            
            // 现病史值对象配置
            entity.OwnsOne(mc => mc.PresentIllness, pi =>
            {
                pi.Property(x => x.Onset).HasMaxLength(200);
                pi.Property(x => x.Development).HasMaxLength(500);
                pi.Property(x => x.CurrentStatus).HasMaxLength(300);
                pi.Property(x => x.TreatmentHistory).HasMaxLength(500);
                pi.Property(x => x.Response).HasMaxLength(300);
            });
            
            // 既往史值对象配置
            entity.OwnsOne(mc => mc.PastHistory, ph =>
            {
                ph.Property(x => x.Diseases).HasMaxLength(500);
                ph.Property(x => x.Surgeries).HasMaxLength(300);
                ph.Property(x => x.Allergies).HasMaxLength(200);
                ph.Property(x => x.Medications).HasMaxLength(300);
                ph.Property(x => x.Immunizations).HasMaxLength(200);
            });
            
            // 转诊信息配置 - 直接属性而非值对象
            entity.Property(mc => mc.IsReferral);
            entity.Property(mc => mc.ReferralReason).HasMaxLength(300);
            
            // 治疗结果值对象配置
            entity.OwnsOne(mc => mc.Outcome, outcome =>
            {
                outcome.Property(o => o.Effect).HasConversion<string>();
                outcome.Property(o => o.Symptoms).HasMaxLength(500);
                outcome.Property(o => o.Signs).HasMaxLength(300);
                outcome.Property(o => o.LabResults).HasMaxLength(500);
                outcome.Property(o => o.Complications).HasMaxLength(300);
                outcome.Property(o => o.Prognosis).HasMaxLength(300);
            });
            
            // 随访计划值对象配置
            entity.OwnsOne(mc => mc.FollowUpPlan, fup =>
            {
                fup.Property(f => f.NextFollowUpDate);
                fup.Property(f => f.FollowUpMethod).HasMaxLength(50);
                fup.Property(f => f.Notes).HasMaxLength(500);
            });
            
            // 检查记录配置 - 使用Owned Entity
            entity.OwnsMany(mc => mc.Examinations, exam =>
            {
                exam.ToTable("DomainMedicalCaseExaminations");
                exam.WithOwner().HasForeignKey("MedicalCaseId");
                exam.Property(e => e.ExaminationType).HasMaxLength(50);
                exam.Property(e => e.ExaminationItem).HasMaxLength(100);
                exam.Property(e => e.ExaminationDate);
                exam.Property(e => e.Result).HasMaxLength(1000);
                exam.Property(e => e.Conclusion).HasMaxLength(500);
                exam.HasKey("MedicalCaseId", "Id");
            });
            
            // 治疗记录配置 - 使用Owned Entity
            entity.OwnsMany(mc => mc.Treatments, treatment =>
            {
                treatment.ToTable("DomainMedicalCaseTreatments");
                treatment.WithOwner().HasForeignKey("MedicalCaseId");
                treatment.Property(t => t.TreatmentType).HasMaxLength(50);
                treatment.Property(t => t.TreatmentMethod).HasMaxLength(100);
                treatment.Property(t => t.TreatmentDate);
                treatment.Property(t => t.TreatmentDetails).HasMaxLength(1000);
                treatment.Property(t => t.Effect).HasMaxLength(200);
                treatment.HasKey("MedicalCaseId", "Id");
            });
            
            // 病程记录配置 - 使用Owned Entity
            entity.OwnsMany(mc => mc.ProgressNotes, note =>
            {
                note.ToTable("DomainMedicalCaseProgressNotes");
                note.WithOwner().HasForeignKey("MedicalCaseId");
                note.Property(n => n.RecordDate);
                note.Property(n => n.Symptoms).HasMaxLength(500);
                note.Property(n => n.Signs).HasMaxLength(300);
                note.Property(n => n.Assessment).HasMaxLength(500);
                note.Property(n => n.Plan).HasMaxLength(500);
                note.Property(n => n.RecordedBy);
                note.Property(n => n.RecorderName).HasMaxLength(100);
                note.HasKey("MedicalCaseId", "Id");
            });
            
            // 随访记录配置 - 使用Owned Entity
            entity.OwnsMany(mc => mc.FollowUps, followUp =>
            {
                followUp.ToTable("DomainMedicalCaseFollowUps");
                followUp.WithOwner().HasForeignKey("MedicalCaseId");
                followUp.Property(f => f.FollowUpDate);
                followUp.Property(f => f.Method).HasMaxLength(50);
                followUp.Property(f => f.Status).HasMaxLength(50);
                followUp.Property(f => f.Symptoms).HasMaxLength(500);
                followUp.Property(f => f.Medication).HasMaxLength(300);
                followUp.Property(f => f.Advice).HasMaxLength(500);
                followUp.Property(f => f.NextFollowUpDate);
                followUp.HasKey("MedicalCaseId", "Id");
            });
            
            // 个人史值对象配置
            entity.OwnsOne(mc => mc.PersonalHistory, ph =>
            {
                ph.Property(x => x.Occupation).HasMaxLength(100);
                ph.Property(x => x.Lifestyle).HasMaxLength(200);
                ph.Property(x => x.DietaryHabits).HasMaxLength(200);
                ph.Property(x => x.SmokingHistory).HasMaxLength(100);
                ph.Property(x => x.DrinkingHistory).HasMaxLength(100);
            });
            
            // 家族史值对象配置
            entity.OwnsOne(mc => mc.FamilyHistory, fh =>
            {
                fh.Property(x => x.Diseases).HasMaxLength(500);
                fh.Property(x => x.GeneticConditions).HasMaxLength(300);
            });
            
            // 费用值对象配置
            entity.OwnsOne(mc => mc.TotalCost, cost =>
            {
                cost.Property(c => c.Amount).HasPrecision(18, 2);
                cost.Property(c => c.Currency).HasMaxLength(10);
            });
            
            // 索引配置
            entity.HasIndex(mc => mc.PatientId);
            entity.HasIndex(mc => mc.DoctorId);
            entity.HasIndex("CaseInfo_CaseNumber");
            entity.HasIndex(mc => mc.CreatedAt);
        }

        /// <summary>
        /// 配置DDD处方聚合根
        /// </summary>
        private static void ConfigureDomainPrescriptions(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Prescription>();
            entity.ToTable("DomainPrescriptions");
            entity.HasKey(p => p.Id);
            
            // 基本属性配置
            entity.Property(p => p.PatientId).IsRequired();
            entity.Property(p => p.DoctorId).IsRequired();
            entity.Property(p => p.ConsultationId).IsRequired();
            entity.Property(p => p.PrescriptionNo).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Days).IsRequired();
            entity.Property(p => p.DosesPerDay).IsRequired();
            entity.Property(p => p.Notes).HasMaxLength(500);
            entity.Property(p => p.PrescribedDate).IsRequired();
            entity.Property(p => p.DispensedDate);
            entity.Property(p => p.CancelledDate);
            entity.Property(p => p.CancellationReason).HasMaxLength(200);
            
            // Type值对象配置
            entity.OwnsOne(p => p.Type, type =>
            {
                type.Property(t => t.Name).HasColumnName("Type").HasMaxLength(50);
            });
            
            // Status值对象配置
            entity.OwnsOne(p => p.Status, status =>
            {
                status.Property(s => s.Name).HasColumnName("Status").HasMaxLength(50);
            });
            
            // Syndrome值对象配置
            entity.OwnsOne(p => p.Syndrome, syndrome =>
            {
                syndrome.Property(s => s.Name).HasColumnName("Syndrome").HasMaxLength(100);
            });
            
            // TreatmentPrinciple值对象配置
            entity.OwnsOne(p => p.TreatmentPrinciple, tp =>
            {
                tp.Property(t => t.Name).HasColumnName("TreatmentPrinciple").HasMaxLength(100);
            });
            
            // Usage属性配置 (字符串类型)
            entity.Property(p => p.Usage).HasMaxLength(500);
            
            // TotalAmount值对象配置
            entity.OwnsOne(p => p.TotalAmount, amount =>
            {
                amount.Property(a => a.Amount).HasColumnName("TotalAmount").HasColumnType("decimal(18,2)");
                amount.Property(a => a.Currency).HasColumnName("Currency").HasMaxLength(10);
            });
            
            // 处方药材配置 - 使用Owned Entity
            entity.OwnsMany(p => p.Items, item =>
            {
                item.ToTable("DomainPrescriptionItems");
                item.WithOwner().HasForeignKey("PrescriptionId");
                item.Property(i => i.HerbId).IsRequired();
                item.Property(i => i.HerbName).HasMaxLength(100).IsRequired();
                item.Property(i => i.SpecialInstructions).HasMaxLength(200);
                item.Property(i => i.Sequence);
                
                // Dosage值对象配置
                item.OwnsOne(i => i.Dosage, dosage =>
                {
                    dosage.Property(d => d.Value).HasColumnName("Dosage").HasColumnType("decimal(10,2)");
                    dosage.Property(d => d.Unit).HasColumnName("Unit").HasMaxLength(10);
                });
                
                // ProcessingMethod值对象配置
                item.OwnsOne(i => i.ProcessingMethod, pm =>
                {
                    pm.Property(p => p.Name).HasColumnName("ProcessingMethod").HasMaxLength(100);
                });
                
                // Role值对象配置
                item.OwnsOne(i => i.Role, role =>
                {
                    role.Property(r => r.Name).HasColumnName("Role").HasMaxLength(50);
                });
                
                item.HasKey("PrescriptionId", "HerbId");
            });
            
            // 索引配置
            entity.HasIndex(p => p.PatientId);
            entity.HasIndex(p => p.DoctorId);
            entity.HasIndex("PrescriptionInfo_PrescriptionNumber");
            entity.HasIndex(p => p.CreatedAt);
        }
    }
}