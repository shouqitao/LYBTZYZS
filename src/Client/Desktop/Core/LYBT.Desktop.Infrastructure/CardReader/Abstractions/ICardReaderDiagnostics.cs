using LYBT.Desktop.Infrastructure.CardReader.Models;
using LYBT.Shared.Configuration.Options.Client;

namespace LYBT.Desktop.Infrastructure.CardReader.Abstractions;

/// <summary>
/// 读卡器诊断接口（SHELL-019: sysadmin 测试模式——厂家选择/探测/读卡/握手/固件）
/// </summary>
public interface ICardReaderDiagnostics
{
    /// <summary>
    /// 运行完整诊断（探测 + 握手 + 读卡测试 + 固件查询）
    /// </summary>
    /// <param name="readerType">目标厂家类型（测试时临时切换）</param>
    /// <param name="options">手动参数覆盖（UsbPort/ConnectTimeout/ReadTimeout——仅自动检测失败时使用）</param>
    Task<CardReaderDiagnosticReport> RunDiagnosticsAsync(
        CardReaderType readerType,
        CardReaderOptions? options = null,
        CancellationToken cancellationToken = default);
}
