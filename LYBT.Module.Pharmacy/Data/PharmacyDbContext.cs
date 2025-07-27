using Microsoft.EntityFrameworkCore;
using LYBT.Module.Pharmacy.Models;

namespace LYBT.Module.Pharmacy.Data {
    /// <summary>
    /// 药房模块数据库上下文
    /// </summary>
    public class PharmacyDbContext : DbContext {
        public PharmacyDbContext(DbContextOptions<PharmacyDbContext> options) : base(options) { }

        public DbSet<PharmacyModel> Pharmacies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            ConfigurePharmacy(modelBuilder);
        }

        private static void ConfigurePharmacy(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<PharmacyModel>();
            entity.ToTable("Pharmacies");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.PrescriptionId).HasDatabaseName("IX_Pharmacies_PrescriptionId");
            entity.HasIndex(p => p.PatientId).HasDatabaseName("IX_Pharmacies_PatientId");
            entity.HasIndex(p => p.Status).HasDatabaseName("IX_Pharmacies_Status");
            entity.HasIndex(p => p.CreateTime).HasDatabaseName("IX_Pharmacies_CreateTime");
        }
    }
}
