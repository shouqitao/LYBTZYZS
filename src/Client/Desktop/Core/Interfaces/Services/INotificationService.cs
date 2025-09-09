namespace LYBT.Desktop.Core.Interfaces.Services
{

    /// <summary>
    /// 通知服务接口 - UltraThink简化的消息通知管理
    /// </summary>
    public interface INotificationService
    {

        #region 消息显示方法

        /// <summary>
        /// 显示信息消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">消息标题（可选）</param>
        void ShowInfo(string message, string? title = null);

        /// <summary>
        /// 显示成功消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">消息标题（可选）</param>
        void ShowSuccess(string message, string? title = null);

        /// <summary>
        /// 显示警告消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">消息标题（可选）</param>
        void ShowWarning(string message, string? title = null);

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">消息标题（可选）</param>
        void ShowError(string message, string? title = null);

        /// <summary>
        /// 显示自定义消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="type">消息类型</param>
        /// <param name="title">消息标题（可选）</param>
        /// <param name="autoClose">是否自动关闭</param>
        /// <param name="duration">显示持续时间（毫秒）</param>
        void ShowMessage(string message, NotificationType type, string? title = null,
            bool autoClose = true, int duration = 3000);

        #endregion 消息显示方法

        #region 对话框方法

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认消息</param>
        /// <param name="title">对话框标题</param>
        /// <returns>用户选择结果</returns>
        Task<bool> ShowConfirmAsync(string message, string title = "确认");

        /// <summary>
        /// 显示选择对话框
        /// </summary>
        /// <param name="message">选择消息</param>
        /// <param name="title">对话框标题</param>
        /// <param name="yesText">确认按钮文本</param>
        /// <param name="noText">取消按钮文本</param>
        /// <returns>用户选择结果</returns>
        Task<bool> ShowChoiceAsync(string message, string title = "选择",
            string yesText = "是", string noText = "否");

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        /// <param name="message">输入提示</param>
        /// <param name="title">对话框标题</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>用户输入结果，null表示取消</returns>
        Task<string?> ShowInputAsync(string message, string title = "输入", string defaultValue = "");

        #endregion 对话框方法

        #region 加载状态

        /// <summary>
        /// 显示加载状态
        /// </summary>
        /// <param name="message">加载消息</param>
        void ShowLoading(string message = "正在加载...");

        /// <summary>
        /// 隐藏加载状态
        /// </summary>
        void HideLoading();

        /// <summary>
        /// 显示进度条
        /// </summary>
        /// <param name="message">进度消息</param>
        /// <param name="progress">当前进度（0-100）</param>
        void ShowProgress(string message, int progress);

        /// <summary>
        /// 隐藏进度条
        /// </summary>
        void HideProgress();

        #endregion 加载状态

        #region 事件

        /// <summary>
        /// 消息显示事件
        /// </summary>
        event EventHandler<NotificationEventArgs>? NotificationShown;

        /// <summary>
        /// 加载状态变化事件
        /// </summary>
        event EventHandler<LoadingStateChangedEventArgs>? LoadingStateChanged;

        #endregion 事件
    }

    #region 枚举和事件参数

    /// <summary>
    /// 通知类型
    /// </summary>
    public enum NotificationType
    {

        /// <summary>信息</summary>
        Info,

        /// <summary>成功</summary>
        Success,

        /// <summary>警告</summary>
        Warning,

        /// <summary>错误</summary>
        Error
    }

    /// <summary>
    /// 通知事件参数
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public string? Title { get; set; }
        public NotificationType Type { get; set; }
        public bool AutoClose { get; set; }
        public int Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 加载状态变化事件参数
    /// </summary>
    public class LoadingStateChangedEventArgs : EventArgs
    {
        public bool IsLoading { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? Progress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    #endregion 枚举和事件参数
}
