using LYBT.Models.Herbs;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Data {

    /// <summary>
    /// 中药模块数据库上下文
    /// 只包含中药相关实体
    /// </summary>
    public class HerbDbContext : DbContext {

        public HerbDbContext(DbContextOptions<HerbDbContext> options) : base(options) {
        }

        // 中药相关
        public DbSet<HerbModel> Herbs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            ConfigureHerbs(modelBuilder);
        }

        private static void ConfigureHerbs(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<HerbModel>();
            entity.ToTable("Herbs");
            entity.HasKey(h => h.Id);
            entity.HasIndex(h => h.Name).HasDatabaseName("IX_Herbs_Name");
            entity.HasIndex(h => h.PinyinCode).HasDatabaseName("IX_Herbs_PinyinCode");
            entity.HasIndex(h => h.Status).HasDatabaseName("IX_Herbs_Status");
            //entity.HasIndex(h => h.Category).HasDatabaseName("IX_Herbs_Category");
            entity.HasIndex(h => h.CreatedAt).HasDatabaseName("IX_Herbs_CreatedAt");
        }
    }
}