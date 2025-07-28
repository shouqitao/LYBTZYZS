using LYBT.Models.DiagnosisTreatment;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.DiagnosisTreatment.Data {

    /// <summary>
    /// 诊疗模块数据库上下文
    /// </summary>
    public class DiagnosisTreatmentDbContext : DbContext {

        public DiagnosisTreatmentDbContext(DbContextOptions<DiagnosisTreatmentDbContext> options) : base(options) {
        }

        public DbSet<DiagnosisTreatmentModel> DiagnosisTreatments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            ConfigureDiagnosisTreatment(modelBuilder);
        }

        private static void ConfigureDiagnosisTreatment(ModelBuilder modelBuilder) {
            // Configure DiagnosisTreatment entity
            var entity = modelBuilder.Entity<DiagnosisTreatmentModel>();
            entity.ToTable("DiagnosisTreatments");
            entity.HasKey(d => d.Id);

            // Configure owned entity for Treatments collection
            entity.OwnsMany(d => d.Treatments, treatments => {
                treatments.WithOwner().HasForeignKey("DiagnosisTreatmentId");
                treatments.Property<int>("Id");
                treatments.HasKey("Id");
                treatments.ToTable("DiagnosisTreatmentItems");
                treatments.Property(t => t.Name).HasMaxLength(200).IsRequired();
                treatments.Property(t => t.Count);
                treatments.Property(t => t.Price).HasColumnType("decimal(18,2)");
            });

            // Configure owned entity for Formula
            entity.OwnsOne(d => d.Formula, formula => {
                formula.Property(f => f.Name).HasMaxLength(200);

                // Configure owned entity for Herbs collection within Formula
                formula.OwnsMany(f => f.Herbs, herbs => {
                    herbs.WithOwner().HasForeignKey("DiagnosisTreatmentId");
                    herbs.Property<int>("Id");
                    herbs.HasKey("Id");
                    herbs.ToTable("DiagnosisTreatmentFormulaHerbs");
                    herbs.Property(h => h.HerbId);
                    herbs.Property(h => h.Name).HasMaxLength(200).IsRequired();
                    herbs.Property(h => h.Amount).HasColumnType("decimal(10,3)");
                    herbs.Property(h => h.UnitPrice).HasColumnType("decimal(18,2)");
                });
            });
        }
    }
}