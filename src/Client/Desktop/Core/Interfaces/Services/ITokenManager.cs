using System;

namespace LYBT.Desktop.Core.Interfaces.Services
{

    /// <summary>
    /// Token管理器接口
    /// </summary>
    [Obsolete("ITokenManager已废弃，请使用ISessionManager.AuthToken来获取Token。此接口将在下个版本中移除。", false)]
    public interface ITokenManager
    {

        /// <summary>
        /// 获取当前Token
        /// </summary>
        string? GetToken();

        /// <summary>
        /// 设置Token
        /// </summary>
        void SetToken(string token);

        /// <summary>
        /// 清除Token
        /// </summary>
        void ClearToken();
    }
}
