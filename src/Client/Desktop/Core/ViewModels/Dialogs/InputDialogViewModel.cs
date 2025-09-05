using System;
using System.Collections.Generic;
using System.Windows.Input;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Core.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Dialogs
{
    /// <summary>
    /// 输入对话框ViewModel
    /// </summary>
    public class InputDialogViewModel : ObservableObject, ICustomDialogAware
    {
        private string _title = "输入";
        private string _message = string.Empty;
        private string _inputValue = string.Empty;

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 提示消息
        /// </summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// 输入值
        /// </summary>
        public string InputValue
        {
            get => _inputValue;
            set => SetProperty(ref _inputValue, value);
        }

        /// <summary>
        /// 确定命令
        /// </summary>
        public ICommand OkCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 请求关闭事件
        /// </summary>
        public event Action<CustomDialogResult> RequestClose = delegate { };

        /// <summary>
        /// 构造函数
        /// </summary>
        public InputDialogViewModel()
        {
            OkCommand = new RelayCommand(OnOk, CanOk);
            CancelCommand = new RelayCommand(OnCancel);
        }

        /// <summary>
        /// 检查是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog() => true;

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        /// <param name="parameters">传入的参数</param>
        public void OnDialogOpened(Dictionary<string, object> parameters)
        {
            if (parameters.ContainsKey("Message"))
            {
                Message = parameters["Message"]?.ToString() ?? string.Empty;
            }

            if (parameters.ContainsKey("Title"))
            {
                Title = parameters["Title"]?.ToString() ?? "输入";
            }

            if (parameters.ContainsKey("DefaultValue"))
            {
                InputValue = parameters["DefaultValue"]?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed()
        {
            // 清理资源
        }

        /// <summary>
        /// 确定按钮是否可用
        /// </summary>
        /// <returns>是否可用</returns>
        private bool CanOk()
        {
            // 可以在这里添加验证逻辑
            return true;
        }

        /// <summary>
        /// 处理确定按钮点击
        /// </summary>
        private void OnOk()
        {
            var result = CustomDialogResult.Success(InputValue);
            result.Parameters["InputValue"] = InputValue;
            RequestClose?.Invoke(result);
        }

        /// <summary>
        /// 处理取消按钮点击
        /// </summary>
        private void OnCancel()
        {
            RequestClose?.Invoke(CustomDialogResult.Cancel());
        }
    }
}
