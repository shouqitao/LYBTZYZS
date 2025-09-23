using System.Windows;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;
using Microsoft.Extensions.Logging;
using Prism.Ioc;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 精简版对话框服务 - Phase 2重构
    /// 去除复杂的注册机制和ViewModel解析，采用约定优于配置的理念
    /// </summary>
    public class SimplifiedDialogService : ICustomDialogService
    {
        private readonly IContainerProvider _container;
        private readonly ILogger<SimplifiedDialogService> _logger;

        public SimplifiedDialogService(IContainerProvider container, ILogger<SimplifiedDialogService> logger)
        {
            _container = container;
            _logger = logger;
        }

        #region 基础消息对话框 - 使用原生MessageBox简化实现

        public async Task ShowInformationAsync(string message, string title = "信息")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        public async Task ShowWarningAsync(string message, string title = "警告")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning));
        }

        public async Task ShowErrorAsync(string message, string title = "错误")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        public async Task ShowSuccessAsync(string message, string title = "成功")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            });
        }

        #endregion

        #region 输入对话框 - 使用约定解析InputDialog

        public async Task<string?> ShowInputAsync(string message, string title = "输入", string defaultValue = "")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // 约定：使用InputDialog和InputDialogViewModel
                    var dialog = _container.Resolve<Views.Dialogs.InputDialog>();
                    var viewModel = _container.Resolve<ViewModels.Dialogs.InputDialogViewModel>();
                    
                    dialog.DataContext = viewModel;
                    
                    var parameters = new Dictionary<string, object>
                    {
                        ["Message"] = message,
                        ["Title"] = title,
                        ["DefaultValue"] = defaultValue
                    };

                    viewModel.OnDialogOpened(parameters);
                    SetDialogOwner(dialog);

                    var result = dialog.ShowDialog();
                    return result == true ? viewModel.InputValue : null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "输入对话框显示失败，降级到MessageBox");
                    
                    // 降级处理
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

        #endregion

        #region 模态对话框 - 简化为仅支持泛型版本

        public async Task<CustomDialogResult> ShowDialogAsync<T>(Dictionary<string, object>? parameters = null) where T : Window
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var dialog = _container.Resolve<T>();
                    SetupDialog(dialog, parameters);
                    
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
                    _logger.LogError(ex, "对话框显示失败: {DialogType}", typeof(T).Name);
                    return CustomDialogResult.Cancel();
                }
            });
        }

        public async Task<CustomDialogResult> ShowDialogAsync(string dialogName, Dictionary<string, object>? parameters = null)
        {
            // 简化实现：不再支持字符串名称版本，引导使用泛型版本
            _logger.LogWarning("字符串名称对话框已弃用，请使用泛型版本 ShowDialogAsync<T>");
            await ShowWarningAsync("此功能已弃用，请联系开发人员更新代码", "功能弃用");
            return CustomDialogResult.Cancel();
        }

        #endregion

        #region 文件对话框 - 保持原有实现

        public async Task<string?> ShowOpenFileDialogAsync(string title = "打开文件", string filter = "所有文件|*.*")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
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
            });
        }

        public async Task<string?> ShowSaveFileDialogAsync(string title = "保存文件", string filter = "所有文件|*.*", string defaultFileName = "")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
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
            });
        }

        public async Task<string?> ShowFolderBrowserDialogAsync(string title = "选择文件夹")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog()
                {
                    Title = title,
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "选择文件夹",
                    Filter = "文件夹|*.folder",
                    ValidateNames = false
                };

                var result = dialog.ShowDialog(Application.Current.MainWindow);
                return result == true ? System.IO.Path.GetDirectoryName(dialog.FileName) : null;
            });
        }

        #endregion

        #region 弃用的注册方法 - 简化为空实现

        public void RegisterDialog(string dialogName, Type dialogType)
        {
            // Phase 2简化：不再需要手动注册，使用约定优于配置
            _logger.LogInformation("对话框注册已简化，无需手动注册: {DialogName}", dialogName);
        }

        public bool IsDialogRegistered(string dialogName)
        {
            // Phase 2简化：始终返回false，引导使用泛型版本
            return false;
        }

        #endregion

        #region 私有辅助方法

        private void SetupDialog(Window dialog, Dictionary<string, object>? parameters)
        {
            // 设置参数到ViewModel
            if (dialog.DataContext is ICustomDialogAware dialogAware && parameters != null)
            {
                dialogAware.OnDialogOpened(parameters);
            }
            
            SetDialogOwner(dialog);
        }

        private static void SetDialogOwner(Window dialog)
        {
            if (Application.Current.MainWindow != null && dialog != Application.Current.MainWindow)
            {
                dialog.Owner = Application.Current.MainWindow;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
        }

        #endregion
    }
}