using LYBT.Infrastructure.Data;

namespace LYBT.Infrastructure.Interfaces
{
    /// <summary>
    /// 数据库上下文访问器接口
    /// P-10: 基础设施服务通过此接口间接访问 AppDbContext，避免直接注入
    /// </summary>
    public interface IDbContextAccessor
    {
        /// <summary>
        /// 获取 AppDbContext 实例
        /// </summary>
        AppDbContext Context { get; }
    }
}
