using LYBT.Shared.Models.Contracts.Common;
using System;
using Prism.Events;
using LYBT.Desktop.Core.Models.Consultation;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 工作流步骤完成事件
    /// </summary>
    public class WorkflowStepCompletedEvent : PubSubEvent<WorkflowStepData>
    {
    }

    /// <summary>
    /// 工作流完成事件
    /// </summary>
    public class WorkflowCompletedEvent : PubSubEvent<WorkflowCompletionData>
    {
    }

    /// <summary>
    /// 导航到步骤事件
    /// </summary>
    public class NavigateToStepEvent : PubSubEvent<WorkflowStep>
    {
    }

    /// <summary>
    /// 保存步骤数据事件
    /// </summary>
    public class SaveStepDataEvent : PubSubEvent<WorkflowStep>
    {
    }

    /// <summary>
    /// 步骤验证请求事件
    /// </summary>
    public class StepValidationRequestEvent : PubSubEvent<WorkflowStep>
    {
    }

    /// <summary>
    /// 数据变更事件
    /// </summary>
    public class DataChangedEvent : PubSubEvent<DataChangedEventArgs>
    {
    }

    /// <summary>
    /// 导航事件
    /// </summary>
    public class NavigationEvent : PubSubEvent<NavigationInfo>
    {
    }

    /// <summary>
    /// 数据变更事件参数
    /// </summary>
    public class DataChangedEventArgs
    {
        public string DataType { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? Source { get; set; }
        public DateTime ChangeTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 导航事件参数
    /// </summary>
    public class NavigationEventArgs
    {
        public string NavigationPath { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
        public object? Parameters { get; set; }
        public string? Source { get; set; }

        public NavigationEventArgs() { }

        public NavigationEventArgs(string viewName)
        {
            ViewName = viewName;
            NavigationPath = viewName;
        }

        public NavigationEventArgs(string viewName, object parameters)
        {
            ViewName = viewName;
            NavigationPath = viewName;
            Parameters = parameters;
        }
    }

    /// <summary>
    /// 导航信息（工作流步骤间导航）
    /// </summary>
    public class NavigationInfo
    {
        public string FromStep { get; set; } = string.Empty;
        public string ToStep { get; set; } = string.Empty;
        public Guid MedicalCaseId { get; set; } = Guid.Empty;
        public DateTime NavigatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 工作流完成数据
    /// </summary>
    public class WorkflowCompletionData
    {
        public DateTime CompletionTime { get; set; } = DateTime.Now;
        public TimeSpan TotalDuration { get; set; } = TimeSpan.Zero;
    }
}