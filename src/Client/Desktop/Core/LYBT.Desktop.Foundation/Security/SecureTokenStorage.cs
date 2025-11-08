using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 安全Token存储实现 - 内存存储（Session级别）
    /// </summary>
    /// <remarks>
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
    ///
    /// Token作用：
    /// - 登录后提高业务流畅性（API请求无需重复密码）
    /// - 30分钟内自动刷新（无需重新输入密码）
    /// - 不是持久化认证凭证（不支持自动登录）
    /// </remarks>
    public class SecureTokenStorage : ITokenStorage
    {
        private readonly ILogger<SecureTokenStorage> _logger;
        
        // ⭐ 内存字段：Session级Token（应用关闭即失效）
        private LoginResponse? _sessionToken;

        public SecureTokenStorage(ILogger<SecureTokenStorage> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _logger.LogDebug("SecureTokenStorage 初始化（内存存储模式）");
        }

        /// <summary>
        /// 保存Token到内存（Session级别）
        /// </summary>
        /// <remarks>
        /// Token仅存储在进程内存中，应用退出后自动清除（包括：
        /// - 正常退出：用户点击关闭
        /// - 异常退出：应用崩溃、强制结束、断电
        /// 
        /// 操作系统保证：进程结束 → 内存自动回收 → Token自动清除
        /// </remarks>
        public async Task SaveTokenAsync(LoginResponse loginResponse)
        {
            if (loginResponse == null)
            {
                throw new ArgumentNullException(nameof(loginResponse));
            }

            _sessionToken = loginResponse;
            
            _logger.LogDebug("Token已保存到内存（Session级别，应用退出即失效）");
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 从内存加载Token
        /// </summary>
        /// <remarks>
        /// 仅在当前应用会话中有效。
        /// 应用重启后返回null（必须重新登录）。
        /// </remarks>
        public async Task<LoginResponse?> LoadTokenAsync()
        {
            if (_sessionToken != null)
            {
                _logger.LogDebug("从内存读取Token（Session有效）");
            }
            else
            {
                _logger.LogDebug("内存中无Token（需要登录）");
            }
            
            return await Task.FromResult(_sessionToken);
        }

        /// <summary>
        /// 清除内存中的Token
        /// </summary>
        /// <remarks>
        /// 用于：
        /// - 用户主动logout
        /// - 密码修改后强制重新登录
        /// - Token验证失败时清除
        /// 
        /// 注：应用退出时无需调用，进程结束会自动清除内存。
        /// </remarks>
        public async Task ClearTokenAsync()
        {
            _sessionToken = null;
            
            _logger.LogDebug("Token已从内存清除");
            
            await Task.CompletedTask;
        }
    }
}
