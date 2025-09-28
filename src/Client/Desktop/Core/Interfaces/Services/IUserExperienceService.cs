using System.ComponentModel;
using LYBT.Desktop.Core.Services;

namespace LYBT.Desktop.Core.Interfaces.Services
{

    /// <summary>
    /// 用户体验增强服务接口 - P7-04 UltraThink用户体验优化
    /// </summary>
    public interface IUserExperienceService : INotifyPropertyChanged, IDisposable
    {

        #region 属性

        /// <summary>全局加载状态</summary>
        bool IsGlobalLoading { get; }

        /// <summary>加载消息</summary>
        string LoadingMessage { get; }

        /// <summary>状态消息</summary>
        string StatusMessage { get; }

        /// <summary>当前反馈类型</summary>
        FeedbackType CurrentFeedbackType { get; }

        /// <summary>操作进度 (0-100)</summary>
        int OperationProgress { get; }

        #endregion 属性

        #region 加载状态管理

        /// <summary>开始全局加载</summary>
        void StartGlobalLoading(string message = "加载中...");

        /// <summary>停止全局加载</summary>
        void StopGlobalLoading();

        /// <summary>更新操作进度</summary>
        void UpdateProgress(int progress, string? message = null);

        #endregion 加载状态管理

        #region 用户反馈

        /// <summary>显示成功反馈</summary>
        void ShowSuccessFeedback(string message);

        /// <summary>显示错误反馈</summary>
        void ShowErrorFeedback(string message);

        /// <summary>显示警告反馈</summary>
        void ShowWarningFeedback(string message);

        /// <summary>显示信息反馈</summary>
        void ShowInfoFeedback(string message);

        /// <summary>清除状态消息</summary>
        void ClearStatusMessage();

        #endregion 用户反馈

        #region 操作执行与反馈

        /// <summary>执行操作并提供用户反馈（有返回值）</summary>
        Task<T> ExecuteWithFeedbackAsync<T>(
            Func<Task<T>> operation,
            string loadingMessage = "处理中...",
            string successMessage = "操作成功",
            string errorMessage = "操作失败");

        /// <summary>执行操作并提供用户反馈（无返回值）</summary>
        Task ExecuteWithFeedbackAsync(
            Func<Task> operation,
            string loadingMessage = "处理中...",
            string successMessage = "操作成功",
            string errorMessage = "操作失败");

        /// <summary>显示友好的错误信息</summary>
        Task ShowFriendlyErrorAsync(Exception exception, string context = "");

        #endregion 操作执行与反馈
    }
}
