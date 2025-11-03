using System.Collections.Concurrent;
using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 键盘快捷键服务实现
    /// 管理全局键盘快捷键
    /// </summary>
    public class KeyboardShortcutService : IKeyboardShortcutService
    {
        private readonly ILogger<KeyboardShortcutService> _logger;
        private readonly ConcurrentDictionary<string, object> _registeredShortcuts;

        public KeyboardShortcutService(ILogger<KeyboardShortcutService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registeredShortcuts = new ConcurrentDictionary<string, object>();
        }

        /// <summary>
        /// 注册全局快捷键
        /// </summary>
        /// <param name="shortcut">快捷键组合</param>
        /// <param name="command">执行命令</param>
        public void RegisterGlobalShortcut(string shortcut, ICommand command)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
                throw new ArgumentException("快捷键不能为空", nameof(shortcut));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            try
            {
                _registeredShortcuts.AddOrUpdate(shortcut, command, (key, oldValue) => command);
                _logger.LogDebug("注册快捷键命令: {Shortcut}", shortcut);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册快捷键命令失败: {Shortcut}", shortcut);
                throw;
            }
        }

        /// <summary>
        /// 注册全局快捷键
        /// </summary>
        /// <param name="shortcut">快捷键组合</param>
        /// <param name="action">执行动作</param>
        public void RegisterGlobalShortcut(string shortcut, Action action)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
                throw new ArgumentException("快捷键不能为空", nameof(shortcut));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                _registeredShortcuts.AddOrUpdate(shortcut, action, (key, oldValue) => action);
                _logger.LogDebug("注册快捷键动作: {Shortcut}", shortcut);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册快捷键动作失败: {Shortcut}", shortcut);
                throw;
            }
        }

        /// <summary>
        /// 取消注册快捷键
        /// </summary>
        /// <param name="shortcut">快捷键组合</param>
        public void UnregisterShortcut(string shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
                return;

            try
            {
                if (_registeredShortcuts.TryRemove(shortcut, out _))
                {
                    _logger.LogDebug("取消注册快捷键: {Shortcut}", shortcut);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消注册快捷键失败: {Shortcut}", shortcut);
            }
        }

        /// <summary>
        /// 启用快捷键管理
        /// </summary>
        public void EnableShortcuts()
        {
            try
            {
                _logger.LogInformation("启用快捷键管理");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用快捷键管理失败");
            }
        }

        /// <summary>
        /// 禁用快捷键管理
        /// </summary>
        public void DisableShortcuts()
        {
            try
            {
                _logger.LogInformation("禁用快捷键管理");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用快捷键管理失败");
            }
        }

        /// <summary>
        /// 获取所有注册的快捷键
        /// </summary>
        /// <returns>快捷键字典</returns>
        public Dictionary<string, object> GetRegisteredShortcuts()
        {
            try
            {
                return new Dictionary<string, object>(_registeredShortcuts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取注册的快捷键失败");
                return new Dictionary<string, object>();
            }
        }

    }
}
