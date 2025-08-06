using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.WPF.Client.Infrastructure.JsonConverters;

namespace LYBT.WPF.Client.Infrastructure {
    /// <summary>
    /// Refit配置
    /// </summary>
    public static class RefitConfiguration {
        /// <summary>
        /// 获取Refit设置
        /// </summary>
        public static RefitSettings GetRefitSettings() {
            var options = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // 添加自定义转换器
            options.Converters.Add(new UserRoleJsonConverter());
            options.Converters.Add(new JsonStringEnumConverter());

            return new RefitSettings {
                ContentSerializer = new SystemTextJsonContentSerializer(options)
            };
        }
    }
}