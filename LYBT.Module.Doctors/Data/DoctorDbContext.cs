using Microsoft.EntityFrameworkCore;
using LYBT.Module.Doctors.Models;

namespace LYBT.Module.Doctors.Data {
    /// <summary>
    /// 医生模块数据库上下文
    /// </summary>
    public class DoctorDbContext : DbContext {
        public DoctorDbContext(DbContextOptions<DoctorDbContext> options) : base(options) { }

        public DbSet<DoctorModel> Doctors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            ConfigureDoctors(modelBuilder);
        }

        private static void ConfigureDoctors(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<DoctorModel>();
            entity.ToTable("Doctors");
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.UserId).HasDatabaseName("IX_Doctors_UserId");
            entity.HasIndex(d => d.PinyinCode).HasDatabaseName("IX_Doctors_PinyinCode");
            entity.HasIndex(d => d.Status).HasDatabaseName("IX_Doctors_Status");
            entity.HasIndex(d => d.Specialty).HasDatabaseName("IX_Doctors_Specialty");
            entity.HasIndex(d => d.Title).HasDatabaseName("IX_Doctors_Title");
        }
    }
}