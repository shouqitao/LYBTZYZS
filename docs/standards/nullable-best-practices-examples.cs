// Nullable 最佳实践示例 - 新代码零 CS86xx 标准
// 本文件展示如何编写符合 Nullable 治理要求的代码
// 🎯 目标: 新代码零 CS8618/CS8625/CS8622 警告

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Standards.Examples
{
    /// <summary>
    /// ✅ 正确示例: 完善的可空性处理
    /// </summary>
    public class CorrectNullableExample
    {
        // ✅ 正确: 不可空字段在构造函数中初始化
        private readonly ILogger<CorrectNullableExample> _logger;
        
        // ✅ 正确: 明确标记可空字段
        private readonly IOptionalService? _optionalService;
        
        // ✅ 正确: 可空属性明确标记
        public string? OptionalData { get; set; }
        
        // ✅ 正确: 不可空属性保证非空
        public string RequiredData { get; set; } = string.Empty;

        /// <summary>
        /// ✅ 正确: 构造函数确保所有不可空字段被初始化
        /// </summary>
        public CorrectNullableExample(
            ILogger<CorrectNullableExample> logger, 
            IOptionalService? optionalService = null)
        {
            // ✅ 正确: 参数验证防止 null
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _optionalService = optionalService;
        }

        /// <summary>
        /// ✅ 正确: 方法参数和返回值明确可空性
        /// </summary>
        public async Task<ProcessResult?> ProcessDataAsync(string? input, CancellationToken cancellationToken = default)
        {
            // ✅ 正确: 处理可能的 null 输入
            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("处理数据时输入为空");
                return null;
            }

            try
            {
                // ✅ 正确: 安全调用可选依赖
                var result = _optionalService?.ProcessAsync(input, cancellationToken);
                
                // ✅ 正确: 处理可能的 null 返回值
                return await (result ?? Task.FromResult<ProcessResult?>(null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理数据时发生错误: {Input}", input);
                throw;
            }
        }

        /// <summary>
        /// ✅ 正确: 集合处理的可空性最佳实践
        /// </summary>
        public IEnumerable<string> GetValidItems(IEnumerable<string?>? items)
        {
            // ✅ 正确: 防御性编程处理可能的 null 集合
            if (items == null)
            {
                yield break;
            }

            foreach (var item in items)
            {
                // ✅ 正确: 过滤 null 项目
                if (!string.IsNullOrEmpty(item))
                {
                    yield return item;
                }
            }
        }
    }

    /// <summary>
    /// ❌ 错误示例: 会产生 CS86xx 警告的代码模式
    /// 新代码中禁止使用这些模式
    /// </summary>
    public class IncorrectNullableExample
    {
        // ❌ 错误: CS8618 - 不可空字段未初始化
        // private readonly ILogger<IncorrectNullableExample> _logger;
        
        // ❌ 错误: CS8618 - 不可空属性未初始化  
        // public string RequiredData { get; set; }

        // ❌ 错误示例构造函数
        // public IncorrectNullableExample(ILogger<IncorrectNullableExample> logger)
        // {
        //     // ❌ 错误: CS8625 - 将 null 赋值给不可空字段
        //     // _logger = null;
        // }

        // ❌ 错误: CS8625 - 不安全的 null 赋值
        // public void SetData(string data)
        // {
        //     RequiredData = null; // CS8625 警告
        // }

        // ❌ 错误: CS8622 - 委托可空性不匹配
        // public void Subscribe(Action<string> callback)
        // {
        //     Action<string?> nullableCallback = callback; // CS8622 警告
        // }
    }

    /// <summary>
    /// 🎯 升级现有代码的迁移模式
    /// </summary>
    public class MigrationPatterns
    {
        /// <summary>
        /// 模式1: 渐进式可空性迁移
        /// </summary>
        public class LegacyToNullable
        {
            // 阶段1: 保持现有行为，添加可空标记
            public string? LegacyProperty { get; set; }
            
            // 阶段2: 为新功能强制非空
            public string NewRequiredProperty { get; set; } = string.Empty;
            
            // 阶段3: 提供迁移助手方法
            public bool HasLegacyData => !string.IsNullOrEmpty(LegacyProperty);
        }

        /// <summary>
        /// 模式2: 安全的 null 检查模式
        /// </summary>
        public static class SafeNullChecks
        {
            public static bool IsNullOrEmpty([NotNullWhen(false)] string? value)
            {
                return string.IsNullOrEmpty(value);
            }

            public static T ThrowIfNull<T>([NotNull] T? value, string paramName = "")
                where T : class
            {
                return value ?? throw new ArgumentNullException(paramName);
            }
        }
    }

    // 支持接口定义
    public interface IOptionalService
    {
        Task<ProcessResult?> ProcessAsync(string input, CancellationToken cancellationToken);
    }

    public interface IProcessResult { }
    public class ProcessResult : IProcessResult { }
}

// 注解支持
namespace System.Diagnostics.CodeAnalysis
{
    public class NotNullAttribute : Attribute { }
    public class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue) { }
    }
}