using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 键盘快捷键服务 - P7-04 UltraThink用户体验优化
    /// 
    /// 功能特性：
    /// 1. 全局键盘快捷键注册与管理
    /// 2. 上下文相关快捷键支持
    /// 3. 快捷键冲突检测与解决
    /// 4. 用户自定义快捷键支持
    /// </summary>
    public class KeyboardShortcutService : IKeyboardShortcutService
    {
        #region 私有字段

        private readonly ILogger<KeyboardShortcutService> _logger;
        private readonly Dictionary<KeyGesture, ShortcutAction> _globalShortcuts;
        private readonly Dictionary<string, Dictionary<KeyGesture, ShortcutAction>> _contextShortcuts;
        private string _currentContext = "Default";

        #endregion

        #region 构造函数

        public KeyboardShortcutService(ILogger<KeyboardShortcutService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _globalShortcuts = new Dictionary<KeyGesture, ShortcutAction>();
            _contextShortcuts = new Dictionary<string, Dictionary<KeyGesture, ShortcutAction>>();

            RegisterDefaultShortcuts();
            RegisterGlobalKeyHandler();
        }

        #endregion

        #region IKeyboardShortcutService 实现

        /// <summary>注册全局快捷键</summary>
        public bool RegisterGlobalShortcut(KeyGesture keyGesture, Action action, string description = "")
        {
            if (keyGesture == null || action == null)
            {
                return false;
            }

            if (_globalShortcuts.ContainsKey(keyGesture))
            {
                _logger.LogWarning("全局快捷键 {Gesture} 已存在，将被覆盖", keyGesture.DisplayString);
            }

            _globalShortcuts[keyGesture] = new ShortcutAction(action, description);
            _logger.LogInformation("注册全局快捷键: {Gesture} - {Description}", keyGesture.DisplayString, description);
            return true;
        }

        /// <summary>注册上下文快捷键</summary>
        public bool RegisterContextShortcut(string context, KeyGesture keyGesture, Action action, string description = "")
        {
            if (string.IsNullOrEmpty(context) || keyGesture == null || action == null)
            {
                return false;
            }

            if (!_contextShortcuts.ContainsKey(context))
            {
                _contextShortcuts[context] = new Dictionary<KeyGesture, ShortcutAction>();
            }

            var contextShortcuts = _contextShortcuts[context];
            if (contextShortcuts.ContainsKey(keyGesture))
            {
                _logger.LogWarning("上下文 {Context} 中的快捷键 {Gesture} 已存在，将被覆盖", context, keyGesture.DisplayString);
            }

            contextShortcuts[keyGesture] = new ShortcutAction(action, description);
            _logger.LogInformation("注册上下文快捷键: {Context}.{Gesture} - {Description}", context, keyGesture.DisplayString, description);
            return true;
        }

        /// <summary>移除全局快捷键</summary>
        public bool UnregisterGlobalShortcut(KeyGesture keyGesture)
        {
            if (keyGesture != null && _globalShortcuts.Remove(keyGesture))
            {
                _logger.LogInformation("移除全局快捷键: {Gesture}", keyGesture.DisplayString);
                return true;
            }
            return false;
        }

        /// <summary>移除上下文快捷键</summary>
        public bool UnregisterContextShortcut(string context, KeyGesture keyGesture)
        {
            if (string.IsNullOrEmpty(context) || keyGesture == null)
            {
                return false;
            }

            if (_contextShortcuts.TryGetValue(context, out var contextShortcuts) &&
                contextShortcuts.Remove(keyGesture))
            {
                _logger.LogInformation("移除上下文快捷键: {Context}.{Gesture}", context, keyGesture.DisplayString);
                return true;
            }
            return false;
        }

        /// <summary>设置当前上下文</summary>
        public void SetCurrentContext(string context)
        {
            if (!string.IsNullOrEmpty(context) && _currentContext != context)
            {
                _currentContext = context;
                _logger.LogDebug("切换快捷键上下文: {Context}", context);
            }
        }

        /// <summary>获取当前上下文</summary>
        public string GetCurrentContext()
        {
            return _currentContext;
        }

        /// <summary>获取所有注册的快捷键</summary>
        public IEnumerable<ShortcutInfo> GetAllShortcuts()
        {
            var shortcuts = new List<ShortcutInfo>();

            // 添加全局快捷键
            foreach (var kvp in _globalShortcuts)
            {
                shortcuts.Add(new ShortcutInfo
                {
                    Context = "Global",
                    KeyGesture = kvp.Key,
                    Description = kvp.Value.Description
                });
            }

            // 添加上下文快捷键
            foreach (var contextKvp in _contextShortcuts)
            {
                foreach (var shortcutKvp in contextKvp.Value)
                {
                    shortcuts.Add(new ShortcutInfo
                    {
                        Context = contextKvp.Key,
                        KeyGesture = shortcutKvp.Key,
                        Description = shortcutKvp.Value.Description
                    });
                }
            }

            return shortcuts;
        }

        /// <summary>执行快捷键</summary>
        public bool ExecuteShortcut(KeyGesture keyGesture)
        {
            if (keyGesture == null)
            {
                return false;
            }

            try
            {
                // 先检查当前上下文的快捷键
                if (_contextShortcuts.TryGetValue(_currentContext, out var contextShortcuts) &&
                    contextShortcuts.TryGetValue(keyGesture, out var contextAction))
                {
                    contextAction.Action.Invoke();
                    _logger.LogDebug("执行上下文快捷键: {Context}.{Gesture}", _currentContext, keyGesture.DisplayString);
                    return true;
                }

                // 再检查全局快捷键
                if (_globalShortcuts.TryGetValue(keyGesture, out var globalAction))
                {
                    globalAction.Action.Invoke();
                    _logger.LogDebug("执行全局快捷键: {Gesture}", keyGesture.DisplayString);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行快捷键时发生错误: {Gesture}", keyGesture.DisplayString);
            }

            return false;
        }

        #endregion

        #region 私有方法

        /// <summary>注册默认快捷键</summary>
        private void RegisterDefaultShortcuts()
        {
            // 系统通用快捷键
            RegisterGlobalShortcut(new KeyGesture(Key.N, ModifierKeys.Control), () => { }, "新建");
            RegisterGlobalShortcut(new KeyGesture(Key.O, ModifierKeys.Control), () => { }, "打开");
            RegisterGlobalShortcut(new KeyGesture(Key.S, ModifierKeys.Control), () => { }, "保存");
            RegisterGlobalShortcut(new KeyGesture(Key.P, ModifierKeys.Control), () => { }, "打印");
            RegisterGlobalShortcut(new KeyGesture(Key.F, ModifierKeys.Control), () => { }, "查找");
            RegisterGlobalShortcut(new KeyGesture(Key.R, ModifierKeys.Control), () => { }, "刷新");
            RegisterGlobalShortcut(new KeyGesture(Key.Z, ModifierKeys.Control), () => { }, "撤销");
            RegisterGlobalShortcut(new KeyGesture(Key.Y, ModifierKeys.Control), () => { }, "重做");

            // 导航快捷键
            RegisterGlobalShortcut(new KeyGesture(Key.Escape), () => { }, "取消/返回");
            RegisterGlobalShortcut(new KeyGesture(Key.Enter), () => { }, "确认");
            RegisterGlobalShortcut(new KeyGesture(Key.F1), () => { }, "帮助");
            RegisterGlobalShortcut(new KeyGesture(Key.F5), () => { }, "刷新");

            // 医疗业务专用快捷键
            RegisterGlobalShortcut(new KeyGesture(Key.F2), () => { }, "快速录入患者信息");
            RegisterGlobalShortcut(new KeyGesture(Key.F3), () => { }, "快速开处方");
            RegisterGlobalShortcut(new KeyGesture(Key.F4), () => { }, "快速查询药材");
            RegisterGlobalShortcut(new KeyGesture(Key.F6), () => { }, "打印处方");
        }

        /// <summary>注册全局按键处理器</summary>
        private void RegisterGlobalKeyHandler()
        {
            // 监听应用程序级别的按键事件
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.PreviewKeyDown += OnGlobalKeyDown;
            }

            // 如果MainWindow还未初始化，则在Loaded事件中注册
            if (Application.Current.MainWindow == null)
            {
                Application.Current.Activated += (s, e) =>
                {
                    if (Application.Current.MainWindow != null)
                    {
                        Application.Current.MainWindow.PreviewKeyDown += OnGlobalKeyDown;
                    }
                };
            }
        }

        /// <summary>全局按键处理事件</summary>
        private void OnGlobalKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // 构建按键手势
                var modifiers = Keyboard.Modifiers;
                var key = e.Key;

                // 处理系统按键映射
                if (key == Key.System)
                {
                    key = e.SystemKey;
                }

                var keyGesture = new KeyGesture(key, modifiers);

                // 尝试执行快捷键
                if (ExecuteShortcut(keyGesture))
                {
                    e.Handled = true; // 阻止事件继续传播
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理全局按键事件时发生错误");
            }
        }

        #endregion
    }

    /// <summary>快捷键动作包装</summary>
    internal class ShortcutAction
    {
        public Action Action { get; }
        public string Description { get; }

        public ShortcutAction(Action action, string description = "")
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            Description = description ?? "";
        }
    }

    /// <summary>快捷键信息</summary>
    public class ShortcutInfo
    {
        public string Context { get; set; } = "";
        public KeyGesture KeyGesture { get; set; } = null!;
        public string Description { get; set; } = "";
    }
}
