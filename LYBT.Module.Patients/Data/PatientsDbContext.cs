using LYBT.Module.Patients.Models;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Data {
    /// <summary>
    /// DbContext for patients module.
    /// </summary>
    public class PatientsDbContext : DbContext {
        public PatientsDbContext(DbContextOptions<PatientsDbContext> options) : base(options) { }

        public DbSet<PatientModel> Patients { get; set; }
        public DbSet<SpecialPatientDoctor> SpecialPatientDoctors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            ConfigurePatientModule(modelBuilder);
        }

        private static void ConfigurePatientModule(ModelBuilder modelBuilder) {
            var patientEntity = modelBuilder.Entity<PatientModel>();
            patientEntity.HasIndex(p => p.IDNumber).IsUnique().HasDatabaseName("IX_Patients_IDNumber");
            patientEntity.HasIndex(p => p.PhoneNumber).HasDatabaseName("IX_Patients_PhoneNumber");
            patientEntity.HasIndex(p => p.PinyinCode).HasDatabaseName("IX_Patients_PinyinCode");
            patientEntity.HasIndex(p => new { p.Name, p.Status }).HasDatabaseName("IX_Patients_Name_Status");
            patientEntity.HasIndex(p => p.CreatedAt).HasDatabaseName("IX_Patients_CreatedAt");
            patientEntity.HasIndex(p => p.Status).HasDatabaseName("IX_Patients_Status");

            ConfigureSpecialPatientDoctorRelation(modelBuilder);
        }

        private static void ConfigureSpecialPatientDoctorRelation(ModelBuilder modelBuilder) {
            var specialEntity = modelBuilder.Entity<SpecialPatientDoctor>();
            specialEntity.HasIndex(s => new { s.PatientId, s.DoctorId })
                .IsUnique()
                .HasDatabaseName("IX_SpecialPatientDoctors_PatientId_DoctorId");
            specialEntity.HasIndex(s => s.DoctorId).HasDatabaseName("IX_SpecialPatientDoctors_DoctorId");
        }
    }
}
