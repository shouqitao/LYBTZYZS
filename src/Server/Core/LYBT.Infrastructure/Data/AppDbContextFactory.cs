using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LYBT.Infrastructure.Data
{

    /// <summary>
    /// 统一应用数据库上下文设计时工厂
    /// 用于EF Core工具（如migrations）在设计时创建AppDbContext实例
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {

        /// <inheritdoc/>
        public AppDbContext CreateDbContext(string[] args)
        {
            // ADR-0019 配置集中: config/ 子目录（与 Program 加载链一致）
            var configDir = Path.Combine(AppContext.BaseDirectory, "config");
            var configuration = new ConfigurationBuilder()
                .SetBasePath(configDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                  ?? throw new InvalidOperationException("DefaultConnection not found in configuration. Please configure connection string via user secrets or environment variables.");

            optionsBuilder.UseSqlServer(connectionString, options =>
            {
                options.MigrationsAssembly("LYBT.Infrastructure");
            });

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}


