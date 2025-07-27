using Microsoft.EntityFrameworkCore;
using LYBT.Module.Registration.Models;

namespace LYBT.Module.Registration.Data {
    /// <summary>
    /// 挂号模块数据库上下文
    /// </summary>
    public class RegistrationDbContext : DbContext {
        public RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : base(options) { }

        public DbSet<RegistrationModel> Registrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            
            ConfigureRegistration(modelBuilder);
        }

        private static void ConfigureRegistration(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<RegistrationModel>();
            entity.ToTable("Registrations");
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.PatientId).HasDatabaseName("IX_Registrations_PatientId");
            entity.HasIndex(r => r.DoctorId).HasDatabaseName("IX_Registrations_DoctorId");
            entity.HasIndex(r => r.RegistrationTime).HasDatabaseName("IX_Registrations_RegistrationTime");
            entity.HasIndex(r => r.Status).HasDatabaseName("IX_Registrations_Status");
        }
    }
}
