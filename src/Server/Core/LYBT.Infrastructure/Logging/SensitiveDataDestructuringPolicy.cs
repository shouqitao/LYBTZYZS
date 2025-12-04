using System.Reflection;
using LYBT.Entities.Attributes;
using Serilog.Core;
using Serilog.Events;

namespace LYBT.Infrastructure.Logging;

/// <summary>
/// Serilog敏感数据脱敏策略
/// 自动对标记了[SensitiveData]的属性进行脱敏处理
/// </summary>
public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    /// <summary>
    /// 尝试解构对象，对敏感字段进行脱敏
    /// </summary>
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        result = null;

        if (value == null)
            return false;

        var type = value.GetType();

        // 只处理引用类型（排除基本类型和字符串）
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            return false;

        // 检查类型是否有敏感数据属性
        var hasSensitiveProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => p.GetCustomAttribute<SensitiveDataAttribute>() != null);

        if (!hasSensitiveProperties)
            return false;

        // 解构为脱敏后的属性集合
        var properties = new List<LogEventProperty>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            try
            {
                var propValue = property.GetValue(value);
                var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>();

                if (sensitiveAttr != null && sensitiveAttr.RequireLogMasking && propValue is string strValue)
                {
                    // 对敏感字符串进行脱敏
                    var maskedValue = SensitiveDataMasker.Mask(strValue, sensitiveAttr.MaskingMode, sensitiveAttr.DataType);
                    properties.Add(new LogEventProperty(property.Name, new ScalarValue(maskedValue)));
                }
                else if (propValue != null)
                {
                    // 非敏感属性保持原值
                    properties.Add(new LogEventProperty(property.Name, propertyValueFactory.CreatePropertyValue(propValue, true)));
                }
                else
                {
                    properties.Add(new LogEventProperty(property.Name, new ScalarValue(null)));
                }
            }
            catch
            {
                // 忽略无法读取的属性
                properties.Add(new LogEventProperty(property.Name, new ScalarValue("[读取失败]")));
            }
        }

        result = new StructureValue(properties, type.Name);
        return true;
    }
}
