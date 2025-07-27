using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LYBT.Module.Prescriptions.Data {
    /// <summary>
    /// 设计时数据库上下文工厂
    /// 用于EF Core工具（如migrations）在设计时创建PrescriptionDbContext实例
    /// </summary>
    public class PrescriptionDbContextFactory : IDesignTimeDbContextFactory<PrescriptionDbContext> {
        public PrescriptionDbContext CreateDbContext(string[] args) {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<PrescriptionDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                                  ?? "Server=(localdb)\\mssqllocaldb;Database=LYBTDB;Trusted_Connection=true;";

            optionsBuilder.UseSqlServer(connectionString, options => {
                options.MigrationsAssembly("LYBT.Module.Prescriptions");
            });

            return new PrescriptionDbContext(optionsBuilder.Options);
        }
    }
}
