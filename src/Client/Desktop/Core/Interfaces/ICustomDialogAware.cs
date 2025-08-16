using System;
using System.Collections.Generic;
using LYBT.Desktop.Core.Models.Common;

namespace LYBT.Desktop.Core.Interfaces
{
    /// <summary>
    /// 自定义对话框感知接口
    /// 替代 Prism IDialogAware，兼容 Prism 8.1.97
    /// </summary>
    public interface ICustomDialogAware
    {
        /// <summary>
        /// 对话框标题
        /// </summary>
        string Title { get; }

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        event Action<CustomDialogResult> RequestClose;

        /// <summary>
        /// 检查是否可以关闭对话框
        /// </summary>
        /// <returns>true: 可以关闭, false: 不可以关闭</returns>
        bool CanCloseDialog();

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        /// <param name="parameters">传入的参数</param>
        void OnDialogOpened(Dictionary<string, object> parameters);

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        void OnDialogClosed();
    }
}