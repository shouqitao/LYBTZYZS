using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 键盘快捷键服务接口
    /// 管理全局键盘快捷键
    /// </summary>
    public interface IKeyboardShortcutService
    {
        /// <summary>
        /// 注册全局快捷键
        /// </summary>
        /// <param name="shortcut">快捷键组合</param>
        /// <param name="command">执行命令</param>
        void RegisterGlobalShortcut(string shortcut, ICommand command);

        /// <summary>
        /// 注册全局快捷键
        /// </summary>
        /// <param name="shortcut">快捷键组合</param>
        /// <param name="action">执行动作</param>
        void RegisterGlobalShortcut(string shortcut, Action action);

        /// <summary>
        /// 取消注册快捷键
        /// </summary>
        /// <param name="shortcut">快捷键组合</param>
        void UnregisterShortcut(string shortcut);

        /// <summary>
        /// 启用快捷键管理
        /// </summary>
        void EnableShortcuts();

        /// <summary>
        /// 禁用快捷键管理
        /// </summary>
        void DisableShortcuts();

        /// <summary>
        /// 获取所有注册的快捷键
        /// </summary>
        /// <returns>快捷键字典</returns>
        Dictionary<string, object> GetRegisteredShortcuts();
    }
}
