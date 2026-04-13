using LYBT.Infrastructure.Data;

namespace LYBT.Infrastructure.Interfaces
{
    /// <summary>
    /// 数据库上下文访问器实现
    /// P-10: 将 AppDbContext 的注入点集中到此处，服务层通过接口访问
    /// </summary>
    public class DbContextAccessor : IDbContextAccessor
    {
        public AppDbContext Context { get; }

        public DbContextAccessor(AppDbContext context)
        {
            Context = context;
        }
    }
}
