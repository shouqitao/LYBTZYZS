using LYBT.Entities.Users;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Users.Interfaces
{
    /// <summary>
    /// 用户仓储接口 - 简化版本，只保留最基础的方法
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<bool> IsEmailExistsAsync(string email);
    }
}