using LYBT.Models;
using LYBT.Models.Billing;
using LYBT.Models.DiagnosisTreatment;
using LYBT.Models.Doctors;
using LYBT.Models.FormulaTemplates;
using LYBT.Models.Logs;
using LYBT.Models.Pharmacy;
using LYBT.Models.Prescriptions;
using LYBT.Models.Queueing;
using LYBT.Models.Records;
using LYBT.Models.Registration;
using LYBT.Models.Settings;
using LYBT.Models.TreatmentRoom;
using LYBT.Module.Users.Models;
using LYBT.Common.Enums.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using LYBT.Models.Patients;

namespace LYBT.Infrastructure {

    /// <summary>
    /// 主数据库上下文，统一管理所有主表和明细字段配置
    /// </summary>
    public class AppDbContext : DbContext {

        /// <summary>
        /// 执行base操作。
        /// </summary>
        /// <param name="options">参数options</param>
        /// <returns>返回值</returns>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
        }

        // ===== 主表 DbSet，全部要有 =====
        /// <summary>
        /// Users 属性。
        /// </summary>
        public DbSet<UserModel> Users { get; set; }

        /// <summary>
        /// Patients 属性。
        /// </summary>
        public DbSet<PatientModel> Patients { get; set; }
        /// <summary>
        /// Doctors 属性。
        /// </summary>
        public DbSet<DoctorModel> Doctors { get; set; }
        /// <summary>
        /// Registrations 属性。
        /// </summary>
        public DbSet<RegistrationModel> Registrations { get; set; }
        /// <summary>
        /// Queueings 属性。
        /// </summary>
        public DbSet<QueueingModel> Queueings { get; set; }
        /// <summary>
        /// DiagnosisTreatments 属性。
        /// </summary>
        public DbSet<DiagnosisTreatmentModel> DiagnosisTreatments { get; set; }
        /// <summary>
        /// Herbs 属性。
        /// </summary>
        public DbSet<HerbModel> Herbs { get; set; }
        /// <summary>
        /// FormulaTemplates 属性。
        /// </summary>
        public DbSet<FormulaTemplateModel> FormulaTemplates { get; set; }
        /// <summary>
        /// Logs 属性。
        /// </summary>
        public DbSet<LogModel> Logs { get; set; }
        /// <summary>
        /// SyncLogs 属性。
        /// </summary>
        public DbSet<SyncLogModel> SyncLogs { get; set; }
        /// <summary>
        /// SyncTasks 属性。
        /// </summary>
        public DbSet<SyncTaskModel> SyncTasks { get; set; }
        /// <summary>
        /// Billings 属性。
        /// </summary>
        public DbSet<BillingModel> Billings { get; set; }
        /// <summary>
        /// Pharmacies 属性。
        /// </summary>
        public DbSet<PharmacyModel> Pharmacies { get; set; }
        /// <summary>
        /// Prescriptions 属性。
        /// </summary>
        public DbSet<PrescriptionModel> Prescriptions { get; set; }
        /// <summary>
        /// PrescriptionItems 属性。
        /// </summary>
        public DbSet<PrescriptionItemModel> PrescriptionItems { get; set; }
        /// <summary>
        /// Records 属性。
        /// </summary>
        public DbSet<RecordModel> Records { get; set; }
        /// <summary>
        /// Settings 属性。
        /// </summary>
        public DbSet<SettingsModel> Settings { get; set; }
        /// <summary>
        /// DiagnosisCatalogs 属性。
        /// </summary>
        public DbSet<DiagnosisCatalogModel> DiagnosisCatalogs { get; set; }
        /// <summary>
        /// TreatmentCatalogs 属性。
        /// </summary>
        public DbSet<TreatmentCatalogModel> TreatmentCatalogs { get; set; }
        /// <summary>
        /// GlobalSettings 属性。
        /// </summary>
        public DbSet<GlobalSettingsModel> GlobalSettings { get; set; }
        /// <summary>
        /// TreatmentRooms 属性。
        /// </summary>
        public DbSet<TreatmentRoomModel> TreatmentRooms { get; set; }
        /// <summary>
        /// SpecialPatientDoctors 属性。
        /// </summary>
        public DbSet<SpecialPatientDoctor> SpecialPatientDoctors { get; set; }
        /// <summary>
        /// AdminSecrets 属性。
        /// </summary>
        public DbSet<AdminSecretModel> AdminSecrets { get; set; }

