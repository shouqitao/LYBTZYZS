using System.ComponentModel;

namespace LYBT.UI.PrismWpf.Models
{
    /// <summary>
    /// API配置
    /// </summary>
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = "https://localhost:5001";
        public int Timeout { get; set; } = 30;
    }

    /// <summary>
    /// 应用程序配置
    /// </summary>
    public class AppSettings
    {
        public string AppName { get; set; } = "LYBT中医诊所管理系统";
        public string Version { get; set; } = "1.0.0";
        public string Theme { get; set; } = "Light";
    }

    /// <summary>
    /// 应用程序配置根类
    /// </summary>
    public class AppConfiguration
    {
        public ApiSettings ApiSettings { get; set; } = new();
        public AppSettings AppSettings { get; set; } = new();
    }
}