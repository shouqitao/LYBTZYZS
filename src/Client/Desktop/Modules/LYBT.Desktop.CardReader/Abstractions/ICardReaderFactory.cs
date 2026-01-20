namespace LYBT.Desktop.CardReader.Abstractions;

/// <summary>
/// 读卡器工厂接口
/// 用于创建不同厂商的读卡器实例
/// </summary>
public interface ICardReaderFactory
{
    /// <summary>
    /// 获取所有支持的读卡器类型
    /// </summary>
    IReadOnlyList<CardReaderInfo> GetSupportedReaders();

    /// <summary>
    /// 创建指定类型的读卡器
    /// </summary>
    /// <param name="readerType">读卡器类型</param>
    /// <param name="options">配置选项</param>
    /// <returns>读卡器实例</returns>
    ICardReader CreateReader(CardReaderType readerType, CardReaderOptions? options = null);

    /// <summary>
    /// 自动检测并创建可用的读卡器
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <returns>可用的读卡器实例，如果没有检测到则返回null</returns>
    Task<ICardReader?> AutoDetectReaderAsync(CardReaderOptions? options = null);
}

/// <summary>
/// 读卡器类型枚举
/// </summary>
public enum CardReaderType
{
    /// <summary>自动检测</summary>
    Auto = 0,

    /// <summary>华大HD100</summary>
    HuaDaHD100 = 1,

    /// <summary>华大HD200（预留）</summary>
    HuaDaHD200 = 2,

    /// <summary>神思SS628（预留）</summary>
    ShenSiSS628 = 10,

    /// <summary>精伦IDR（预留）</summary>
    JingLunIDR = 20,

    /// <summary>新中新DKQ（预留）</summary>
    XinZhongXinDKQ = 30,

    /// <summary>模拟读卡器（测试用）</summary>
    Mock = 99
}

/// <summary>
/// 读卡器信息
/// </summary>
public record CardReaderInfo
{
    /// <summary>读卡器类型</summary>
    public CardReaderType Type { get; init; }

    /// <summary>显示名称</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>厂商</summary>
    public string Vendor { get; init; } = string.Empty;

    /// <summary>型号</summary>
    public string Model { get; init; } = string.Empty;

    /// <summary>说明</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>所需DLL文件列表</summary>
    public IReadOnlyList<string> RequiredDlls { get; init; } = [];

    /// <summary>是否可用（DLL存在）</summary>
    public bool IsAvailable { get; init; }
}
