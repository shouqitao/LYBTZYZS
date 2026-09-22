namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 首次运行状态（B-07：首次运行检测 + 完成标记）。
/// </summary>
/// <remarks>
/// 标记文件沿用既有路径 <c>%LOCALAPPDATA%\LYBT\Desktop\first_run_done.flag</c>
/// （<c>AppDataPaths.DesktopDataDirectory</c>），既有安装的「已完成」状态继续有效。
/// 检测方（Shell 登录成功协调器）与写入方（初始化向导）共用本服务，避免两处各自拼路径。
/// </remarks>
public interface IFirstRunStateService
{
    /// <summary>标记文件路径</summary>
    string MarkerPath { get; }

    /// <summary>是否为首次运行（标记文件不存在）</summary>
    bool IsFirstRun { get; }

    /// <summary>标记首次运行已完成（写入标记文件）</summary>
    void MarkCompleted();

    /// <summary>清除标记（供「重新运行初始化向导」场景重置状态）</summary>
    void Reset();
}
