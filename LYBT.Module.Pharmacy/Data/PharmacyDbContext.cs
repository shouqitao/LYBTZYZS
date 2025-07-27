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
            entity.HasIndex(p => p.Name).HasDatabaseName("IX_Pharmacies_Name");
            entity.HasIndex(p => p.Location).HasDatabaseName("IX_Pharmacies_Location");
            entity.HasIndex(p => p.IsActive).HasDatabaseName("IX_Pharmacies_IsActive");
        }
    }
}
