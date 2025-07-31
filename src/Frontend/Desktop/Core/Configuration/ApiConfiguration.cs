using System;

namespace LYBT.WPF.Client.Core.Configuration
{
    /// <summary>
    /// API客户端配置
    /// </summary>
    public static class ApiConfiguration
    {
        /// <summary>
        /// API基础地址
        /// </summary>
        public static string BaseUrl { get; set; } = "http://localhost:5299/";

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        public static int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 获取完整的API地址
        /// </summary>
        public static string GetApiUrl(string endpoint = "")
        {
            var baseUri = new Uri(BaseUrl);
            if (string.IsNullOrEmpty(endpoint))
                return baseUri.ToString();
            
            return new Uri(baseUri, endpoint).ToString();
        }

        /// <summary>
        /// 获取超时时间跨度
        /// </summary>
        public static TimeSpan GetTimeout() => TimeSpan.FromSeconds(TimeoutSeconds);
    }
}