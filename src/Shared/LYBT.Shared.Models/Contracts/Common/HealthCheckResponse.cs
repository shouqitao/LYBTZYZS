using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 健康检查响应模型
    /// </summary>
    public class HealthCheckResponse
    {
        /// <summary>
        /// 健康状态
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Healthy";

        /// <summary>
        /// 时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// API版本（仅开发环境返回）
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        /// <summary>
        /// 运行环境（仅开发环境返回）
        /// </summary>
        [JsonPropertyName("environment")]
        public string? Environment { get; set; }
    }
}
