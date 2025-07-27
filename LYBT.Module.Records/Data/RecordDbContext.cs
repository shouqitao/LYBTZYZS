using Microsoft.EntityFrameworkCore;
using LYBT.Module.Records.Models;

namespace LYBT.Module.Records.Data {
    /// <summary>
    /// 病历模块数据库上下文
    /// </summary>
    public class RecordDbContext : DbContext {
        public RecordDbContext(DbContextOptions<RecordDbContext> options) : base(options) { }

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
            entity.HasIndex(r => r.RecordType).HasDatabaseName("IX_Records_RecordType");
        }
    }
}
