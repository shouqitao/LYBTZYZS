using System;
using System.Runtime.InteropServices;

namespace LYBT.Desktop.Shell;

/// <summary>
/// Windows API P/Invoke 封装
/// 用于单实例模式下激活已存在的窗口
/// </summary>
internal static class NativeMethods
{
    /// <summary>
    /// 窗口最小化状态恢复
    /// </summary>
    private const int SW_RESTORE = 9;

    /// <summary>
    /// 显示窗口
    /// </summary>
    private const int SW_SHOW = 5;

    /// <summary>
    /// 根据类名和窗口标题查找窗口
    /// </summary>
    /// <param name="lpClassName">窗口类名，可为null</param>
    /// <param name="lpWindowName">窗口标题</param>
    /// <returns>窗口句柄，未找到返回IntPtr.Zero</returns>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    /// <summary>
    /// 将窗口置于前台
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <returns>操作是否成功</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// 控制窗口显示状态
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <param name="nCmdShow">显示命令</param>
    /// <returns>操作前窗口是否可见</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// 检查窗口是否最小化
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <returns>窗口是否最小化</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);

    /// <summary>
    /// 激活已存在的应用程序窗口
    /// </summary>
    /// <param name="windowTitle">窗口标题</param>
    /// <returns>是否成功激活</returns>
    public static bool ActivateExistingWindow(string windowTitle)
    {
        var hWnd = FindWindow(null, windowTitle);
        if (hWnd == IntPtr.Zero)
            return false;

        // 如果窗口最小化，先恢复
        if (IsIconic(hWnd))
            ShowWindow(hWnd, SW_RESTORE);
        else
            ShowWindow(hWnd, SW_SHOW);

        return SetForegroundWindow(hWnd);
    }
}
