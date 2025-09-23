using System.Windows;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;
using Microsoft.Extensions.Logging;
using Prism.Ioc;

namespace LYBT.Desktop.Core.Services
{

    /// <summary>
    /// WPF 对话框服务实现
    /// 基于原生 WPF 功能，兼容 Prism 8.1.97
    /// </summary>
    public class WpfDialogService : ICustomDialogService
    {
        private readonly IContainerProvider _container;
        private readonly ILogger<WpfDialogService> _logger;

        /// <summary>
        /// 注册的对话框映射 (对话框名称 -> 窗口类型)
        /// </summary>
        private readonly Dictionary<string, Type> _dialogRegistry = new();

        public WpfDialogService(IContainerProvider container, ILogger<WpfDialogService> logger)
        {
            _container = container;
            _logger = logger;

            // 初始化基础对话框注册
            InitializeDefaultDialogs();

            // UltraThink修复：调用业务对话框注册Action
            try
            {
                var businessDialogRegistrar = _container.Resolve<Action<LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService>>();
                businessDialogRegistrar?.Invoke(this);
                _logger.LogDebug("业务对话框注册完成，当前共注册 {Count} 个对话框", _dialogRegistry.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "注册业务对话框时发生警告，将使用基础对话框功能");
            }
        }

        /// <summary>
        /// 显示信息对话框
        /// </summary>
        public async Task ShowInformationAsync(string message, string title = "信息")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        public async Task ShowWarningAsync(string message, string title = "警告")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        public async Task ShowErrorAsync(string message, string title = "错误")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        /// <summary>
        /// 显示成功对话框
        /// </summary>
        public async Task ShowSuccessAsync(string message, string title = "成功")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            });
        }

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        public async Task<string?> ShowInputAsync(string message, string title = "输入", string defaultValue = "")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var viewModel = new ViewModels.Dialogs.InputDialogViewModel();
                    var dialog = new Views.Dialogs.InputDialog();

                    // 设置对话框参数
                    var parameters = new Dictionary<string, object>
                    {
                        ["Message"] = message,
                        ["Title"] = title,
                        ["DefaultValue"] = defaultValue
                    };

                    dialog.SetViewModel(viewModel);
                    viewModel.OnDialogOpened(parameters);

                    // 设置父窗口
                    if (Application.Current.MainWindow != null)
                    {
                        dialog.Owner = Application.Current.MainWindow;
                    }

                    var result = dialog.ShowDialog();

                    // 返回输入值或null
                    return result == true ? viewModel.InputValue : null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "显示输入对话框时发生错误");

                    // 降级到简单的MessageBox实现
                    var result = MessageBox.Show(
                        $"{message}\n\n当前值: {defaultValue}\n\n点击'是'保持当前值，'否'清空",
                        title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    return result switch
                    {
                        MessageBoxResult.Yes => defaultValue,
                        MessageBoxResult.No => string.Empty,
                        _ => null
                    };
                }
            });
        }

        /// <summary>
        /// 显示模态对话框 (泛型版本)
        /// </summary>
        public async Task<CustomDialogResult> ShowDialogAsync<T>(Dictionary<string, object>? parameters = null) where T : Window
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var dialog = _container.Resolve<T>();

                    // 设置参数到 ViewModel
                    if (dialog.DataContext is ICustomDialogAware dialogAware && parameters != null)
                    {
                        dialogAware.OnDialogOpened(parameters);
                    }

                    // 设置父窗口
                    if (Application.Current.MainWindow != null && dialog != Application.Current.MainWindow)
                    {
                        dialog.Owner = Application.Current.MainWindow;
                        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    }

                    var result = dialog.ShowDialog();

                    // 调用关闭回调
                    if (dialog.DataContext is ICustomDialogAware dialogAware2)
                    {
                        dialogAware2.OnDialogClosed();
                    }

                    return CustomDialogResult.Create(result, parameters, dialog.DataContext);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "显示对话框时发生错误: {DialogType}", typeof(T).Name);
                    return CustomDialogResult.Cancel();
                }
            });
        }

        /// <summary>
        /// 显示模态对话框 (字符串名称版本) - Prism 8.x优化版本
        /// </summary>
        public async Task<CustomDialogResult> ShowDialogAsync(string dialogName, Dictionary<string, object>? parameters = null)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (!_dialogRegistry.ContainsKey(dialogName))
                    {
                        _logger.LogWarning("未找到注册的对话框: {DialogName}", dialogName);
                        return CustomDialogResult.Cancel();
                    }

                    var dialogType = _dialogRegistry[dialogName];
                    var dialog = (Window)_container.Resolve(dialogType);

                    // Prism 8.x标准模式：通过约定解析ViewModel
                    SetupDialogViewModel(dialog, dialogName, parameters);

                    // 设置父窗口
                    SetupDialogOwner(dialog);

                    var result = dialog.ShowDialog();

                    // 调用关闭回调
                    if (dialog.DataContext is ICustomDialogAware dialogAware)
                    {
                        dialogAware.OnDialogClosed();
                    }

                    return CustomDialogResult.Create(result, parameters, dialog.DataContext);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "显示对话框时发生错误: {DialogName}", dialogName);
                    return CustomDialogResult.Cancel();
                }
            });
        }

        /// <summary>
        /// 设置对话框ViewModel - Prism 8.x标准模式
        /// </summary>
        private void SetupDialogViewModel(Window dialog, string dialogName, Dictionary<string, object>? parameters)
        {
            try
            {
                // 如果DataContext已设置且实现了ICustomDialogAware，直接使用
                if (dialog.DataContext is ICustomDialogAware existingDialogAware)
                {
                    if (parameters != null)
                    {
                        existingDialogAware.OnDialogOpened(parameters);
                    }
                    return;
                }

                // 尝试通过约定解析ViewModel：DialogName + "ViewModel"
                var viewModelName = GetViewModelNameByConvention(dialogName);
                if (!string.IsNullOrEmpty(viewModelName))
                {
                    var viewModelType = FindViewModelType(viewModelName);
                    if (viewModelType != null)
                    {
                        var viewModel = _container.Resolve(viewModelType);
                        dialog.DataContext = viewModel;

                        if (parameters != null && viewModel is ICustomDialogAware dialogAware)
                        {
                            dialogAware.OnDialogOpened(parameters);
                        }
                        return;
                    }
                }

                _logger.LogWarning("无法为对话框 {DialogName} 找到合适的ViewModel", dialogName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置对话框ViewModel时发生错误: {DialogName}", dialogName);
            }
        }

        /// <summary>
        /// 根据约定获取ViewModel名称
        /// </summary>
        private string GetViewModelNameByConvention(string dialogName)
        {
            // 移除Dialog后缀，添加ViewModel后缀
            if (dialogName.EndsWith("Dialog"))
            {
                var baseName = dialogName.Substring(0, dialogName.Length - 6);
                return baseName + "DialogViewModel";
            }
            return dialogName + "ViewModel";
        }

        /// <summary>
        /// 在所有加载的程序集中查找ViewModel类型
        /// </summary>
        private Type? FindViewModelType(string viewModelName)
        {
            try
            {
                // 在当前应用域的所有程序集中搜索
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.IsDynamic) continue;

                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.Name == viewModelName)
                        {
                            return type;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查找ViewModel类型时发生错误: {ViewModelName}", viewModelName);
                return null;
            }
        }

        /// <summary>
        /// 设置对话框父窗口
        /// </summary>
        private void SetupDialogOwner(Window dialog)
        {
            if (Application.Current.MainWindow != null && dialog != Application.Current.MainWindow)
            {
                dialog.Owner = Application.Current.MainWindow;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
        }

        /// <summary>
        /// 初始化默认对话框注册
        /// </summary>
        private void InitializeDefaultDialogs()
        {
            try
            {
                // 注册系统基础对话框
                RegisterBasicDialogs();
                
                _logger.LogDebug("默认对话框注册完成，共注册 {Count} 个对话框", _dialogRegistry.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化默认对话框注册时发生错误");
            }
        }

        /// <summary>
        /// 注册基础对话框
        /// </summary>
        private void RegisterBasicDialogs()
        {
            // 基础对话框注册在这里，业务对话框由各模块自行注册
            // 避免Core层与业务模块耦合
        }

        /// <summary>
        /// 注册对话框类型
        /// </summary>
        public void RegisterDialog(string dialogName, Type dialogType)
        {
            if (string.IsNullOrWhiteSpace(dialogName))
                throw new ArgumentException("对话框名称不能为空", nameof(dialogName));

            if (dialogType == null)
                throw new ArgumentNullException(nameof(dialogType));

            if (!typeof(Window).IsAssignableFrom(dialogType))
                throw new ArgumentException("对话框类型必须继承自Window", nameof(dialogType));

            _dialogRegistry[dialogName] = dialogType;
            _logger.LogDebug("已注册对话框: {DialogName} -> {DialogType}", dialogName, dialogType.Name);
        }

        /// <summary>
        /// 检查对话框是否已注册
        /// </summary>
        public bool IsDialogRegistered(string dialogName)
        {
            return !string.IsNullOrWhiteSpace(dialogName) && _dialogRegistry.ContainsKey(dialogName);
        }

        /// <summary>
        /// 显示打开文件对话框
        /// </summary>
        public async Task<string?> ShowOpenFileDialogAsync(string title = "打开文件", string filter = "所有文件|*.*")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var dialog = new Microsoft.Win32.OpenFileDialog()
                    {
                        Title = title,
                        Filter = filter,
                        CheckFileExists = true,
                        CheckPathExists = true
                    };

                    var result = dialog.ShowDialog(Application.Current.MainWindow);
                    return result == true ? dialog.FileName : null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "显示打开文件对话框时发生错误");
                    return null;
                }
            });
        }

        /// <summary>
        /// 显示保存文件对话框
        /// </summary>
        public async Task<string?> ShowSaveFileDialogAsync(string title = "保存文件", string filter = "所有文件|*.*", string defaultFileName = "")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var dialog = new Microsoft.Win32.SaveFileDialog()
                    {
                        Title = title,
                        Filter = filter,
                        FileName = defaultFileName,
                        CheckPathExists = true
                    };

                    var result = dialog.ShowDialog(Application.Current.MainWindow);
                    return result == true ? dialog.FileName : null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "显示保存文件对话框时发生错误");
                    return null;
                }
            });
        }

        /// <summary>
        /// 显示文件夹选择对话框 (使用WPF原生实现)
        /// </summary>
        public async Task<string?> ShowFolderBrowserDialogAsync(string title = "选择文件夹")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // 使用WPF原生的OpenFileDialog替代FolderBrowserDialog
                    var dialog = new Microsoft.Win32.OpenFileDialog()
                    {
                        Title = title,
                        CheckFileExists = false,
                        CheckPathExists = true,
                        FileName = "选择文件夹",
                        Filter = "文件夹|*.folder",
                        ValidateNames = false
                    };

                    // 尝试显示文件夹选择
                    var result = dialog.ShowDialog(Application.Current.MainWindow);
                    if (result == true)
                    {
                        // 返回所选文件的目录
                        return System.IO.Path.GetDirectoryName(dialog.FileName);
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "显示文件夹选择对话框时发生错误");
                    return null;
                }
            });
        }
    }
}
