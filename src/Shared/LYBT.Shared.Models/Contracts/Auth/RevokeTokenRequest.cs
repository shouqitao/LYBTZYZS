namespace LYBT.Shared.Models.Contracts.Auth
{
    /// <summary>
    /// 撤销令牌请求
    /// </summary>
    public class RevokeTokenRequest
    {
        /// <summary>
        /// 要撤销的刷新令牌
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// 撤销原因
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 是否撤销所有设备的令牌
        /// </summary>
        public bool RevokeAll { get; set; }
    }
}
