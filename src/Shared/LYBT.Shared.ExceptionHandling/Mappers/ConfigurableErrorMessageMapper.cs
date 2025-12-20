using LYBT.Shared.Primitives.ErrorCodes;
using Microsoft.Extensions.Configuration;

namespace LYBT.Shared.ExceptionHandling.Mappers;

/// <summary>
/// 可配置的错误消息映射器
/// consolidate-exception-handling: 从LYBT.Infrastructure迁移
/// 从IConfiguration读取ErrorMessages配置，支持运行时覆盖默认消息
/// </summary>
public class ConfigurableErrorMessageMapper : IErrorMessageMapper
{
    private readonly IConfiguration _configuration;

    public ConfigurableErrorMessageMapper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public string GetUserMessage(ErrorCode errorCode)
    {
        // 优先从配置读取
        var configMessage = GetConfiguredMessage(errorCode, "UserMessage");
        if (!string.IsNullOrEmpty(configMessage))
        {
            return configMessage;
        }

        // 回退到默认消息
        return ErrorMessages.Get(errorCode);
    }

    /// <inheritdoc/>
    public string GetTechnicalMessage(ErrorCode errorCode)
    {
        // 优先从配置读取
        var configMessage = GetConfiguredMessage(errorCode, "TechnicalMessage");
        if (!string.IsNullOrEmpty(configMessage))
        {
            return configMessage;
        }

        // 回退到英文技术消息
        return ErrorMessages.GetEnglish(errorCode);
    }

    /// <inheritdoc/>
    public string GetUserMessage(ErrorCode errorCode, params object[] args)
    {
        var template = GetUserMessage(errorCode);
        try
        {
            return args.Length > 0 ? string.Format(template, args) : template;
        }
        catch (FormatException)
        {
            // 格式化失败时返回原始模板
            return template;
        }
    }

    /// <summary>
    /// 从配置读取错误消息
    /// </summary>
    private string? GetConfiguredMessage(ErrorCode errorCode, string messageType)
    {
        var errorCodeString = errorCode.ToFormattedString();
        var section = _configuration.GetSection($"Lybt:ErrorMessages:{errorCodeString}:{messageType}");
        return section.Value;
    }
}
