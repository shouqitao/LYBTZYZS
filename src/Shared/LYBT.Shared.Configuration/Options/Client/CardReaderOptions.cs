using System.ComponentModel.DataAnnotations;
using System.IO;

namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// 读卡器配置选项
/// PRD-13: 支持从 appsettings.json 的 "CardReader" 节点读取配置
/// </summary>
public sealed class CardReaderOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "CardReader";

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    [Range(1000, 30000)]
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// 读卡超时时间（毫秒）
    /// </summary>
    [Range(1000, 60000)]
    public int ReadTimeout { get; set; } = 10000;

    /// <summary>
    /// 自动重连间隔（毫秒）
    /// </summary>
    [Range(500, 30000)]
    public int ReconnectInterval { get; set; } = 3000;

    /// <summary>
    /// 是否启用自动重连
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// 照片保存目录
    /// </summary>
    [Required]
    public string PhotoSaveDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "LYBT_CardPhotos");

    /// <summary>
    /// USB端口（华大读卡器默认1001）
    /// </summary>
    public int UsbPort { get; set; } = 1001;

    /// <summary>
    /// 串口端口号（1-16，0表示不使用串口）
    /// </summary>
    [Range(0, 16)]
    public int SerialPort { get; set; } = 0;
}
