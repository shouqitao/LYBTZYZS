using LYBT.Desktop.Infrastructure.CardReader.Abstractions;

namespace LYBT.Desktop.Infrastructure.CardReader.Models;

/// <summary>
/// 读卡器诊断报告（SHELL-019: 探测/握手/读卡/固件 4 步结果）
/// </summary>
public sealed class CardReaderDiagnosticReport
{
    /// <summary>目标厂家类型</summary>
    public CardReaderType ReaderType { get; init; }

    /// <summary>厂家显示名（如「华大HD100」/「自动检测」）</summary>
    public string ReaderTypeDisplay { get; init; } = string.Empty;

    /// <summary>设备探测是否通过（连接成功）</summary>
    public bool DeviceDetected { get; init; }

    /// <summary>通信握手是否通过（连接链路正常）</summary>
    public bool HandshakeOk { get; init; }

    /// <summary>固件版本（驱动未暴露时显示提示）</summary>
    public string FirmwareVersion { get; init; } = string.Empty;

    /// <summary>读卡测试结果（未放卡/读取成功/失败原因）</summary>
    public string ReadTestResult { get; init; } = string.Empty;

    /// <summary>是否全部通过</summary>
    public bool Passed => DeviceDetected && HandshakeOk;

    /// <summary>诊断过程消息（供面板逐行展示）</summary>
    public List<string> Messages { get; init; } = new();

    /// <summary>设备详情（连接成功后）</summary>
    public CardReaderDeviceInfo? DeviceInfo { get; init; }
}
