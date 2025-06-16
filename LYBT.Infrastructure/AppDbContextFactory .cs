using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LYBT.Infrastructure {
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> {
        public AppDbContext CreateDbContext(string[] args) {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            // 下面请填写你的连接字符串（与 appsettings.json 里一致）
            optionsBuilder.UseSqlServer("Server=60.190.215.86;Database=LYBTDB;User Id=sa;Password=Shou@850528;Encrypt=True;TrustServerCertificate=True");
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
