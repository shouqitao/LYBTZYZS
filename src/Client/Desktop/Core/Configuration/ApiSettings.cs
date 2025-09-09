using System.ComponentModel.DataAnnotations;

namespace LYBT.Desktop.Core.Configuration
{

    /// <summary>
    /// API设置配置模型
    /// </summary>
    public class ApiSettings
    {
        public const string SectionName = "ApiSettings";

        /// <summary>
        /// API基础地址
        /// </summary>
        [Required(ErrorMessage = "API基础地址不能为空")]
        [Url(ErrorMessage = "API基础地址格式不正确")]
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        [Range(5, 300, ErrorMessage = "请求超时时间必须在5-300秒之间")]
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 获取完整的API地址
        /// </summary>
        public string GetApiUrl(string endpoint = "")
        {
            var baseUri = new Uri(BaseUrl);
            if (string.IsNullOrEmpty(endpoint))
            {
                return baseUri.ToString();
            }

            return new Uri(baseUri, endpoint).ToString();
        }

        /// <summary>
        /// 获取超时时间跨度
        /// </summary>
        public TimeSpan GetTimeout() => TimeSpan.FromSeconds(TimeoutSeconds);
    }
}
