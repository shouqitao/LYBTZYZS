using System.IO;
using LYBT.Desktop.CardReader.Abstractions;
using LYBT.Desktop.CardReader.Adapters;
using LYBT.Desktop.CardReader.Native;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.CardReader.Services;

/// <summary>
/// 读卡器工厂实现
/// 负责创建不同厂商的读卡器实例
/// </summary>
public class CardReaderFactory : ICardReaderFactory
{
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// 支持的读卡器列表
    /// </summary>
    private static readonly List<CardReaderInfo> _supportedReaders =
    [
        new CardReaderInfo
        {
            Type = CardReaderType.HuaDaHD100,
            DisplayName = "华大HD100",
            Vendor = "华大电子",
            Model = "HD100",
            Description = "USB接口身份证读卡器，支持居民身份证、港澳台居民居住证、外国人永久居留证",
            RequiredDlls = ["HDstdapi.dll"]
        },
        new CardReaderInfo
        {
            Type = CardReaderType.Mock,
            DisplayName = "模拟读卡器",
            Vendor = "LYBT",
            Model = "Mock-v1",
            Description = "用于开发测试的模拟读卡器，不需要真实硬件",
            RequiredDlls = []
        }
    ];

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="loggerFactory">日志工厂（可选）</param>
    public CardReaderFactory(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// 获取所有支持的读卡器类型
    /// </summary>
    public IReadOnlyList<CardReaderInfo> GetSupportedReaders()
    {
        return _supportedReaders
            .Select(r => r with { IsAvailable = CheckDllAvailability(r.RequiredDlls) })
            .ToList();
    }

    /// <summary>
    /// 创建指定类型的读卡器
    /// </summary>
    public ICardReader CreateReader(CardReaderType readerType, CardReaderOptions? options = null)
    {
        var opts = options ?? new CardReaderOptions();

        return readerType switch
        {
            CardReaderType.HuaDaHD100 => new HuaDaHD100CardReader(opts,
                _loggerFactory?.CreateLogger<HuaDaHD100CardReader>()),

            CardReaderType.Mock => new MockCardReader(opts),

            CardReaderType.Auto => throw new InvalidOperationException(
                "请使用 AutoDetectReaderAsync 方法进行自动检测"),

            _ => throw new NotSupportedException($"不支持的读卡器类型: {readerType}")
        };
    }

    /// <summary>
    /// 自动检测并创建可用的读卡器
    /// </summary>
    public async Task<ICardReader?> AutoDetectReaderAsync(CardReaderOptions? options = null)
    {
        var opts = options ?? new CardReaderOptions();

        // 按优先级尝试检测读卡器
        var detectOrder = new[]
        {
            CardReaderType.HuaDaHD100,
            // 未来添加其他读卡器类型
        };

        foreach (var readerType in detectOrder)
        {
            var readerInfo = _supportedReaders.FirstOrDefault(r => r.Type == readerType);
            if (readerInfo == null) continue;

            // 检查DLL是否存在
            if (!CheckDllAvailability(readerInfo.RequiredDlls))
                continue;

            // 尝试创建并连接
            var reader = CreateReader(readerType, opts);
            try
            {
                if (await reader.ConnectAsync())
                {
                    return reader;
                }
            }
            catch
            {
                reader.Dispose();
            }
        }

        // 如果没有检测到真实读卡器，在开发环境返回Mock
#if DEBUG
        return new MockCardReader(opts);
#else
        return null;
#endif
    }

    /// <summary>
    /// 检查DLL是否可用
    /// </summary>
    private static bool CheckDllAvailability(IReadOnlyList<string> requiredDlls)
    {
        if (requiredDlls.Count == 0) return true;

        foreach (var dll in requiredDlls)
        {
            // 检查应用程序目录
            var appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dll);
            if (File.Exists(appPath)) continue;

            // 检查Native子目录
            var nativePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Native", dll);
            if (File.Exists(nativePath)) continue;

            // 检查系统目录
            var sysPath = Path.Combine(Environment.SystemDirectory, dll);
            if (File.Exists(sysPath)) continue;

            return false;
        }

        return true;
    }
}
