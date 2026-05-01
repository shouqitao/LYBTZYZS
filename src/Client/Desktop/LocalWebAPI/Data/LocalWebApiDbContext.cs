using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Data.Configuration;
using LYBT.Entities.Users;
using LYBT.Entities.Auth;
using LYBT.Entities.Patients;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Consultations;
using LYBT.Entities.Prescriptions;
using LYBT.Entities.Herbs;
using LYBT.Entities.Formulas;
using LYBT.Entities.Registrations;
using LYBT.Entities.Common;
using AppDbContext = LYBT.Infrastructure.Data.AppDbContext;

namespace LYBT.LocalWebAPI.Data;

public class LocalWebApiDbContext : DbContext
{
    public LocalWebApiDbContext(DbContextOptions<LocalWebApiDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<AuthSession> AuthSessions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AutoLoginToken> AutoLoginTokens { get; set; }
    public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<MedicalCase> MedicalCases { get; set; }
    public DbSet<MedicalCaseAuditLog> MedicalCaseAuditLogs { get; set; }
    public DbSet<Consultation> Consultations { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
    public DbSet<MedicalCasePrintLog> MedicalCasePrintLogs { get; set; }
    public DbSet<Herb> Herbs { get; set; }
    public DbSet<Formula> Formulas { get; set; }
    public DbSet<Registration> Registrations { get; set; }
    public DbSet<SystemLog> SystemLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyOptimizations();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
