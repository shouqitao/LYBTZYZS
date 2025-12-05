using System.ComponentModel.DataAnnotations;
using LYBT.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Configuration.Extensions;

/// <summary>
/// 配置注册扩展方法
/// 简化统一配置选项的注册和验证
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// 注册凌隐宝堂系统统一配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLybtConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册统一配置
        services.Configure<LybtOptions>(configuration.GetSection(LybtOptions.SectionName));

        // 添加配置验证
        services.AddConfigurationValidation<LybtOptions>();

        return services;
    }

    /// <summary>
    /// 获取凌隐宝堂配置选项
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <returns>配置选项</returns>
    public static LybtOptions GetLybtOptions(this IConfiguration configuration)
    {
        var options = new LybtOptions();
        configuration.GetSection(LybtOptions.SectionName).Bind(options);
        return options;
    }

    /// <summary>
    /// 添加配置验证
    /// </summary>
    private static IServiceCollection AddConfigurationValidation<TOptions>(this IServiceCollection services)
        where TOptions : class
    {
        services.AddSingleton<IValidateOptions<TOptions>, ConfigurationValidator<TOptions>>();
        return services;
    }
}

/// <summary>
/// 配置验证器
/// </summary>
/// <typeparam name="TOptions">配置选项类型</typeparam>
public class ConfigurationValidator<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        var context = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(options, context, validationResults, true);

        if (isValid)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
        return ValidateOptionsResult.Fail(errors);
    }
}
