using LYBT.Models.Records;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Records.Data {

    /// <summary>
    /// 病历模块数据库上下文
    /// </summary>
    public class RecordDbContext : DbContext {

        public RecordDbContext(DbContextOptions<RecordDbContext> options) : base(options) {
        }

        public DbSet<RecordModel> Records { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            ConfigureRecords(modelBuilder);
        }

        private static void ConfigureRecords(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<RecordModel>();
            entity.ToTable("Records");
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.PatientId).HasDatabaseName("IX_Records_PatientId");
            entity.HasIndex(r => r.DoctorId).HasDatabaseName("IX_Records_DoctorId");
            entity.HasIndex(r => r.RecordTime).HasDatabaseName("IX_Records_RecordTime");

            // 配置HerbItemModel为拥有实体
            entity.OwnsMany(r => r.HerbalFormula, herb => {
                herb.WithOwner().HasForeignKey("RecordId");
                herb.Property<int>("Id");
                herb.HasKey("Id");
                herb.ToTable("RecordHerbalFormula");
                herb.Property(h => h.HerbId);
                herb.Property(h => h.Name).HasMaxLength(200).IsRequired();
                herb.Property(h => h.Amount).HasColumnType("decimal(10,3)");
                herb.Property(h => h.UnitPrice).HasColumnType("decimal(18,2)");
            });

            // 配置TreatmentItemModel为拥有实体
            entity.OwnsMany(r => r.TreatmentPlans, treatment => {
                treatment.WithOwner().HasForeignKey("RecordId");
                treatment.Property<int>("Id");
                treatment.HasKey("Id");
                treatment.ToTable("RecordTreatmentPlans");
                treatment.Property(t => t.Name).HasMaxLength(200).IsRequired();
                treatment.Property(t => t.Count);
                treatment.Property(t => t.Price).HasColumnType("decimal(18,2)");
            });
        }
    }
}