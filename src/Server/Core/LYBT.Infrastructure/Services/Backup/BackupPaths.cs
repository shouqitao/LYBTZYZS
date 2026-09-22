using LYBT.Shared.Models.Primitives;

namespace LYBT.Infrastructure.Services.Backup;

/// <summary>
/// 备份目录默认值（B-06：备份文件存储在可配置目录；<c>Backup:Directory</c> 优先）。
/// </summary>
public static class BackupPaths
{
    /// <summary>备份子目录名</summary>
    public const string DirectoryName = "Backup";

    /// <summary>
    /// 桌面（本机）宿主默认备份目录：<c>%LOCALAPPDATA%\LYBT\Desktop\Backup</c>。
    /// 与 <c>SystemConstants.UserDataDirectory</c> 同源（<see cref="AppDataPaths.DesktopDataDirectory"/>），
    /// 必须位于安装目录之外——Velopack 更新/卸载会清理安装根目录。
    /// </summary>
    public static string DesktopDefaultDirectory => Path.Combine(AppDataPaths.DesktopDataDirectory, DirectoryName);

    /// <summary>独立服务宿主默认备份目录：<c>{应用基目录}/backup</c></summary>
    public static string HostDefaultDirectory => Path.Combine(AppContext.BaseDirectory, DirectoryName);
}
