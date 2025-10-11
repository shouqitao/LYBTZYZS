namespace LYBT.Desktop.Foundation.Api.Managers
{
    /// <summary>
    /// 统一API客户端管理器接口 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则
    /// </summary>
    public interface IUnifiedApiClientManager
    {
        /// <summary>
        /// 设置授权令牌
        /// </summary>
        void SetAuthorizationToken(string? token);

        /// <summary>
        /// 获取授权令牌
        /// </summary>
        string? GetAuthorizationToken();

        /// <summary>
        /// 清除授权令牌
        /// </summary>
        void ClearAuthorizationToken();
    }
}
