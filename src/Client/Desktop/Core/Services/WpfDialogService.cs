using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;
// using LYBT.Desktop.Shell.Dialogs.Views; // 暂时注释以避免跨项目依赖

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
                    var result = MessageBox.Show($"{message}\n\n当前值: {defaultValue}\n\n点击'是'保持当前值，'否'清空", 
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
        /// 显示模态对话框 (字符串名称版本)
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
                    
                    // 为特殊对话框设置ViewModel
                    if (dialogName == "HerbSelectionDialog" && dialog is Views.Dialogs.HerbSelectionDialog herbDialog)
                    {
                        var viewModel = _container.Resolve<ViewModels.Dialogs.HerbSelectionDialogViewModel>();
                        herbDialog.SetViewModel(viewModel);
                        // 新的DialogViewModelBase架构不需要OnDialogOpened方法
                        // 参数通过构造函数或其他方式处理
                    }
                    else if (dialogName == "FormulaSelectionDialog" && dialog is Views.Dialogs.FormulaSelectionDialog formulaDialog)
                    {
                        var viewModel = _container.Resolve<ViewModels.Dialogs.FormulaSelectionDialogViewModel>();
                        formulaDialog.SetViewModel(viewModel);
                        // FormulaSelectionDialogViewModel现在继承DialogViewModelBase，不需要OnDialogOpened调用
                        // 参数处理已经在DialogViewModelBase中标准化
                    }
                    else if (dialogName == "InputDialog" && dialog is Views.Dialogs.InputDialog inputDialog)
                    {
                        var viewModel = new ViewModels.Dialogs.InputDialogViewModel();
                        inputDialog.SetViewModel(viewModel);
                        if (parameters != null)
                        {
                            viewModel.OnDialogOpened(parameters);
                        }
                    }
                    else if (dialogName == "PatientAddEditDialog")
                    {
                        // 使用反射来设置ViewModel，避免直接引用模块类型
                        var viewModelTypeName = "LYBT.Desktop.Patients.ViewModels.PatientAddEditDialogViewModel";
                        var assembly = dialog.GetType().Assembly;
                        var viewModelType = assembly.GetType(viewModelTypeName);
                        
                        if (viewModelType != null)
                        {
                            var viewModel = _container.Resolve(viewModelType);
                            dialog.DataContext = viewModel;
                            
                            if (parameters != null && viewModel is ICustomDialogAware dialogAware)
                            {
                                dialogAware.OnDialogOpened(parameters);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("无法找到PatientAddEditDialogViewModel类型");
                        }
                    }
                    // 通用处理：如果DataContext已经是ICustomDialogAware
                    else if (dialog.DataContext is ICustomDialogAware dialogAware && parameters != null)
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
                    _logger.LogError(ex, "显示对话框时发生错误: {DialogName}", dialogName);
                    return CustomDialogResult.Cancel();
                }
            });
        }

        /// <summary>
        /// 注册对话框类型
        /// </summary>
        /// <param name="dialogName">对话框名称</param>
        /// <param name="dialogType">对话框窗口类型</param>
        public void RegisterDialog(string dialogName, Type dialogType)
        {
            if (string.IsNullOrEmpty(dialogName))
                throw new ArgumentException("对话框名称不能为空", nameof(dialogName));

            if (dialogType == null)
                throw new ArgumentNullException(nameof(dialogType));

            if (!typeof(Window).IsAssignableFrom(dialogType))
                throw new ArgumentException("对话框类型必须继承自 Window", nameof(dialogType));

            _dialogRegistry[dialogName] = dialogType;
            _logger.LogDebug("注册对话框: {DialogName} -> {DialogType}", dialogName, dialogType.Name);
        }

        /// <summary>
        /// 检查对话框是否已注册
        /// </summary>
        /// <param name="dialogName">对话框名称</param>
        /// <returns>是否已注册</returns>
        public bool IsDialogRegistered(string dialogName)
        {
            return !string.IsNullOrEmpty(dialogName) && _dialogRegistry.ContainsKey(dialogName);
        }

        /// <summary>
        /// 显示打开文件对话框
        /// </summary>
        public async Task<string?> ShowOpenFileDialogAsync(string title = "打开文件", string filter = "所有文件|*.*")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = filter
                };

                if (dialog.ShowDialog() == true)
                {
                    return dialog.FileName;
                }
                return null;
            });
        }

        /// <summary>
        /// 显示保存文件对话框
        /// </summary>
        public async Task<string?> ShowSaveFileDialogAsync(string title = "保存文件", string filter = "所有文件|*.*", string defaultFileName = "")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = filter,
                    FileName = defaultFileName
                };

                if (dialog.ShowDialog() == true)
                {
                    return dialog.FileName;
                }
                return null;
            });
        }

        /// <summary>
        /// 显示文件夹选择对话框
        /// </summary>
        public async Task<string?> ShowFolderBrowserDialogAsync(string title = "选择文件夹")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // WPF没有内置的文件夹选择对话框，可以使用Windows Forms或WPF-UI的实现
                // 这里暂时返回null，可以根据需要后续实现
                return (string?)null;
            });
        }

        /// <summary>
        /// 初始化默认对话框注册
        /// </summary>
        private void InitializeDefaultDialogs()
        {
            try
            {
                // 注册系统内置对话框
                // RegisterDialog("InputDialog", typeof(Views.Dialogs.InputDialog));
                // RegisterDialog("ConfirmationDialog", typeof(ConfirmationDialog)); // Shell项目对话框暂时注释
                // RegisterDialog("InformationDialog", typeof(InformationDialog)); // Shell项目对话框暂时注释
                // RegisterDialog("ErrorDialog", typeof(ErrorDetailsDialog)); // Shell项目对话框暂时注释
                
                // 注册业务对话框
                RegisterDialog("HerbSelectionDialog", typeof(Views.Dialogs.HerbSelectionDialog));
                RegisterDialog("FormulaSelectionDialog", typeof(Views.Dialogs.FormulaSelectionDialog));
                
                // 业务对话框将由各模块在初始化时动态注册
                // 避免Core层直接依赖业务模块类型
                
                _logger.LogDebug("默认对话框注册完成，共注册 {Count} 个对话框", _dialogRegistry.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化默认对话框注册时发生错误");
            }
        }
    }
}