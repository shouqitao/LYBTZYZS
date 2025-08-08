using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Logging;
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

        // 患者管理
        public DbSet<PatientModel> Patients { get; set; }

        // 医生管理
        // public DbSet<DoctorModel> Doctors { get; set; } // 医生功能已整合到Users

        // 挂号管理
        // public DbSet<RegistrationModel> Registrations { get; set; } // 模块已删除

        // 排队管理
        // public DbSet<QueueingModel> Queueings { get; set; } // 模块已删除

        // 医疗案例
        public DbSet<MedicalCaseModel> MedicalCases { get; set; }

        // 看诊
        public DbSet<ConsultationModel> Consultations { get; set; }

        // 治疗方案
        // public DbSet<TreatmentPlanModel> TreatmentPlans { get; set; } // 模块已删除

        // 诊断治疗（已删除，使用Consultation替代）
        // public DbSet<DiagnosisTreatmentModel> DiagnosisTreatments { get; set; }

        // 处方管理
        public DbSet<PrescriptionModel> Prescriptions { get; set; }

        public DbSet<PrescriptionItemModel> PrescriptionItems { get; set; }

        // 药材管理
        public DbSet<HerbModel> Herbs { get; set; }

        // 验方管理
        public DbSet<FormulaModel> Formulas { get; set; }

        // 药房管理
        // public DbSet<PharmacyModel> Pharmacies { get; set; } // 模块已删除

        // public DbSet<PharmacyHerbModel> PharmacyHerbs { get; set; } // 模块已删除

        // 收银管理
        // public DbSet<CashierRecord> CashierRecords { get; set; } // 模块已删除
        // public DbSet<CashierItem> CashierItems { get; set; } // 模块已删除
        // public DbSet<CashierPayment> CashierPayments { get; set; } // 模块已删除
        // public DbSet<DailySettlement> DailySettlements { get; set; } // 模块已删除
        // public DbSet<Invoice> Invoices { get; set; } // 模块已删除

        // 病历管理（已删除，使用MedicalCase和Consultation替代）
        // public DbSet<RecordModel> Records { get; set; }

        // 治疗室管理
        // public DbSet<TreatmentRoomModel> TreatmentRooms { get; set; } // 模块已删除
        // public DbSet<TreatmentTaskModel> TreatmentTasks { get; set; } // 模块已删除

        // 同步管理（MVP阶段暂不需要）
        // public DbSet<SyncTaskModel> SyncTasks { get; set; }
        // public DbSet<SyncLogModel> SyncLogs { get; set; }

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
    // public DbSet<LYBT.Models.TreatmentRoom.TreatmentCatalogModel> TreatmentCatalogs { get; set; } // 模块已删除

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置各个实体的映射关系
            ConfigureUsers(modelBuilder);
            ConfigureAdminSecrets(modelBuilder);
            ConfigurePatients(modelBuilder);
            // ConfigureDoctors(modelBuilder); // 功能已整合到Users
            // ConfigureRegistrations(modelBuilder); // 模块已删除
            // ConfigureQueueings(modelBuilder); // 模块已删除
            ConfigureMedicalCases(modelBuilder);
            ConfigureConsultations(modelBuilder);
            // ConfigureDiagnosisTreatments(modelBuilder); // 已删除，使用Consultation替代
            ConfigurePrescriptions(modelBuilder);
            ConfigureHerbs(modelBuilder);
            ConfigureFormulas(modelBuilder);
            // ConfigurePharmacies(modelBuilder); // 模块已删除
            // ConfigurePharmacyHerbs(modelBuilder); // 模块已删除
            // ConfigureCashiers(modelBuilder); // 模块已删除
            // ConfigureRecords(modelBuilder); // 已删除，使用MedicalCase替代
            // ConfigureTreatmentTasks(modelBuilder); // 模块已删除
            // ConfigureSyncs(modelBuilder); // MVP阶段暂不需要
            ConfigureLogModels(modelBuilder);
            ConfigureConfigurationModels(modelBuilder);
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
        private static void ConfigureDoctors(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<DoctorModel>();
            entity.ToTable("Doctors");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Specialty).HasMaxLength(100);
            entity.Property(d => d.LicenseNumber).HasMaxLength(32);
            entity.Property(d => d.CreateTime).HasColumnName("CreatedTime");
        }
        */


        // 挂号模块已删除
        /*        private static void ConfigureRegistrations(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<RegistrationModel>();
            entity.ToTable("Registrations");
            entity.HasKey(r => r.Id);
        }*/

        // 排队模块已删除
        /*        private static void ConfigureQueueings(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<QueueingModel>();
            entity.ToTable("Queueings");
            entity.HasKey(q => q.Id);
        }*/

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

        // DiagnosisTreatments已删除，使用Consultation替代

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
            cashierEntity.ToTable("CashierRecords");
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


        // Records已删除，使用MedicalCase和Consultation替代

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
    }
}