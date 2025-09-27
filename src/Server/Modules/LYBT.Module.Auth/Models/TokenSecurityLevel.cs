namespace LYBT.Module.Auth.Models
{
    /// <summary>
    /// Token安全级别枚举
    /// </summary>
    public enum TokenSecurityLevel
    {
        /// <summary>
        /// 低安全级别
        /// </summary>
        Low = 0,

        /// <summary>
        /// 中等安全级别
        /// </summary>
        Medium = 1,

        /// <summary>
        /// 高安全级别
        /// </summary>
        High = 2,

        /// <summary>
        /// 极高安全级别
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// Token安全验证结果
    /// </summary>
    public class TokenSecurityValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 安全级别
        /// </summary>
        public TokenSecurityLevel SecurityLevel { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 警告消息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 原因列表
        /// </summary>
        public List<string> Reasons { get; set; } = new List<string>();

        /// <summary>
        /// 是否需要额外验证
        /// </summary>
        public bool RequiresAdditionalVerification { get; set; }

        /// <summary>
        /// 验证时间
        /// </summary>
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    }
}