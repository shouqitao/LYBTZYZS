using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration
{
    /// <summary>
    /// 统一配置管理接口
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// 获取配置值
        /// </summary>
        T GetSection<T>(string sectionName) where T : class, new();

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        string GetConnectionString(string name = "DefaultConnection");

        /// <summary>
        /// 获取环境变量或配置值
        /// </summary>
        string GetValue(string key, string defaultValue = "");

        /// <summary>
        /// 验证配置
        /// </summary>
        ValidationResult ValidateConfiguration();

        /// <summary>
        /// 是否为开发环境
        /// </summary>
        bool IsDevelopment { get; }

        /// <summary>
        /// 是否为生产环境
        /// </summary>
        bool IsProduction { get; }

        /// <summary>
        /// 当前环境名称
        /// </summary>
        string Environment { get; }
    }
}
