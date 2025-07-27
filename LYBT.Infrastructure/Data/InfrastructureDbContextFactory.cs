using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LYBT.Infrastructure.Data {

    /// <summary>
    /// 基础设施数据库上下文工厂
    /// </summary>
    public class InfrastructureDbContextFactory : IDesignTimeDbContextFactory<InfrastructureDbContext> {

        public InfrastructureDbContext CreateDbContext(string[] args) {
            var optionsBuilder = new DbContextOptionsBuilder<InfrastructureDbContext>();
            
            // 使用默认连接字符串进行设计时迁移
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LYBTInfrastructure;Trusted_Connection=true;");

            return new InfrastructureDbContext(optionsBuilder.Options);
        }
    }
}