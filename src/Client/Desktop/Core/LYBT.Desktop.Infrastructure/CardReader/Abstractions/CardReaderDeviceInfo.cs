namespace LYBT.Desktop.Infrastructure.CardReader.Abstractions;

/// <summary>
/// 读卡器设备信息（SHELL-019: 诊断面板设备详情）
/// </summary>
public sealed record CardReaderDeviceInfo(
    string Name,
    string Vendor,
    string Model,
    bool IsConnected,
    string FirmwareVersion);
