using System;
using System.Threading.Tasks;

namespace LYBT.Desktop.Services.Dialogs
{
    /// <summary>
    /// 自定义对话框服务接口 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则，提供基本的对话框功能
    /// </summary>
    public interface ICustomDialogService
    {
        /// <summary>
        /// 显示信息对话框
        /// </summary>
        Task ShowMessageAsync(string message, string title = "提示");

        /// <summary>
        /// 显示信息对话框（兼容方法）
        /// </summary>
        Task ShowInformationAsync(string message, string title = "提示");

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        Task ShowErrorAsync(string message, string title = "错误");

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        Task ShowWarningAsync(string message, string title = "警告");

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        Task<bool> ShowConfirmAsync(string message, string title = "确认");

        /// <summary>
        /// 显示确认对话框（兼容方法）
        /// </summary>
        Task<bool> ShowConfirmationAsync(string message, string title = "确认");

        /// <summary>
        /// 显示自定义对话框
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        /// <param name="dialogName">对话框名称</param>
        /// <param name="parameters">对话框参数</param>
        /// <returns>对话框结果</returns>
        Task<TResult?> ShowDialogAsync<TResult>(string dialogName, object? parameters = null);

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        /// <param name="prompt">提示信息</param>
        /// <param name="title">标题</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>用户输入的值</returns>
        Task<string?> ShowInputAsync(string prompt, string title = "输入", string? defaultValue = null);

        /// <summary>
        /// 显示进度对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        /// <returns>进度对话框控制器</returns>
        IProgressDialog ShowProgressDialog(string title, string message);
    }

    /// <summary>
    /// 进度对话框接口
    /// </summary>
    public interface IProgressDialog : IDisposable
    {
        /// <summary>
        /// 更新进度
        /// </summary>
        void UpdateProgress(int percentage, string? message = null);

        /// <summary>
        /// 设置为不确定进度
        /// </summary>
        void SetIndeterminate(string? message = null);

        /// <summary>
        /// 关闭对话框
        /// </summary>
        void Close();

        /// <summary>
        /// 是否已取消
        /// </summary>
        bool IsCancelled { get; }
    }

    /// <summary>
    /// 对话框结果
    /// </summary>
    public enum DialogResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 3,
        No = 4
    }
}