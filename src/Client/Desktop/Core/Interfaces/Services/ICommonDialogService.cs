using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 通用对话框服务接口
    /// 提供统一的对话框调用方式，支持消息框、输入框和文件选择对话框
    /// </summary>
    public interface ICommonDialogService
    {
        #region 消息对话框

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户点击"是"返回 true，否则返回 false</returns>
        Task<bool> ShowConfirmationAsync(string message, string title = "确认");

        /// <summary>
        /// 显示信息对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        Task ShowInformationAsync(string message, string title = "信息");

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        Task ShowWarningAsync(string message, string title = "警告");

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        Task ShowErrorAsync(string message, string title = "错误");

        #endregion

        #region 输入对话框

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        /// <param name="message">提示信息</param>
        /// <param name="title">标题</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>用户输入的内容，取消返回 null</returns>
        Task<string?> ShowInputAsync(string message, string title = "输入", string defaultValue = "");

        #endregion

        #region 文件对话框

        /// <summary>
        /// 显示打开文件对话框
        /// </summary>
        /// <param name="filter">文件过滤器</param>
        /// <param name="title">标题</param>
        /// <returns>选择的文件路径，取消返回 null</returns>
        Task<string?> ShowOpenFileDialogAsync(string filter = "All Files (*.*)|*.*", string title = "打开文件");

        /// <summary>
        /// 显示保存文件对话框
        /// </summary>
        /// <param name="filter">文件过滤器</param>
        /// <param name="title">标题</param>
        /// <param name="defaultFileName">默认文件名</param>
        /// <returns>保存的文件路径，取消返回 null</returns>
        Task<string?> ShowSaveFileDialogAsync(string filter = "All Files (*.*)|*.*", string title = "保存文件", string defaultFileName = "");

        /// <summary>
        /// 显示选择文件夹对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <returns>选择的文件夹路径，取消返回 null</returns>
        Task<string?> ShowFolderBrowserDialogAsync(string title = "选择文件夹");

        #endregion

        #region 同步方法（为了兼容旧代码）

        /// <summary>
        /// 显示确认对话框（同步）
        /// </summary>
        bool ShowConfirmation(string message, string title = "确认");

        /// <summary>
        /// 显示信息对话框（同步）
        /// </summary>
        void ShowInformation(string message, string title = "信息");

        /// <summary>
        /// 显示警告对话框（同步）
        /// </summary>
        void ShowWarning(string message, string title = "警告");

        /// <summary>
        /// 显示错误对话框（同步）
        /// </summary>
        void ShowError(string message, string title = "错误");

        #endregion
    }
}