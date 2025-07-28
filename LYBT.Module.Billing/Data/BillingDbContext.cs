using LYBT.Models.Billing;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Billing.Data {

    /// <summary>
    /// 计费模块数据库上下文
    /// </summary>
    public class BillingDbContext : DbContext {

        public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) {
        }

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
            entity.HasIndex(b => b.BillingTime).HasDatabaseName("IX_Billings_BillingTime");
            entity.HasIndex(b => b.Status).HasDatabaseName("IX_Billings_Status");
            entity.HasIndex(b => b.TotalAmount).HasDatabaseName("IX_Billings_TotalAmount");

            // Configure owned entity for Items collection
            entity.OwnsMany(b => b.Items, items => {
                items.WithOwner().HasForeignKey("BillingId");
                items.Property<int>("Id");
                items.HasKey("Id");
                items.ToTable("BillingItems");
                items.Property(i => i.ItemId);
                items.Property(i => i.Name).HasMaxLength(64).IsRequired();
                items.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
                items.Property(i => i.Quantity).HasColumnType("decimal(18,2)");
            });
        }
    }
}