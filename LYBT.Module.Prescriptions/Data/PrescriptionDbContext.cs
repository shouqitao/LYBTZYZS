using LYBT.Models.Prescriptions;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Prescriptions.Data {

    /// <summary>
    /// 处方模块数据库上下文
    /// </summary>
    public class PrescriptionDbContext : DbContext {

        public PrescriptionDbContext(DbContextOptions<PrescriptionDbContext> options) : base(options) {
        }

        public DbSet<PrescriptionModel> Prescriptions { get; set; }
        public DbSet<PrescriptionItemModel> PrescriptionItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            ConfigurePrescriptions(modelBuilder);
        }

        private static void ConfigurePrescriptions(ModelBuilder modelBuilder) {
            var prescriptionEntity = modelBuilder.Entity<PrescriptionModel>();
            prescriptionEntity.ToTable("Prescriptions");
            prescriptionEntity.HasKey(p => p.Id);
            prescriptionEntity.HasIndex(p => p.PatientId).HasDatabaseName("IX_Prescriptions_PatientId");
            prescriptionEntity.HasIndex(p => p.DoctorId).HasDatabaseName("IX_Prescriptions_DoctorId");
            prescriptionEntity.HasIndex(p => p.CreateTime).HasDatabaseName("IX_Prescriptions_CreateTime");
            prescriptionEntity.HasIndex(p => p.Status).HasDatabaseName("IX_Prescriptions_Status");

            var itemEntity = modelBuilder.Entity<PrescriptionItemModel>();
            itemEntity.ToTable("PrescriptionItems");
            itemEntity.HasKey(pi => pi.Id);
            itemEntity.HasIndex(pi => pi.PrescriptionId).HasDatabaseName("IX_PrescriptionItems_PrescriptionId");
            itemEntity.HasIndex(pi => pi.HerbId).HasDatabaseName("IX_PrescriptionItems_HerbId");

            // 配置关系
            prescriptionEntity
                .HasMany<PrescriptionItemModel>()
                .WithOne()
                .HasForeignKey(pi => pi.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}