using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refit;

namespace LYBT.Desktop.Infrastructure;

/// <summary>
/// Refit 配置管理器 - 企业级HTTP客户端配置
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 提供统一的JSON序列化配置和企业级HTTP客户端配置
/// 支持类型安全的REST API访问，适配小型诊所部署环境
/// </summary>
public static class RefitConfiguration
{

    /// <summary>
    /// 获取企业级Refit配置设置
    /// 配置JSON序列化、错误处理和性能优化策略
    /// </summary>
    /// <returns>配置完成的RefitSettings实例</returns>
    public static RefitSettings GetRefitSettings()
    {
        var jsonOptions = CreateJsonSerializerOptions();

        return new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions),
            HttpMessageHandlerFactory = () => new HttpClientHandler()
        };
    }

    /// <summary>
    /// 获取标准的Refit配置设置（用于UnifiedApiClientManager）
    /// 提供与UnifiedApiClientManager一致的配置
    /// </summary>
    /// <returns>配置完成的RefitSettings实例</returns>
    public static RefitSettings GetStandardRefitSettings()
    {
        var jsonOptions = CreateJsonSerializerOptions();

        return new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
        };
    }

    /// <summary>
    /// 创建企业级JSON序列化选项
    /// 统一的序列化配置，确保前后端数据传输一致性
    /// </summary>
    /// <returns>配置好的JsonSerializerOptions实例</returns>
    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            // 命名策略：与后端API保持一致（camelCase）
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,

            // 生产环境优化：紧凑格式，减少网络传输量
            WriteIndented = false,

            // 忽略空值，减少数据传输量
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            // JSON解析容错性配置
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,

            // 数字处理配置
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        // 添加标准转换器
        ConfigureJsonConverters(options);

        return options;
    }

    /// <summary>
    /// 配置JSON转换器
    /// 添加企业级自定义转换器和标准转换器
    /// </summary>
    /// <param name="options">JSON序列化选项</param>
    private static void ConfigureJsonConverters(JsonSerializerOptions options)
    {
        // 枚举转换器：使用字符串表示，便于调试和维护
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        // DateTime转换器：ISO 8601格式，确保时间准确性
        options.Converters.Add(new DateTimeConverter());

        // Guid转换器：标准格式，确保ID一致性
        options.Converters.Add(new GuidConverter());
    }
}

/// <summary>
/// 自定义DateTime转换器
/// 确保DateTime序列化的一致性和准确性
/// </summary>
public class DateTimeConverter : JsonConverter<DateTime>
{

    /// <summary>
    /// 反序列化DateTime
    /// </summary>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return DateTime.TryParse(stringValue, out var dateTime) ? dateTime : DateTime.MinValue;
    }

    /// <summary>
    /// 序列化DateTime为ISO 8601格式
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("O")); // ISO 8601格式
    }
}

/// <summary>
/// 自定义Guid转换器
/// 确保Guid序列化的一致性，使用标准格式
/// </summary>
public class GuidConverter : JsonConverter<Guid>
{

    /// <summary>
    /// 反序列化Guid
    /// </summary>
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return Guid.TryParse(stringValue, out var guid) ? guid : Guid.Empty;
    }

    /// <summary>
    /// 序列化Guid为标准字符串格式
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("D")); // 标准格式: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
    }
}
