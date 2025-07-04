using LYBT.Models;
using LYBT.Models.Billing;
using LYBT.Models.DiagnosisTreatment;
using LYBT.Models.Doctors;
using LYBT.Models.FormulaTemplates;
using LYBT.Models.Logs;
using LYBT.Models.Patient;
using LYBT.Models.Pharmacy;
using LYBT.Models.Queueing;
using LYBT.Models.Records;
using LYBT.Models.Registration;
using LYBT.Models.Settings;
using LYBT.Models.TreatmentRoom;
using LYBT.Module.Patients.Models;
using LYBT.Module.Users.Models;
using LYBT.Common.Enums.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using System.Collections.Generic;

namespace LYBT.Infrastructure {

    /// <summary>
    /// 主数据库上下文，统一管理所有主表和明细字段配置
    /// </summary>
    public class AppDbContext : DbContext {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
        }

        // ===== 主表 DbSet，全部要有 =====
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
        public DbSet<RecordModel> Records { get; set; }
        public DbSet<SettingsModel> Settings { get; set; }
        public DbSet<DiagnosisCatalogModel> DiagnosisCatalogs { get; set; }
        public DbSet<TreatmentCatalogModel> TreatmentCatalogs { get; set; }
        public DbSet<GlobalSettingsModel> GlobalSettings { get; set; }
        public DbSet<TreatmentRoomModel> TreatmentRooms { get; set; }
        public DbSet<SpecialPatientDoctor> SpecialPatientDoctors { get; set; }
        public DbSet<AdminSecretModel> AdminSecrets { get; set; }
        public DbSet<DoctorInfoRequestModel> DoctorInfoRequests { get; set; }

        /// <summary>
        /// 配置明细字段Json存储，decimal金额加精度，集合加ValueComparer
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            // === List<UserRole> for UserModel ===
            modelBuilder.Entity<UserModel>()
                .Property(x => x.Roles)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<UserRole>>(v, (JsonSerializerOptions)null))
                .Metadata.SetValueComparer(
                    new ValueComparer<List<UserRole>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions)null),
                        c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions)null).GetHashCode(),
                        c => c == null ? null : JsonSerializer.Deserialize<List<UserRole>>(JsonSerializer.Serialize(c, (JsonSerializerOptions)null), (JsonSerializerOptions)null)
                    ));

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
        }
    }
}