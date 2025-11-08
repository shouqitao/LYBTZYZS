using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token 存储服务实现 - 内存存储（Session级别）
    /// Issue #1907: Token改为内存存储 - 符合医疗系统安全要求
    ///
    /// 设计原则：
    /// 1. Token = 会话级数据（Session Token），应用关闭即失效
    /// 2. 存储方式：进程内存（不持久化到磁盘）
    /// 3. 生命周期：应用启动 → 用户登录 → 应用退出
    /// 4. 安全原则：数据安全高于方便
    ///
    /// 医疗系统特殊要求：
    /// - 每次启动必须输入密码（合规性要求）
    /// - 多人共享工作站安全（患者隐私保护）
    /// - 审计追溯完整（每次登录可追踪）
    /// - 进程结束自动清除（任何退出方式都安全）
    /// </summary>
    public class TokenStorageService : ITokenStorageService
    {
        private readonly ILogger<TokenStorageService> _logger;
        private LoginResponse? _cachedLoginResponse; // 内存缓存（Session级别）

        public TokenStorageService(ILogger<TokenStorageService> logger)
        {
            _logger = logger;
            _logger.LogDebug("TokenStorageService 初始化（内存存储模式）");
        }

        /// <summary>
        /// 保存认证信息到内存（Session级别）
        /// </summary>
        /// <param name="loginResponse">登录响应数据</param>
        /// <param name="rememberMe">忽略此参数 - Issue #1907：医疗系统不支持RememberMe，始终使用Session存储</param>
        /// <remarks>
        /// Token仅存储在进程内存中，应用退出后自动清除（包括：
        /// - 正常退出：用户点击关闭
        /// - 异常退出：应用崩溃、强制结束、断电
        ///
        /// 操作系统保证：进程结束 → 内存自动回收 → Token自动清除
        /// </remarks>
        public async Task SaveAuthenticationAsync(LoginResponse loginResponse, bool rememberMe)
        {
            try
            {
                if (loginResponse == null)
                {
                    throw new ArgumentNullException(nameof(loginResponse));
                }

                // 保存到内存缓存（Session级别）
                _cachedLoginResponse = loginResponse;

                _logger.LogDebug("Token已保存到内存（Session级别，应用退出即失效）");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存认证信息失败");
                throw;
            }
        }

        /// <summary>
        /// 获取当前保存的Token
        /// </summary>
        public async Task<string?> GetTokenAsync()
        {
            var loginResponse = await GetLoginResponseAsync();
            return loginResponse?.Token;
        }

        /// <summary>
        /// 获取当前保存的RefreshToken
        /// </summary>
        public async Task<string?> GetRefreshTokenAsync()
        {
            var loginResponse = await GetLoginResponseAsync();
            return loginResponse?.RefreshToken;
        }

        /// <summary>
        /// 获取完整的登录响应数据（从内存）
        /// </summary>
        /// <remarks>
        /// 仅在当前应用会话中有效。
        /// 应用重启后返回null（必须重新登录）。
        /// </remarks>
        public async Task<LoginResponse?> GetLoginResponseAsync()
        {
            if (_cachedLoginResponse != null)
            {
                _logger.LogDebug("从内存读取Token（Session有效）");
            }
            else
            {
                _logger.LogDebug("内存中无Token（需要登录）");
            }

            return await Task.FromResult(_cachedLoginResponse);
        }

        /// <summary>
        /// 清除内存中的认证信息
        /// </summary>
        /// <remarks>
        /// 用于：
        /// - 用户主动logout
        /// - 密码修改后强制重新登录
        /// - Token验证失败时清除
        ///
        /// 注：应用退出时无需调用，进程结束会自动清除内存。
        /// </remarks>
        public async Task ClearAuthenticationAsync()
        {
            _cachedLoginResponse = null;
            _logger.LogDebug("Token已从内存清除");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 检查Token是否过期
        /// </summary>
        public async Task<bool> IsTokenExpiredAsync()
        {
            var loginResponse = await GetLoginResponseAsync();

            if (loginResponse == null)
            {
                return true; // 无Token视为已过期
            }

            // 检查过期时间(留5分钟缓冲)
            var isExpired = loginResponse.ExpiresAt <= DateTime.UtcNow.AddMinutes(5);

            if (isExpired)
            {
                _logger.LogWarning("Token已过期或即将过期,ExpiresAt: {ExpiresAt}", loginResponse.ExpiresAt);
            }

            return isExpired;
        }
    }
}
