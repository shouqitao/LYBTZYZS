using LYBT.Models.FormulaTemplates;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.FormulaTemplates.Data {

    /// <summary>
    /// 经验方模板模块数据库上下文
    /// 包含经验方模板相关实体
    /// </summary>
    public class FormulaTemplateDbContext : DbContext {

        public FormulaTemplateDbContext(DbContextOptions<FormulaTemplateDbContext> options) : base(options) {
        }

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

            // Configure owned entity for Herbs collection
            entity.OwnsMany(f => f.Herbs, herbs => {
                herbs.WithOwner().HasForeignKey("FormulaTemplateId");
                herbs.Property<int>("Id");
                herbs.HasKey("Id");
                herbs.ToTable("FormulaTemplateHerbs");
                herbs.Property(h => h.HerbId);
                herbs.Property(h => h.Name).HasMaxLength(200);
                herbs.Property(h => h.HerbName).HasMaxLength(200);
                herbs.Property(h => h.Quantity).HasColumnType("decimal(10,3)");
                herbs.Property(h => h.Amount).HasColumnType("decimal(10,3)");
                herbs.Property(h => h.Unit).HasMaxLength(50);
                herbs.Property(h => h.UnitPrice).HasColumnType("decimal(10,2)");
                herbs.Property(h => h.Usage).HasMaxLength(500);
                herbs.Property(h => h.Remark).HasMaxLength(1000);
            });
        }
    }
}