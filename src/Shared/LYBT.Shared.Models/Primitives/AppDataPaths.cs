namespace LYBT.Shared.Models.Primitives;

/// <summary>
/// 桌面客户端本地数据目录（Server 与 Desktop 共享的唯一权威定义）。
/// </summary>
/// <remarks>
/// <para><b>必须位于应用安装目录之外</b>：经 Velopack 安装时安装根为
/// <c>%LOCALAPPDATA%\{packId}</c>（即 <c>%LOCALAPPDATA%\LYBTZYZS</c>），更新/卸载会管理该目录内容——
/// 把用户设置或备份放在其中会在更新或卸载时丢失。</para>
/// <para>本目录是凭据、照片、系统设置、首次运行标记、备份的既有约定目录；
/// 桌面侧 <c>SystemConstants.UserDataDirectory</c> 与备份引擎的默认备份目录均派生自本定义。</para>
/// </remarks>
public static class AppDataPaths
{
    /// <summary>桌面客户端用户数据根目录：<c>%LOCALAPPDATA%\LYBT\Desktop</c></summary>
    public static string DesktopDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LYBT",
        "Desktop");
}
