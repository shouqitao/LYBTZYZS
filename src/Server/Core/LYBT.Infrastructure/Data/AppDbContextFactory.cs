using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LYBT.Infrastructure.Data {

    /// <summary>
    /// 统一应用数据库上下文设计时工厂
    /// 用于EF Core工具（如migrations）在设计时创建AppDbContext实例
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> {

        public AppDbContext CreateDbContext(string[] args) {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                  ?? "Server=192.168.190.243;Database=LYBTDB;User Id=sa;Password=Shou@850528;TrustServerCertificate=true;";

            optionsBuilder.UseSqlServer(connectionString, options => {
                options.MigrationsAssembly("LYBT.Infrastructure");
            });

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
