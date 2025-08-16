using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Core.Models.Common;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 自定义对话框服务接口
    /// 替代 Prism IDialogService，兼容 Prism 8.1.97
    /// </summary>
    public interface ICustomDialogService
    {
        /// <summary>
        /// 显示信息对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">对话框标题</param>
        /// <returns>任务</returns>
        Task ShowInformationAsync(string message, string title = "信息");

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">对话框标题</param>
        /// <returns>任务</returns>
        Task ShowWarningAsync(string message, string title = "警告");

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">对话框标题</param>
        /// <returns>任务</returns>
        Task ShowErrorAsync(string message, string title = "错误");

        /// <summary>
        /// 显示成功对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">对话框标题</param>
        /// <returns>任务</returns>
        Task ShowSuccessAsync(string message, string title = "成功");

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">对话框标题</param>
        /// <returns>用户选择结果</returns>
        Task<bool> ShowConfirmationAsync(string message, string title = "确认");

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        /// <param name="message">提示消息</param>
        /// <param name="title">对话框标题</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>用户输入的字符串，取消返回null</returns>
        Task<string?> ShowInputAsync(string message, string title = "输入", string defaultValue = "");

        /// <summary>
        /// 显示模态对话框
        /// </summary>
        /// <typeparam name="T">对话框窗口类型</typeparam>
        /// <param name="parameters">传递给对话框的参数</param>
        /// <returns>对话框结果</returns>
        Task<CustomDialogResult> ShowDialogAsync<T>(Dictionary<string, object>? parameters = null) where T : Window;

        /// <summary>
        /// 显示模态对话框（非泛型版本）
        /// </summary>
        /// <param name="dialogName">对话框名称</param>
        /// <param name="parameters">传递给对话框的参数</param>
        /// <returns>对话框结果</returns>
        Task<CustomDialogResult> ShowDialogAsync(string dialogName, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// 注册对话框类型
        /// </summary>
        /// <param name="dialogName">对话框名称</param>
        /// <param name="dialogType">对话框窗口类型</param>
        void RegisterDialog(string dialogName, Type dialogType);

        /// <summary>
        /// 检查对话框是否已注册
        /// </summary>
        /// <param name="dialogName">对话框名称</param>
        /// <returns>是否已注册</returns>
        bool IsDialogRegistered(string dialogName);
    }
}