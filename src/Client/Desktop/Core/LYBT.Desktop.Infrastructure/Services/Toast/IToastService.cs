using System;

namespace LYBT.Desktop.Infrastructure.Services.Toast;

/// <summary>
/// Toast消息服务接口
/// 提供轻量级、非阻塞的消息提示功能
/// ADR-0003: 使用轻量Toast替代MessageBox和HandyControl Snackbar
/// </summary>
public interface IToastService
{
    /// <summary>
    /// 显示信息消息
    /// </summary>
    void ShowInfo(string message);

    /// <summary>
    /// 显示成功消息
    /// </summary>
    void ShowSuccess(string message);

    /// <summary>
    /// 显示警告消息
    /// </summary>
    void ShowWarning(string message);

    /// <summary>
    /// 显示错误消息
    /// </summary>
    void ShowError(string message);

    /// <summary>
    /// 显示自定义持续时间Toast
    /// </summary>
    void Show(string message, ToastType type, int durationMilliseconds = 3000);
}

/// <summary>
/// Toast消息类型
/// </summary>
public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}
