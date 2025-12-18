namespace LYBT.Shared.Models.Contracts.Auth
{
    /// <summary>
    /// 刷新令牌请求
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// 要刷新的令牌
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// 设备ID（可选）
        /// </summary>
        public string? DeviceId { get; set; }
    }
}
