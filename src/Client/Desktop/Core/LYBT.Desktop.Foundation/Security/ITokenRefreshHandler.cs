namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token刷新处理器接口
    /// </summary>
    public interface ITokenRefreshHandler
    {
        /// <summary>
        /// 主动刷新Token
        /// </summary>
        Task<TokenRefreshResult> RefreshTokenAsync();
    }
}
