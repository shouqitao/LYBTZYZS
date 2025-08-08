namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// Token管理器接口
    /// </summary>
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