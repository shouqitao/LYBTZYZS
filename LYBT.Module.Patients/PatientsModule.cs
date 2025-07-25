using LYBT.Module.Patients.Data;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Patients {
    /// <summary>
    /// Dependency injection entry for Patients module
    /// </summary>
    public static class PatientsModule {
        public static IServiceCollection AddPatientsModule(this IServiceCollection services, string connectionString) {
            services.AddDbContext<PatientsDbContext>(opts => opts.UseSqlServer(connectionString));
            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IPatientService, PatientService>();
            return services;
        }
    }
}
