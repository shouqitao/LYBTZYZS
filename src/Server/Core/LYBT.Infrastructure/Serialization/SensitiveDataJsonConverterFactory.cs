using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Entities.Attributes;
using SharedMasking = LYBT.Shared.Logging.Masking;

namespace LYBT.Infrastructure.Serialization;

/// <summary>
/// 敏感数据JSON转换器工厂
/// 为包含[SensitiveData]属性的类型创建自定义转换器
/// Issue #2254: API响应敏感数据脱敏
/// </summary>
public class SensitiveDataJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// 缓存类型是否包含敏感属性的检查结果
    /// </summary>
    private static readonly Dictionary<Type, bool> _hasSensitivePropertiesCache = new();
    private static readonly object _cacheLock = new();

    /// <summary>
    /// 判断是否可以转换指定类型
    /// </summary>
    public override bool CanConvert(Type typeToConvert)
    {
        // 排除基本类型、字符串、枚举、集合类型等
        if (typeToConvert.IsPrimitive ||
            typeToConvert == typeof(string) ||
            typeToConvert == typeof(decimal) ||
            typeToConvert == typeof(DateTime) ||
            typeToConvert == typeof(DateTimeOffset) ||
            typeToConvert == typeof(Guid) ||
            typeToConvert.IsEnum ||
            typeToConvert.IsArray ||
            (typeToConvert.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(typeToConvert.GetGenericTypeDefinition())))
        {
            return false;
        }

        // 检查类型是否有敏感数据属性
        return HasSensitiveProperties(typeToConvert);
    }

    /// <summary>
    /// 检查类型是否包含敏感数据属性
    /// </summary>
    private static bool HasSensitiveProperties(Type type)
    {
        lock (_cacheLock)
        {
            if (_hasSensitivePropertiesCache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            var hasSensitive = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(p => p.GetCustomAttribute<SensitiveDataAttribute>() != null);

            _hasSensitivePropertiesCache[type] = hasSensitive;
            return hasSensitive;
        }
    }

    /// <summary>
    /// 创建转换器实例
    /// </summary>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(SensitiveDataJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType, options);
    }
}

/// <summary>
/// 敏感数据JSON转换器
/// 在序列化时自动对敏感字段进行脱敏处理
/// </summary>
/// <typeparam name="T">要转换的类型</typeparam>
public class SensitiveDataJsonConverter<T> : JsonConverter<T> where T : class
{
    private readonly JsonSerializerOptions _originalOptions;

    public SensitiveDataJsonConverter(JsonSerializerOptions options)
    {
        // 创建不包含此转换器的选项副本，避免递归
        _originalOptions = new JsonSerializerOptions(options);
        var factoryToRemove = _originalOptions.Converters
            .FirstOrDefault(c => c is SensitiveDataJsonConverterFactory);
        if (factoryToRemove != null)
        {
            _originalOptions.Converters.Remove(factoryToRemove);
        }
    }

    /// <summary>
    /// 反序列化（不做处理，原样读取）
    /// </summary>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, _originalOptions);
    }

    /// <summary>
    /// 序列化时进行敏感数据脱敏
    /// </summary>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        writer.WriteStartObject();

        foreach (var property in properties)
        {
            // 检查是否应该忽略此属性
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
            {
                continue;
            }

            // 获取属性名（考虑JsonPropertyName特性）
            var jsonPropertyAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            var propertyName = jsonPropertyAttr?.Name ?? GetPropertyName(property.Name, options);

            try
            {
                var propValue = property.GetValue(value);

                // 检查是否为敏感数据
                var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>();
                if (sensitiveAttr != null && propValue is string strValue)
                {
                    // 对敏感字符串进行脱敏 - 转换枚举类型（两个命名空间的枚举值一致）
                    var maskedValue = SharedMasking.SensitiveDataMasker.Mask(
                        strValue,
                        (SharedMasking.MaskingMode)(int)sensitiveAttr.MaskingMode,
                        (SharedMasking.SensitiveDataType)(int)sensitiveAttr.DataType);
                    writer.WriteString(propertyName, maskedValue);
                }
                else if (propValue == null)
                {
                    // 根据配置决定是否写入null值
                    if (options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingNull)
                    {
                        writer.WriteNull(propertyName);
                    }
                }
                else
                {
                    // 非敏感属性使用原始选项序列化
                    writer.WritePropertyName(propertyName);
                    JsonSerializer.Serialize(writer, propValue, propValue.GetType(), _originalOptions);
                }
            }
            catch
            {
                // 属性读取失败时写入错误标记
                writer.WriteString(propertyName, "[读取失败]");
            }
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// 根据JsonSerializerOptions的命名策略转换属性名
    /// </summary>
    private static string GetPropertyName(string name, JsonSerializerOptions options)
    {
        return options.PropertyNamingPolicy?.ConvertName(name) ?? name;
    }
}
