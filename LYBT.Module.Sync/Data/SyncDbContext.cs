using LYBT.Models.Sync;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Sync.Data {

    /// <summary>
    /// 数据同步模块数据库上下文
    /// </summary>
    public class SyncDbContext : DbContext {

        /// <summary>
        /// 构造函数
        /// </summary>
        public SyncDbContext(DbContextOptions<SyncDbContext> options) : base(options) {
        }

        /// <summary>
        /// 同步任务数据集
        /// </summary>
        public DbSet<SyncTaskModel> SyncTasks { get; set; }

        /// <summary>
        /// 同步日志数据集
        /// </summary>
        public DbSet<SyncLogModel> SyncLogs { get; set; }

        /// <summary>
        /// 配置数据库模型
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            ConfigureSyncTasks(modelBuilder);
            ConfigureSyncLogs(modelBuilder);
        }

        /// <summary>
        /// 配置同步任务表
        /// </summary>
        private static void ConfigureSyncTasks(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<SyncTaskModel>();
            entity.ToTable("SyncTasks");
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.TaskType).HasDatabaseName("IX_SyncTasks_TaskType");
            entity.HasIndex(s => s.Status).HasDatabaseName("IX_SyncTasks_Status");
        }

        /// <summary>
        /// 配置同步日志表
        /// </summary>
        private static void ConfigureSyncLogs(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<SyncLogModel>();
            entity.ToTable("SyncLogs");
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.SyncTime).HasDatabaseName("IX_SyncLogs_SyncTime");
            entity.HasIndex(s => s.Status).HasDatabaseName("IX_SyncLogs_Status");
        }
    }
}