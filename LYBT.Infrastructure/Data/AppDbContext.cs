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

namespace LYBT.Infrastructure.Data {

    /// <summary>
    /// 统一应用数据库上下文 - 整个项目使用单一数据库LYBTDB
    /// </summary>
    public class AppDbContext : DbContext {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
        }

        // 用户管理
        public DbSet<UserModel> Users { get; set; }

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

        // 计费管理
        public DbSet<BillingModel> Billings { get; set; }

        // 病历管理
        public DbSet<RecordModel> Records { get; set; }

        // 治疗室管理
        public DbSet<TreatmentRoomModel> TreatmentRooms { get; set; }

        // 同步管理
        public DbSet<SyncTaskModel> SyncTasks { get; set; }

        public DbSet<SyncLogModel> SyncLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            // 配置各个实体的映射关系
            ConfigureUsers(modelBuilder);
            ConfigurePatients(modelBuilder);
            ConfigureDoctors(modelBuilder);
            ConfigureRegistrations(modelBuilder);
            ConfigureQueueings(modelBuilder);
            ConfigureDiagnosisTreatments(modelBuilder);
            ConfigurePrescriptions(modelBuilder);
            ConfigureHerbs(modelBuilder);
            ConfigureFormulaTemplates(modelBuilder);
            ConfigurePharmacies(modelBuilder);
            ConfigureBillings(modelBuilder);
            ConfigureRecords(modelBuilder);
            ConfigureTreatmentRooms(modelBuilder);
            ConfigureSyncs(modelBuilder);
        }

        private static void ConfigureUsers(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<UserModel>();
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.UserName).IsUnique();
            entity.Property(u => u.UserName).HasMaxLength(50);
            entity.Property(u => u.RealName).HasMaxLength(100);
            entity.Property(u => u.PasswordHash).HasMaxLength(255);
        }

        private static void ConfigurePatients(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<PatientModel>();
            entity.ToTable("Patients");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(100);
            entity.Property(p => p.WuBiCode).HasMaxLength(20);
        }

        private static void ConfigureDoctors(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<DoctorModel>();
            entity.ToTable("Doctors");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Specialty).HasMaxLength(100);
            entity.Property(d => d.LicenseNumber).HasMaxLength(32);
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
            entity.Property(h => h.PinyinCode).HasMaxLength(20);
        }

        private static void ConfigureFormulaTemplates(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<FormulaTemplateModel>();
            entity.ToTable("FormulaTemplates");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).HasMaxLength(200);

            entity.OwnsMany(f => f.Herbs, herbs => {
                herbs.WithOwner().HasForeignKey("FormulaTemplateId");
                herbs.Property<int>("Id");
                herbs.HasKey("Id");
                herbs.ToTable("FormulaTemplateHerbs");
                herbs.Property(h => h.HerbId);
                herbs.Property(h => h.Name).HasMaxLength(200);
                herbs.Property(h => h.HerbName).HasMaxLength(200);
                herbs.Property(h => h.Quantity).HasColumnType("decimal(10,3)");
                herbs.Property(h => h.Amount).HasColumnType("decimal(10,3)");
                herbs.Property(h => h.Unit).HasMaxLength(50);
                herbs.Property(h => h.UnitPrice).HasColumnType("decimal(10,2)");
                herbs.Property(h => h.Usage).HasMaxLength(500);
                herbs.Property(h => h.Remark).HasMaxLength(1000);
            });
        }

        private static void ConfigurePharmacies(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<PharmacyModel>();
            entity.ToTable("Pharmacies");
            entity.HasKey(p => p.Id);
        }

        private static void ConfigureBillings(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<BillingModel>();
            entity.ToTable("Billings");
            entity.HasKey(b => b.Id);
        }

        private static void ConfigureRecords(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<RecordModel>();
            entity.ToTable("Records");
            entity.HasKey(r => r.Id);
        }

        private static void ConfigureTreatmentRooms(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<TreatmentRoomModel>();
            entity.ToTable("TreatmentRooms");
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
    }
}