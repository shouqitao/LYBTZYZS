using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace LYBT.Infrastructure {

    /// <summary>
    /// 设计时创建 DbContext 的工厂
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> {

        /// <summary>
        /// 创建数据库上下文实例
        /// </summary>
        public AppDbContext CreateDbContext(string[] args) {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}