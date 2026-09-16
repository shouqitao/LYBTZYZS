using SerilogLog = Serilog.Log;
using Velopack.Logging;

namespace LYBT.Desktop.Shell.Services.Startup;

/// <summary>
/// 把 Velopack 自身的日志接入应用 Serilog 管道。
/// </summary>
/// <remarks>
/// 更新失败在现场最难排查（安装器在应用外运行、异常不经过应用日志）。
/// 接入后「检查更新 / 下载 / 应用」各阶段的 Velopack 输出与业务日志同源，便于随用户日志收集排查。
/// （别名 <c>SerilogLog</c> 用于避开本类自身 <see cref="Log"/> 方法与静态 <c>Serilog.Log</c> 的重名。）
/// </remarks>
internal sealed class SerilogVelopackLogger : IVelopackLogger
{
    /// <inheritdoc />
    public void Log(VelopackLogLevel logLevel, string? message, Exception? exception = null)
    {
        switch (logLevel)
        {
            case VelopackLogLevel.Trace:
                SerilogLog.Verbose(exception, "[Velopack] {Message}", message);
                break;
            case VelopackLogLevel.Debug:
                SerilogLog.Debug(exception, "[Velopack] {Message}", message);
                break;
            case VelopackLogLevel.Information:
                SerilogLog.Information(exception, "[Velopack] {Message}", message);
                break;
            case VelopackLogLevel.Warning:
                SerilogLog.Warning(exception, "[Velopack] {Message}", message);
                break;
            case VelopackLogLevel.Error:
            case VelopackLogLevel.Critical:
                SerilogLog.Error(exception, "[Velopack] {Message}", message);
                break;
            default:
                SerilogLog.Verbose(exception, "[Velopack] {Message}", message);
                break;
        }
    }
}
