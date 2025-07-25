using LYBT.Common.Enums.Users;
using LYBT.Models;
using LYBT.Models.Billing;
using LYBT.Models.DiagnosisTreatment;
using LYBT.Models.Doctors;
using LYBT.Models.FormulaTemplates;
using LYBT.Models.Herbs;
using LYBT.Models.Logs;
using LYBT.Models.Patients;
using LYBT.Models.Pharmacy;
using LYBT.Models.Prescriptions;
using LYBT.Models.Queueing;
using LYBT.Models.Records;
using LYBT.Models.Registration;
using LYBT.Models.Settings;
using LYBT.Models.TreatmentRoom;
using LYBT.Module.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace LYBT.Infrastructure {
    /// <summary>
    /// 主数据库上下文，统一管理所有主表和明细字段配置
    /// </summary>
    public class AppDbContext : DbContext {
        // 静态 JSON 序列化选项，避免重复创建
        private static readonly JsonSerializerOptions JsonOptions = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        #region DbSets - 主表定义
        public DbSet<UserModel> Users { get; set; }
        public DbSet<PatientModel> Patients { get; set; }
        public DbSet<DoctorModel> Doctors { get; set; }
        public DbSet<RegistrationModel> Registrations { get; set; }
        public DbSet<QueueingModel> Queueings { get; set; }
        public DbSet<DiagnosisTreatmentModel> DiagnosisTreatments { get; set; }
        public DbSet<HerbModel> Herbs { get; set; }
        public DbSet<FormulaTemplateModel> FormulaTemplates { get; set; }
        public DbSet<LogModel> Logs { get; set; }
        public DbSet<SyncLogModel> SyncLogs { get; set; }
        public DbSet<SyncTaskModel> SyncTasks { get; set; }
        public DbSet<BillingModel> Billings { get; set; }
        public DbSet<PharmacyModel> Pharmacies { get; set; }
        public DbSet<PrescriptionModel> Prescriptions { get; set; }
        public DbSet<PrescriptionItemModel> PrescriptionItems { get; set; }
        public DbSet<RecordModel> Records { get; set; }
        public DbSet<SettingsModel> Settings { get; set; }
        public DbSet<DiagnosisCatalogModel> DiagnosisCatalogs { get; set; }
        public DbSet<TreatmentCatalogModel> TreatmentCatalogs { get; set; }
        public DbSet<GlobalSettingsModel> GlobalSettings { get; set; }
        public DbSet<TreatmentRoomModel> TreatmentRooms { get; set; }
        public DbSet<SpecialPatientDoctor> SpecialPatientDoctors { get; set; }
        public DbSet<AdminSecretModel> AdminSecrets { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            // 分模块配置
            ConfigureUserModule(modelBuilder);
            ConfigurePatientModule(modelBuilder);
            ConfigureDoctorModule(modelBuilder);
            ConfigureBillingModule(modelBuilder);
            ConfigureDiagnosisTreatmentModule(modelBuilder);
            ConfigureRecordModule(modelBuilder);
            ConfigureFormulaModule(modelBuilder);
            ConfigurePrescriptionModule(modelBuilder);
            ConfigureLogModule(modelBuilder);
            ConfigureRegistrationModule(modelBuilder);
            ConfigureQueueingModule(modelBuilder);
            ConfigureHerbModule(modelBuilder);
        }

        #region 模块化配置方法

        /// <summary>
        /// 配置用户模块
        /// </summary>
        private static void ConfigureUserModule(ModelBuilder modelBuilder) {
            var userEntity = modelBuilder.Entity<UserModel>();

            // 索引配置
            userEntity.HasIndex(u => u.UserName)
                .IsUnique()
                .HasDatabaseName("IX_Users_UserName");

            userEntity.HasIndex(u => u.IsActive)
                .HasDatabaseName("IX_Users_IsActive");

            userEntity.HasIndex(u => u.PinyinCode)
                .HasDatabaseName("IX_Users_PinyinCode");

            userEntity.HasIndex(u => u.PhoneNumber)
                .HasDatabaseName("IX_Users_PhoneNumber");

            // UserRole 枚举列表转换
            userEntity.Property(x => x.Roles)
                .HasConversion(
                    roles => string.Join(",", roles.Select(r => (int)r)),
                    value => ParseUserRoles(value))
                .Metadata.SetValueComparer(CreateListComparer<UserRole>());
        }

        /// <summary>
        /// 配置患者模块
        /// </summary>
        private static void ConfigurePatientModule(ModelBuilder modelBuilder) {
            var patientEntity = modelBuilder.Entity<PatientModel>();

            // 索引配置 - 根据业务查询频率优化
            patientEntity.HasIndex(p => p.IDNumber)
                .IsUnique()
                .HasDatabaseName("IX_Patients_IDNumber");

            patientEntity.HasIndex(p => p.PhoneNumber)
                .HasDatabaseName("IX_Patients_PhoneNumber");

            patientEntity.HasIndex(p => p.PinyinCode)
                .HasDatabaseName("IX_Patients_PinyinCode");

            patientEntity.HasIndex(p => new { p.Name, p.Status })
                .HasDatabaseName("IX_Patients_Name_Status");

            patientEntity.HasIndex(p => p.CreatedAt)
                .HasDatabaseName("IX_Patients_CreatedAt");

            patientEntity.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Patients_Status");

            // 特殊患者医生关系配置
            ConfigureSpecialPatientDoctorRelation(modelBuilder);
        }

        /// <summary>
        /// 配置特殊患者医生关系
        /// </summary>
        private static void ConfigureSpecialPatientDoctorRelation(ModelBuilder modelBuilder) {
            var specialRelationEntity = modelBuilder.Entity<SpecialPatientDoctor>();

            specialRelationEntity.HasIndex(s => new { s.PatientId, s.DoctorId })
                .IsUnique()
                .HasDatabaseName("IX_SpecialPatientDoctors_PatientId_DoctorId");

            specialRelationEntity.HasIndex(s => s.DoctorId)
                .HasDatabaseName("IX_SpecialPatientDoctors_DoctorId");
        }

        /// <summary>
        /// 配置医生模块
        /// </summary>
        private static void ConfigureDoctorModule(ModelBuilder modelBuilder) {
            var doctorEntity = modelBuilder.Entity<DoctorModel>();

            // 外键关系
            doctorEntity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 性能优化索引
            doctorEntity.HasIndex(d => d.Status)
                .HasDatabaseName("IX_Doctors_Status");

            doctorEntity.HasIndex(d => d.PinyinCode)
                .HasDatabaseName("IX_Doctors_PinyinCode");

            doctorEntity.HasIndex(d => new { d.Status, d.WorkStatus })
                .HasDatabaseName("IX_Doctors_Status_WorkStatus");
        }

        /// <summary>
        /// 配置计费模块
        /// </summary>
        private static void ConfigureBillingModule(ModelBuilder modelBuilder) {
            var billingEntity = modelBuilder.Entity<BillingModel>();

            // 金额字段精度配置
            billingEntity.Property(x => x.PaidAmount).HasPrecision(18, 2);
            billingEntity.Property(x => x.TotalAmount).HasPrecision(18, 2);

            // 计费项目JSON存储
            billingEntity.Property(x => x.Items)
                .HasJsonConversion<List<BillingItem>>();
        }

        /// <summary>
        /// 配置诊疗模块
        /// </summary>
        private static void ConfigureDiagnosisTreatmentModule(ModelBuilder modelBuilder) {
            var diagnosisEntity = modelBuilder.Entity<DiagnosisTreatmentModel>();

            // JSON 字段配置
            diagnosisEntity.Property(x => x.Treatments)
                .HasJsonConversion<List<TreatmentItemModel>>();

            diagnosisEntity.Property(x => x.Formula)
                .HasJsonConversion<FormulaModel>();
        }

        /// <summary>
        /// 配置病历模块
        /// </summary>
        private static void ConfigureRecordModule(ModelBuilder modelBuilder) {
            var recordEntity = modelBuilder.Entity<RecordModel>();

            // 多个 JSON 字段配置
            recordEntity.Property(x => x.DiagnosisResults)
                .HasJsonConversion<List<string>>();

            recordEntity.Property(x => x.HerbalFormula)
                .HasJsonConversion<List<HerbItemModel>>();

            recordEntity.Property(x => x.TreatmentPlans)
                .HasJsonConversion<List<TreatmentItemModel>>();

            recordEntity.Property(x => x.SharedToDoctorIds)
                .HasJsonConversion<List<string>>();

            // 业务查询优化索引
            recordEntity.HasIndex(r => new { r.PatientId, r.RecordTime })
                .HasDatabaseName("IX_Records_PatientId_RecordTime");

            recordEntity.HasIndex(r => new { r.DoctorId, r.RecordTime })
                .HasDatabaseName("IX_Records_DoctorId_RecordTime");
        }

        /// <summary>
        /// 配置方剂模块
        /// </summary>
        private static void ConfigureFormulaModule(ModelBuilder modelBuilder) {
            var formulaEntity = modelBuilder.Entity<FormulaTemplateModel>();

            formulaEntity.Property(x => x.Herbs)
                .HasJsonConversion<List<HerbItemModel>>();
        }

        /// <summary>
        /// 配置处方模块
        /// </summary>
        private static void ConfigurePrescriptionModule(ModelBuilder modelBuilder) {
            var prescriptionEntity = modelBuilder.Entity<PrescriptionModel>();

            // 外键关系
            modelBuilder.Entity<PrescriptionItemModel>()
                .HasOne<PrescriptionModel>()
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            // 业务查询优化索引
            prescriptionEntity.HasIndex(p => new { p.PatientId, p.CreateTime })
                .HasDatabaseName("IX_Prescriptions_PatientId_CreateTime");

            prescriptionEntity.HasIndex(p => new { p.DoctorId, p.CreateTime })
                .HasDatabaseName("IX_Prescriptions_DoctorId_CreateTime");
        }

        /// <summary>
        /// 配置日志模块
        /// </summary>
        private static void ConfigureLogModule(ModelBuilder modelBuilder) {
            var logEntity = modelBuilder.Entity<LogModel>();

            // 日志查询优化索引
            logEntity.HasIndex(l => new { l.LogTime, l.LogType })
                .HasDatabaseName("IX_Logs_LogTime_LogType");

            logEntity.HasIndex(l => new { l.OperatorId, l.LogTime })
                .HasDatabaseName("IX_Logs_OperatorId_LogTime");
        }

        /// <summary>
        /// 配置挂号模块
        /// </summary>
        private static void ConfigureRegistrationModule(ModelBuilder modelBuilder) {
            var registrationEntity = modelBuilder.Entity<RegistrationModel>();

            // 挂号查询优化索引
            registrationEntity.HasIndex(r => new { r.RegistrationTime, r.Status })
                .HasDatabaseName("IX_Registrations_RegistrationTime_Status");

            registrationEntity.HasIndex(r => new { r.DoctorId, r.RegistrationTime })
                .HasDatabaseName("IX_Registrations_DoctorId_RegistrationTime");
        }

        /// <summary>
        /// 配置排队模块
        /// </summary>
        private static void ConfigureQueueingModule(ModelBuilder modelBuilder) {
            var queueingEntity = modelBuilder.Entity<QueueingModel>();

            // 排队状态查询优化
            queueingEntity.HasIndex(q => new { q.Status, q.QueueTime })
                .HasDatabaseName("IX_Queueings_Status_QueueTime");

            queueingEntity.HasIndex(q => new { q.DoctorId, q.Status })
                .HasDatabaseName("IX_Queueings_DoctorId_Status");
        }

        /// <summary>
        /// 配置药材模块
        /// </summary>
        private static void ConfigureHerbModule(ModelBuilder modelBuilder) {
            var herbEntity = modelBuilder.Entity<HerbModel>();

            // 价格精度
            herbEntity.Property(x => x.Price)
                .HasPrecision(18, 2);

            // 药材查询优化索引
            herbEntity.HasIndex(h => h.Name)
                .HasDatabaseName("IX_Herbs_Name");

            herbEntity.HasIndex(h => h.Pinyin)
                .HasDatabaseName("IX_Herbs_Pinyin");

            // 药材状态索引
            herbEntity.HasIndex(h => h.Status)
                .HasDatabaseName("IX_Herbs_Status");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 解析用户角色字符串
        /// </summary>
        private static List<UserRole> ParseUserRoles(string value) {
            if (string.IsNullOrEmpty(value))
                return new List<UserRole>();

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => int.TryParse(s, out _))
                .Select(s => (UserRole)int.Parse(s))
                .ToList();
        }

        /// <summary>
        /// 创建泛型列表比较器
        /// </summary>
        private static ValueComparer<List<T>> CreateListComparer<T>() {
            return new ValueComparer<List<T>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v?.GetHashCode() ?? 0)),
                c => c?.ToList() ?? new List<T>()
            );
        }

        #endregion
    }

    /// <summary>
    /// EF Core 扩展方法
    /// </summary>
    internal static class PropertyBuilderExtensions {
        /// <summary>
        /// JSON 转换扩展方法，支持空值处理
        /// </summary>
        public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> propertyBuilder)
            where T : class {
            var jsonOptions = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return propertyBuilder
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<T>(v, jsonOptions))
                .Metadata.SetValueComparer(CreateJsonComparer<T>(jsonOptions));
        }

        /// <summary>
        /// 创建 JSON 比较器，支持空值处理
        /// </summary>
        private static ValueComparer<T> CreateJsonComparer<T>(JsonSerializerOptions options)
            where T : class {
            return new ValueComparer<T>(
                (left, right) => DoEquals(left, right, options),
                instance => DoGetHashCode(instance, options),
                instance => DoGetSnapshot(instance, options));
        }

        private static bool DoEquals<T>(T left, T right, JsonSerializerOptions options)
            where T : class {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;

            try {
                var leftJson = JsonSerializer.Serialize(left, options);
                var rightJson = JsonSerializer.Serialize(right, options);
                return leftJson.Equals(rightJson, StringComparison.Ordinal);
            } catch {
                // 序列化失败时回退到引用比较
                return ReferenceEquals(left, right);
            }
        }

        private static int DoGetHashCode<T>(T instance, JsonSerializerOptions options)
            where T : class {
            if (instance == null)
                return 0;

            try {
                return JsonSerializer.Serialize(instance, options).GetHashCode();
            } catch {
                // 序列化失败时回退到对象哈希码
                return instance.GetHashCode();
            }
        }

        private static T DoGetSnapshot<T>(T instance, JsonSerializerOptions options)
            where T : class {
            if (instance == null)
                return null;

            try {
                var serialized = JsonSerializer.Serialize(instance, options);
                return JsonSerializer.Deserialize<T>(serialized, options);
            } catch {
                // 序列化失败时返回原实例
                return instance;
            }
        }
    }
}