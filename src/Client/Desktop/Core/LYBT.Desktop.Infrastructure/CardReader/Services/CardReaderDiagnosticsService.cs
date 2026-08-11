using LYBT.Desktop.Infrastructure.CardReader.Abstractions;
using LYBT.Desktop.Infrastructure.CardReader.Models;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.CardReader.Services;

/// <summary>
/// 读卡器诊断服务（SHELL-019: 测试模式编排——厂家选择 → 探测 → 握手 → 读卡测试 → 固件）
/// 使用模式（医生端）仍走 ICardReaderFactory.AutoDetectReaderAsync，诊断不触碰医生会话。
/// </summary>
public sealed class CardReaderDiagnosticsService : ICardReaderDiagnostics
{
    private readonly ICardReaderFactory _factory;
    private readonly ILogger<CardReaderDiagnosticsService> _logger;

    public CardReaderDiagnosticsService(
        ICardReaderFactory factory,
        ILogger<CardReaderDiagnosticsService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CardReaderDiagnosticReport> RunDiagnosticsAsync(
        CardReaderType readerType,
        CardReaderOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        ICardReader? reader = null;

        try
        {
            var type = readerType == CardReaderType.Auto ? CardReaderType.HuaDaHD100 : readerType;
            var typeName = readerType == CardReaderType.Auto ? "自动检测（当前适配: 华大HD100）" : type.ToString();
            messages.Add($"厂家: {typeName}");
            messages.Add($"参数: UsbPort={options?.UsbPort ?? 1001} · ConnectTimeout={options?.ConnectTimeout ?? 5000}ms · ReadTimeout={options?.ReadTimeout ?? 10000}ms");

            reader = _factory.CreateReader(type, options);
            if (reader is null)
                return FailReport(readerType, typeName, messages, "创建读卡器实例失败（厂家类型不支持）");

            // 1) 设备探测：连接成功 = 设备在线
            messages.Add("步骤 1/4 · 设备探测（连接）...");
            var connected = await reader.ConnectAsync().WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            if (!connected)
                return FailReport(readerType, typeName, messages, "设备探测失败——未检测到设备（检查 USB 连接与端口）");

            messages.Add("步骤 1/4 · 设备探测通过 ✔");
            var deviceInfo = reader.GetDeviceInfo();
            messages.Add($"设备: {deviceInfo.Vendor} {deviceInfo.Model}（{deviceInfo.Name}）");

            // 2) 通信握手：连接链路已建立 = 握手通过（华大驱动 HD_InitComm 返回 0 即链路 OK）
            messages.Add("步骤 2/4 · 通信握手（USB 链路）...");
            messages.Add("步骤 2/4 · 握手通过 ✔（HD_InitComm 链路正常）");

            // 3) 固件版本：华大驱动未暴露独立固件查询 API
            messages.Add($"步骤 3/4 · 固件版本: {deviceInfo.FirmwareVersion}");

            // 4) 读卡测试：检测卡片 → 读取
            messages.Add("步骤 4/4 · 读卡测试（请将样卡放至感应区）...");
            var hasCard = await reader.DetectCardAsync().WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            string readResult;
            if (!hasCard)
            {
                readResult = "未检测到卡片——请放置样卡后重试";
                messages.Add($"步骤 4/4 · {readResult}");
            }
            else
            {
                var card = await reader.ReadCardAsync(cancellationToken: cancellationToken);
                readResult = string.IsNullOrWhiteSpace(card.Name)
                    ? "已检测到卡片，但未能解析身份信息（样卡需为有效身份证）"
                    : $"读卡成功: {card.Name} · {card.IdNumber} · {card.Gender}";
                messages.Add($"步骤 4/4 · {readResult}");
            }

            messages.Add($"诊断完成: {(connected ? "设备链路正常" : "失败")}");
            return new CardReaderDiagnosticReport
            {
                ReaderType = readerType,
                ReaderTypeDisplay = typeName,
                DeviceDetected = connected,
                HandshakeOk = connected,
                FirmwareVersion = deviceInfo.FirmwareVersion,
                ReadTestResult = readResult,
                DeviceInfo = deviceInfo,
                Messages = messages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CARD-DIAG] 读卡器诊断失败: {Type}", readerType);
            messages.Add($"诊断异常: {ex.Message}");
            return FailReport(readerType, readerType.ToString(), messages, ex.Message);
        }
        finally
        {
            try
            {
                if (reader is not null)
                    await reader.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CARD-DIAG] 断开读卡器失败（忽略）");
            }
        }
    }

    private static CardReaderDiagnosticReport FailReport(
        CardReaderType readerType, string typeDisplay, List<string> messages, string error)
    {
        messages.Add($"✘ {error}");
        return new CardReaderDiagnosticReport
        {
            ReaderType = readerType,
            ReaderTypeDisplay = typeDisplay,
            DeviceDetected = false,
            HandshakeOk = false,
            ReadTestResult = error,
            Messages = messages
        };
    }
}
