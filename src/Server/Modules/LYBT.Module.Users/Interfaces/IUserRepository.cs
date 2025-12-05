using LYBT.Entities.Users;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Users.Interfaces
{
    /// <summary>
    /// 用户仓储接口 - 简化版本，只保留最基础的方法
    /// </summary>
    /// <summary>
/// 用户仓储接口 - 继承IRepository<User>标准接口
/// Phase 1 Task 1.2: 实现基础数据模块统一Repository规范
/// </summary>
/// <remarks>
/// 设计原则：
/// - ⭐ 统一共性：继承IRepository<User>获得11个标准CRUD方法
/// - ⭐ 保持特性：保留用户模块特定业务方法
/// 
/// 特定业务方法说明：
/// - GetByUsernameAsync: 用户名登录查询
/// - UsernameExistsAsync: 用户名唯一性校验
/// </remarks>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// 根据用户名获取用户（支持用户名或邮箱登录）
    /// </summary>
    /// <param name="username">用户名或邮箱</param>
    /// <returns>用户对象，不存在时返回null</returns>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// 检查用户名是否已存在
    /// </summary>
    /// <param name="username">待检查的用户名</param>
    /// <returns>存在返回true，否则返回false</returns>
    Task<bool> UsernameExistsAsync(string username);
}
}