        /// <summary>
        /// 配置明细字段Json存储，decimal金额加精度，集合加ValueComparer
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            // UserName唯一索引
            modelBuilder.Entity<UserModel>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            // === List<UserRole> for UserModel ===
            modelBuilder.Entity<UserModel>()
                .Property(x => x.Roles)
                .HasConversion(
                    v => string.Join(",", v.Select(r => ((int)r).ToString())),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => (UserRole)int.Parse(s)).ToList()
                )
                .Metadata.SetValueComparer(
                    new ValueComparer<List<UserRole>>(
                        (c1, c2) => c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()
                    ));

            // 患者模块索引配置
            modelBuilder.Entity<PatientModel>()
                .HasIndex(p => p.IDNumber)
                .IsUnique()
                .HasDatabaseName("IX_Patients_IDNumber");

            modelBuilder.Entity<PatientModel>()
                .HasIndex(p => p.PhoneNumber)
                .HasDatabaseName("IX_Patients_PhoneNumber");

            modelBuilder.Entity<PatientModel>()
                .HasIndex(p => p.PinyinCode)
                .HasDatabaseName("IX_Patients_PinyinCode");

            modelBuilder.Entity<PatientModel>()
                .HasIndex(p => new { p.Name, p.Status })
                .HasDatabaseName("IX_Patients_Name_Status");

            modelBuilder.Entity<PatientModel>()
                .HasIndex(p => p.CreatedAt)
                .HasDatabaseName("IX_Patients_CreatedAt");

            modelBuilder.Entity<PatientModel>()
                .HasIndex(p => p.Status)
                .HasDatabaseName("IX_Patients_Status");

            // 特殊患者医生关系表索引
            modelBuilder.Entity<SpecialPatientDoctor>()
                .HasIndex(s => new { s.PatientId, s.DoctorId })
                .IsUnique()
                .HasDatabaseName("IX_SpecialPatientDoctors_PatientId_DoctorId");

            modelBuilder.Entity<SpecialPatientDoctor>()
                .HasIndex(s => s.DoctorId)
                .HasDatabaseName("IX_SpecialPatientDoctors_DoctorId");

            // === List<HerbItemModel> for FormulaTemplateModel ===
            modelBuilder.Entity<FormulaTemplateModel>()
                .Property(x => x.Herbs)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<HerbItemModel>>(v, (JsonSerializerOptions)null))
                .Metadata.SetValueComparer(
                    new ValueComparer<List<HerbItemModel>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions)null),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions)null).GetHashCode(),
                        c => c == null ? null : JsonSerializer.Deserialize<List<HerbItemModel>>(JsonSerializer.Serialize(c, (JsonSerializerOptions)null), (JsonSerializerOptions)null)
                    ));

            // === List<BillingItem> for BillingModel ===
            modelBuilder.Entity<BillingModel>()
                .Property(x => x.Items)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<BillingItem>>(v, (JsonSerializerOptions)null))
                .Metadata.SetValueComparer(
                    new ValueComparer<List<BillingItem>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions)null),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions)null).GetHashCode(),
                        c => c == null ? null : JsonSerializer.Deserialize<List<BillingItem>>(JsonSerializer.Serialize(c, (JsonSerializerOptions)null), (JsonSerializerOptions)null)
                    ));

            // === List<TreatmentItemModel> for DiagnosisTreatmentModel ===
            modelBuilder.Entity<DiagnosisTreatmentModel>()
                .Property(x => x.Treatments)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<TreatmentItemModel>>(v, (JsonSerializerOptions)null))
                .Metadata.SetValueComparer(
                    new ValueComparer<List<TreatmentItemModel>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions)null),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions)null).GetHashCode(),
                        c => c == null ? null : JsonSerializer.Deserialize<List<TreatmentItemModel>>(JsonSerializer.Serialize(c, (JsonSerializerOptions)null), (JsonSerializerOptions)null)
                    ));

            // === FormulaModel for DiagnosisTreatmentModel ===
            modelBuilder.Entity<DiagnosisTreatmentModel>()
                .Property(x => x.Formula)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<FormulaModel>(v, (JsonSerializerOptions)null));

            // === RecordModel complex fields ===
            modelBuilder.Entity<RecordModel>()
                .Property(x => x.DiagnosisResults)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null))
                .Metadata.SetValueComparer(
                    new ValueComparer<List<string>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions)null),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions)null).GetHashCode(),
                        c => c == null ? null : JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(c, (JsonSerializerOptions)null), (JsonSerializerOptions)null)
                    ));

            modelBuilder.Entity<RecordModel>()
                .Property(x => x.HerbalFormula)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<HerbItemModel>>(v, (JsonSerializerOptions)null))
                .Metadata.SetValueComparer(
                    new ValueComparer<List<HerbItemModel>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions)null),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions)null).GetHashCode(),
                        c => c == null ? null : JsonSerializer.Deserialize<List<HerbItemModel>>(JsonSerializer.Serialize(c, (JsonSerializerOptions)null), (JsonSerializerOptions)null)
                    ));

            modelBuilder.Entity<RecordModel>()
                .Property(x => x.TreatmentPlans)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<TreatmentItemModel>>(v, (JsonSerializerOptions)null))
                .Metadata.SetValueComparer(
                    new ValueComparer<List<TreatmentItemModel>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions)null),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions)null).GetHashCode(),
                        c => c == null ? null : JsonSerializer.Deserialize<List<TreatmentItemModel>>(JsonSerializer.Serialize(c, (JsonSerializerOptions)null), (JsonSerializerOptions)null)
                    ));

            modelBuilder.Entity<RecordModel>()
                .Property(x => x.SharedToDoctorIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null))
                .Metadata.SetValueComparer(
                    new ValueComparer<List<string>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions)null),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions)null).GetHashCode(),
                        c => c == null ? null : JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(c, (JsonSerializerOptions)null), (JsonSerializerOptions)null)
                    ));

            // === 所有金额 decimal 字段加精度（18,2） ===
            modelBuilder.Entity<BillingModel>().Property(x => x.PaidAmount).HasPrecision(18, 2);
            modelBuilder.Entity<BillingModel>().Property(x => x.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<HerbModel>().Property(x => x.Price).HasPrecision(18, 2);
            // 如有其他金额字段都可如此加

            modelBuilder.Entity<DoctorModel>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PrescriptionItemModel>()
                .HasOne<PrescriptionModel>()
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
