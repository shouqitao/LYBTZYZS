using System;

namespace LYBT.Infrastructure.Services
{
    /// <summary>
    /// 当前用户服务接口
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// 获取当前用户名
        /// </summary>
        string? UserName { get; }

        /// <summary>
        /// 是否已认证
        /// </summary>
        bool IsAuthenticated { get; }
    }
}