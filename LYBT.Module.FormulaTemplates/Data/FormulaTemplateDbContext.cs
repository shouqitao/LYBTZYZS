using Microsoft.EntityFrameworkCore;
using LYBT.Module.FormulaTemplates.Models;

namespace LYBT.Module.FormulaTemplates.Data {
    /// <summary>
    /// 经验方模板模块数据库上下文
    /// 包含经验方模板相关实体
    /// </summary>
    public class FormulaTemplateDbContext : DbContext {
        public FormulaTemplateDbContext(DbContextOptions<FormulaTemplateDbContext> options) : base(options) { }

        // 经验方模板相关
        public DbSet<FormulaTemplateModel> FormulaTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            
            ConfigureFormulaTemplates(modelBuilder);
        }

        private static void ConfigureFormulaTemplates(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<FormulaTemplateModel>();
            entity.ToTable("FormulaTemplates");
            entity.HasKey(f => f.Id);
            entity.HasIndex(f => f.Name).HasDatabaseName("IX_FormulaTemplates_Name");
            entity.HasIndex(f => f.PinyinCode).HasDatabaseName("IX_FormulaTemplates_PinyinCode");
            entity.HasIndex(f => f.Category).HasDatabaseName("IX_FormulaTemplates_Category");
            entity.HasIndex(f => f.IsActive).HasDatabaseName("IX_FormulaTemplates_IsActive");
            entity.HasIndex(f => f.CreatedAt).HasDatabaseName("IX_FormulaTemplates_CreatedAt");
        }
    }
}
