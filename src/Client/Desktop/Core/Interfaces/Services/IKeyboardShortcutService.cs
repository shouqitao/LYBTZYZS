using System;
using System.Collections.Generic;
using System.Windows.Input;
using LYBT.Desktop.Core.Services;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 键盘快捷键服务接口 - P7-04 UltraThink用户体验优化
    /// </summary>
    public interface IKeyboardShortcutService
    {
        /// <summary>注册全局快捷键</summary>
        bool RegisterGlobalShortcut(KeyGesture keyGesture, Action action, string description = "");

        /// <summary>注册上下文快捷键</summary>
        bool RegisterContextShortcut(string context, KeyGesture keyGesture, Action action, string description = "");

        /// <summary>移除全局快捷键</summary>
        bool UnregisterGlobalShortcut(KeyGesture keyGesture);

        /// <summary>移除上下文快捷键</summary>
        bool UnregisterContextShortcut(string context, KeyGesture keyGesture);

        /// <summary>设置当前上下文</summary>
        void SetCurrentContext(string context);

        /// <summary>获取当前上下文</summary>
        string GetCurrentContext();

        /// <summary>获取所有注册的快捷键</summary>
        IEnumerable<ShortcutInfo> GetAllShortcuts();

        /// <summary>执行快捷键</summary>
        bool ExecuteShortcut(KeyGesture keyGesture);
    }
}
