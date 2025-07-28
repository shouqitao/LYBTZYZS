using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LYBT.Infrastructure.Data {

    /// <summary>
    /// 基础设施数据库上下文工厂
    /// </summary>
    public class InfrastructureDbContextFactory : IDesignTimeDbContextFactory<InfrastructureDbContext> {

        public InfrastructureDbContext CreateDbContext(string[] args) {
            var optionsBuilder = new DbContextOptionsBuilder<InfrastructureDbContext>();

            // 使用与运行时相同的连接字符串
            optionsBuilder.UseSqlServer("Server=localhost;Database=LYBTDB;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;");

            return new InfrastructureDbContext(optionsBuilder.Options);
        }
    }
}