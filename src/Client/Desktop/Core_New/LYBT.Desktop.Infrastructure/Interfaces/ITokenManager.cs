namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// Token管理接口 - UltraThink架构Token抽象
    /// 负责JWT Token的存储、验证、刷新、过期处理
    /// </summary>
    public interface ITokenManager
    {
        /// <summary>
        /// 当前Token
        /// </summary>
        string? CurrentToken { get; }

        /// <summary>
        /// Token是否有效
        /// </summary>
        bool IsTokenValid { get; }

        /// <summary>
        /// Token过期时间
        /// </summary>
        DateTime? TokenExpiration { get; }

        /// <summary>
        /// 设置Token
        /// </summary>
        /// <param name="token">JWT Token</param>
        void SetToken(string token);

        /// <summary>
        /// 清除Token
        /// </summary>
        void ClearToken();

        /// <summary>
        /// 刷新Token
        /// </summary>
        /// <returns>新Token</returns>
        Task<string?> RefreshTokenAsync();

        /// <summary>
        /// 验证Token是否有效
        /// </summary>
        /// <param name="token">要验证的Token</param>
        /// <returns>是否有效</returns>
        bool ValidateToken(string? token = null);

        /// <summary>
        /// 获取Token中的用户ID
        /// </summary>
        /// <returns>用户ID</returns>
        string? GetUserIdFromToken();

        /// <summary>
        /// 获取Token中的用户名
        /// </summary>
        /// <returns>用户名</returns>
        string? GetUsernameFromToken();

        /// <summary>
        /// 检查Token是否即将过期
        /// </summary>
        /// <param name="minutesBeforeExpiry">过期前分钟数</param>
        /// <returns>是否即将过期</returns>
        bool IsTokenExpiringSoon(int minutesBeforeExpiry = 5);

        /// <summary>
        /// Token即将过期事件
        /// </summary>
        event EventHandler? TokenExpiring;

        /// <summary>
        /// Token已过期事件
        /// </summary>
        event EventHandler? TokenExpired;
    }
}
