using Microsoft.EntityFrameworkCore;
using LYBT.Module.Queueing.Models;

namespace LYBT.Module.Queueing.Data {
    /// <summary>
    /// 排队模块数据库上下文
    /// </summary>
    public class QueueingDbContext : DbContext {
        public QueueingDbContext(DbContextOptions<QueueingDbContext> options) : base(options) { }

        public DbSet<QueueingModel> Queueings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            
            ConfigureQueueing(modelBuilder);
        }

        private static void ConfigureQueueing(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<QueueingModel>();
            entity.ToTable("Queueings");
            entity.HasKey(q => q.Id);
            entity.HasIndex(q => q.PatientId).HasDatabaseName("IX_Queueings_PatientId");
            entity.HasIndex(q => q.DoctorId).HasDatabaseName("IX_Queueings_DoctorId");
            entity.HasIndex(q => q.QueueTime).HasDatabaseName("IX_Queueings_QueueTime");
            entity.HasIndex(q => q.Status).HasDatabaseName("IX_Queueings_Status");
            entity.HasIndex(q => q.QueueType).HasDatabaseName("IX_Queueings_QueueType");
        }
    }
}
