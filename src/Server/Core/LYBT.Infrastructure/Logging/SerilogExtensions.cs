using Serilog;

namespace LYBT.Infrastructure.Logging;

/// <summary>
/// Serilog配置扩展方法
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// 添加敏感数据脱敏策略
    /// </summary>
    /// <param name="configuration">Serilog配置</param>
    /// <returns>配置后的LoggerConfiguration</returns>
    public static LoggerConfiguration WithSensitiveDataMasking(this LoggerConfiguration configuration)
    {
        return configuration.Destructure.With<SensitiveDataDestructuringPolicy>();
    }
}
