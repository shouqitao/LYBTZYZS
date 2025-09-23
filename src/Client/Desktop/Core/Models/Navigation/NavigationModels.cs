using System;

namespace LYBT.Desktop.Core.Models.Navigation
{
    /// <summary>
    /// 导航信息
    /// </summary>
    public class NavigationInfo
    {
        /// <summary>
        /// 来源步骤
        /// </summary>
        public string FromStep { get; set; } = string.Empty;

        /// <summary>
        /// 目标步骤
        /// </summary>
        public string ToStep { get; set; } = string.Empty;

        /// <summary>
        /// 病历ID
        /// </summary>
        public Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid? PatientId { get; set; }

        /// <summary>
        /// 导航参数
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }

        /// <summary>
        /// 是否取消导航
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// 导航时间
        /// </summary>
        public DateTime NavigationTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 导航事件参数
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        public NavigationInfo NavigationInfo { get; }

        public NavigationEventArgs(NavigationInfo navigationInfo)
        {
            NavigationInfo = navigationInfo ?? throw new ArgumentNullException(nameof(navigationInfo));
        }
    }

    /// <summary>
    /// 导航结果
    /// </summary>
    public class NavigationResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 导航上下文
        /// </summary>
        public object? Context { get; set; }

        public static NavigationResult SuccessResult()
        {
            return new NavigationResult { Success = true };
        }

        public static NavigationResult FailureResult(string errorMessage)
        {
            return new NavigationResult 
            { 
                Success = false, 
                ErrorMessage = errorMessage 
            };
        }
    }
}