namespace LYBT.WPF.Client.Core.Configuration
{
    /// <summary>
    /// API设置配置模型
    /// </summary>
    public class ApiSettings
    {
        /// <summary>
        /// API基础地址
        /// </summary>
        public string BaseUrl { get; set; } = "https://localhost:7001/";

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 获取完整的API地址
        /// </summary>
        public string GetApiUrl(string endpoint = "")
        {
            var baseUri = new Uri(BaseUrl);
            if (string.IsNullOrEmpty(endpoint))
                return baseUri.ToString();
            
            return new Uri(baseUri, endpoint).ToString();
        }

        /// <summary>
        /// 获取超时时间跨度
        /// </summary>
        public TimeSpan GetTimeout() => TimeSpan.FromSeconds(TimeoutSeconds);
    }
}