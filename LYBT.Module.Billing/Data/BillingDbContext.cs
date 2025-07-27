using Microsoft.EntityFrameworkCore;
using LYBT.Module.Billing.Models;

namespace LYBT.Module.Billing.Data {
    /// <summary>
    /// 计费模块数据库上下文
    /// </summary>
    public class BillingDbContext : DbContext {
        public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

        public DbSet<BillingModel> Billings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            ConfigureBilling(modelBuilder);
        }

        private static void ConfigureBilling(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<BillingModel>();
            entity.ToTable("Billings");
            entity.HasKey(b => b.Id);
            entity.HasIndex(b => b.PatientId).HasDatabaseName("IX_Billings_PatientId");
            entity.HasIndex(b => b.BillingDate).HasDatabaseName("IX_Billings_BillingDate");
            entity.HasIndex(b => b.Status).HasDatabaseName("IX_Billings_Status");
            entity.HasIndex(b => b.TotalAmount).HasDatabaseName("IX_Billings_TotalAmount");
        }
    }
}
