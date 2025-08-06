using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Logging;
using LYBT.Models.Billing;
using LYBT.Models.DiagnosisTreatment;
using LYBT.Models.Doctors;
using LYBT.Models.FormulaTemplates;
using LYBT.Models.Herbs;
using LYBT.Models.Patients;
using LYBT.Models.Pharmacy;
using LYBT.Models.Prescriptions;
using LYBT.Models.Queueing;
using LYBT.Models.Records;
using LYBT.Models.Registration;
using LYBT.Models.Sync;
using LYBT.Models.TreatmentRoom;
using LYBT.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace LYBT.Infrastructure.Data {

    /// <summary>
    /// 统一应用数据库上下文 - 整个项目使用单一数据库LYBTDB
    /// </summary>
    public class AppDbContext : DbContext {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
        }

        // 用户管理
        public DbSet<UserModel> Users { get; set; }

        // 管理员密钥
        public DbSet<AdminSecretModel> AdminSecrets { get; set; }

        // 患者管理
        public DbSet<PatientModel> Patients { get; set; }

        // 医生管理
        public DbSet<DoctorModel> Doctors { get; set; }

        // 挂号管理
        public DbSet<RegistrationModel> Registrations { get; set; }

        // 排队管理
        public DbSet<QueueingModel> Queueings { get; set; }

        // 诊断治疗
        public DbSet<DiagnosisTreatmentModel> DiagnosisTreatments { get; set; }

        // 处方管理
        public DbSet<PrescriptionModel> Prescriptions { get; set; }

        public DbSet<PrescriptionItemModel> PrescriptionItems { get; set; }

        // 药材管理
        public DbSet<HerbModel> Herbs { get; set; }

        // 经验方模板
        public DbSet<FormulaTemplateModel> FormulaTemplates { get; set; }

        // 药房管理
        public DbSet<PharmacyModel> Pharmacies { get; set; }

        public DbSet<PharmacyHerbModel> PharmacyHerbs { get; set; }

        // 计费管理
        public DbSet<BillingModel> Billings { get; set; }

        public DbSet<BillingItemModel> BillingItems { get; set; }

        // 病历管理
        public DbSet<RecordModel> Records { get; set; }

        // 治疗室管理
        public DbSet<TreatmentRoomModel> TreatmentRooms { get; set; }
        public DbSet<TreatmentTaskModel> TreatmentTasks { get; set; }

        // 同步管理
        public DbSet<SyncTaskModel> SyncTasks { get; set; }

        public DbSet<SyncLogModel> SyncLogs { get; set; }

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
        public DbSet<LYBT.Models.TreatmentRoom.TreatmentCatalogModel> TreatmentCatalogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            // 配置各个实体的映射关系
            ConfigureUsers(modelBuilder);
            ConfigureAdminSecrets(modelBuilder);
            ConfigurePatients(modelBuilder);
            ConfigureDoctors(modelBuilder);
            ConfigureRegistrations(modelBuilder);
            ConfigureQueueings(modelBuilder);
            ConfigureDiagnosisTreatments(modelBuilder);
            ConfigurePrescriptions(modelBuilder);
            ConfigureHerbs(modelBuilder);
            ConfigureFormulaTemplates(modelBuilder);
            ConfigurePharmacies(modelBuilder);
            ConfigurePharmacyHerbs(modelBuilder);
            ConfigureBillings(modelBuilder);
            ConfigureRecords(modelBuilder);
            ConfigureTreatmentRooms(modelBuilder);
            ConfigureTreatmentTasks(modelBuilder);
            ConfigureSyncs(modelBuilder);
            ConfigureLogModels(modelBuilder);
            ConfigureConfigurationModels(modelBuilder);
        }

        private static void ConfigureUsers(ModelBuilder modelBuilder) {
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
            entity.Property(u => u.Email).HasMaxLength(100);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            // 配置业务字段
            entity.Property(u => u.Department).HasMaxLength(100);
            entity.Property(u => u.Position).HasMaxLength(100);
            entity.Property(u => u.Remark).HasMaxLength(500);
        }

        private static void ConfigureAdminSecrets(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<AdminSecretModel>();
            entity.ToTable("AdminSecrets");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Username).IsUnique();
            entity.Property(a => a.Username).HasMaxLength(50).IsRequired();
            entity.Property(a => a.PasswordHash).HasMaxLength(500).IsRequired();

            // 添加默认的 sysadmin 种子数据
            // 密码: Admin@123456
            entity.HasData(new AdminSecretModel {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Username = "sysadmin",
                PasswordHash = "AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ=="
            });
        }

        private static void ConfigurePatients(ModelBuilder modelBuilder) {
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
            entity.Property(p => p.Occupation).HasMaxLength(50);
            entity.Property(p => p.MaritalStatus).HasMaxLength(20);
            entity.Property(p => p.Ethnicity).HasMaxLength(20);
            entity.Property(p => p.Education).HasMaxLength(30);
            entity.Property(p => p.AllergyHistory).HasMaxLength(500);
            entity.Property(p => p.DisableReason).HasMaxLength(128);
            // 忽略IsActive字段，使用Status字段替代
            entity.Ignore(p => p.IsActive);
        }

        private static void ConfigureDoctors(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<DoctorModel>();
            entity.ToTable("Doctors");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Specialty).HasMaxLength(100);
            entity.Property(d => d.LicenseNumber).HasMaxLength(32);
            entity.Property(d => d.CreateTime).HasColumnName("CreatedTime");
        }

        private static void ConfigureRegistrations(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<RegistrationModel>();
            entity.ToTable("Registrations");
            entity.HasKey(r => r.Id);
        }

        private static void ConfigureQueueings(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<QueueingModel>();
            entity.ToTable("Queueings");
            entity.HasKey(q => q.Id);
        }

        private static void ConfigureDiagnosisTreatments(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<DiagnosisTreatmentModel>();
            entity.ToTable("DiagnosisTreatments");
            entity.HasKey(d => d.Id);

            entity.OwnsOne(d => d.Formula, f => {
                f.OwnsMany(x => x.Herbs);
            });

            entity.OwnsMany(d => d.Treatments);
        }

        private static void ConfigurePrescriptions(ModelBuilder modelBuilder) {
            var prescriptionEntity = modelBuilder.Entity<PrescriptionModel>();
            prescriptionEntity.ToTable("Prescriptions");
            prescriptionEntity.HasKey(p => p.Id);

            var itemEntity = modelBuilder.Entity<PrescriptionItemModel>();
            itemEntity.ToTable("PrescriptionItems");
            itemEntity.HasKey(i => i.Id);
        }

        private static void ConfigureHerbs(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<HerbModel>();
            entity.ToTable("Herbs");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Name).HasMaxLength(100);
            entity.Property(h => h.PinYinCode).HasMaxLength(20);
            entity.Property(h => h.WuBiCode).HasMaxLength(20);
            entity.Property(h => h.BatchNo).HasMaxLength(32);
            entity.Property(h => h.CreateTime).HasColumnName("CreatedAt");
            entity.Property(h => h.Origin).HasMaxLength(50);
            entity.Property(h => h.Spec).HasMaxLength(50);
            entity.Property(h => h.Unit).HasMaxLength(10);
            entity.Property(h => h.Effect).HasMaxLength(256);
            entity.Property(h => h.Price).HasColumnType("decimal(18,2)");
            entity.Property(h => h.Specification).HasColumnType("decimal(18,2)");
            entity.HasIndex(h => h.Name);
            entity.HasIndex(h => h.PinYinCode);
            entity.HasIndex(h => h.WuBiCode);
            entity.HasIndex(h => h.ExpireDate);
            // 忽略IsActive字段，使用Status字段替代
            entity.Ignore(h => h.IsActive);
        }

        private static void ConfigureFormulaTemplates(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<FormulaTemplateModel>();
            entity.ToTable("FormulaTemplates");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).HasMaxLength(200);

            // 简化配置，忽略子实体以避免复杂的配置问题
            entity.Ignore(f => f.Herbs);
        }

        private static void ConfigurePharmacies(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<PharmacyModel>();
            entity.ToTable("Pharmacies");
            entity.HasKey(p => p.Id);

            // 配置药房与药材的一对多关系
            entity.HasMany(p => p.Herbs)
                  .WithOne(ph => ph.Pharmacy)
                  .HasForeignKey(ph => ph.PharmacyId);
        }

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

        private static void ConfigureBillings(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<BillingModel>();
            entity.ToTable("Billings");
            entity.HasKey(b => b.Id);
            entity.HasMany(b => b.Items).WithOne().HasForeignKey(i => i.BillingId);

            // 配置 BillingItemModel 实体
            var itemEntity = modelBuilder.Entity<BillingItemModel>();
            itemEntity.ToTable("BillingItems");
            itemEntity.HasKey(i => i.ItemId);
            itemEntity.Property(i => i.ItemName).HasMaxLength(200).IsRequired();
            itemEntity.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            itemEntity.Property(i => i.Quantity).HasColumnType("decimal(18,2)");
        }

        private static void ConfigureRecords(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<RecordModel>();
            entity.ToTable("Records");
            entity.HasKey(r => r.Id);
            var stringListConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new List<string>());

            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
                c => c == null ? new List<string>() : c.ToList());
            var diagnosisProperty = entity.Property(r => r.DiagnosisResults)
                  .HasConversion(stringListConverter)
                  .HasColumnType("nvarchar(max)");
            diagnosisProperty.Metadata.SetValueComparer(stringListComparer);
            var sharedProperty = entity.Property(r => r.SharedToDoctorIds)
                  .HasConversion(stringListConverter)
                  .HasColumnType("nvarchar(max)");
            sharedProperty.Metadata.SetValueComparer(stringListComparer);

            entity.OwnsMany(r => r.HerbalFormula, b => {
                b.ToTable("RecordHerbalFormulas");
                b.WithOwner().HasForeignKey("RecordId");
                b.Property<Guid>("Id");
                b.HasKey("Id");
            });

            entity.OwnsMany(r => r.TreatmentPlans, b => {
                b.ToTable("RecordTreatmentPlans");
                b.WithOwner().HasForeignKey("RecordId");
                b.Property<Guid>("Id");
                b.HasKey("Id");
            });
        }

        private static void ConfigureTreatmentRooms(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<TreatmentRoomModel>();
            entity.ToTable("TreatmentRooms");
            entity.HasKey(t => t.Id);
        }

        private static void ConfigureTreatmentTasks(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<TreatmentTaskModel>();
            entity.ToTable("TreatmentTasks");
            entity.HasKey(t => t.Id);
        }

        private static void ConfigureSyncs(ModelBuilder modelBuilder) {
            // 配置同步任务
            var syncTaskEntity = modelBuilder.Entity<SyncTaskModel>();
            syncTaskEntity.ToTable("SyncTasks");
            syncTaskEntity.HasKey(s => s.Id);

            // 配置同步日志
            var syncLogEntity = modelBuilder.Entity<SyncLogModel>();
            syncLogEntity.ToTable("SyncLogs");
            syncLogEntity.HasKey(s => s.Id);
        }

        /// <summary>
        /// 配置日志相关实体
        /// </summary>
        private static void ConfigureLogModels(ModelBuilder modelBuilder) {
            // 统一日志配置
            modelBuilder.Entity<LogModel>(entity => {
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
            modelBuilder.Entity<SystemLogModel>(entity => {
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
            modelBuilder.Entity<UserActionLogModel>(entity => {
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
            modelBuilder.Entity<ErrorLogModel>(entity => {
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
            modelBuilder.Entity<AuditLogModel>(entity => {
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
            modelBuilder.Entity<PerformanceLogModel>(entity => {
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
        private static void ConfigureConfigurationModels(ModelBuilder modelBuilder) {
            // 全局设置配置
            modelBuilder.Entity<GlobalSettingsModel>(entity => {
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
            modelBuilder.Entity<SettingsModel>(entity => {
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
            modelBuilder.Entity<DiagnosisCatalogModel>(entity => {
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

            // 治疗目录配置
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
        }
    }
}