using System;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// Toast消息服务接口
    /// 提供轻量级、非阻塞的消息提示功能
    /// ADR-0003: 使用轻量Toast替代MessageBox和HandyControl Snackbar
    /// </summary>
    public interface IToastService
    {
        void ShowInfo(string message);
        void ShowSuccess(string message);
        void ShowWarning(string message);
        void ShowError(string message);
        void Show(string message, ToastType type, int durationMilliseconds = 3000);
    }

    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
