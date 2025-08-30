using LYBT.Shared.Models.Contracts.Common;
using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LYBT.Desktop.Infrastructure
{
    /// <summary>
    /// Refit配置
    /// </summary>
    public static class RefitConfiguration
    {
        /// <summary>
        /// 获取Refit设置
        /// </summary>
        public static RefitSettings GetRefitSettings()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // 添加自定义转换器
            // UserRoleJsonConverter removed
            options.Converters.Add(new JsonStringEnumConverter());

            return new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(options)
            };
        }
    }
}