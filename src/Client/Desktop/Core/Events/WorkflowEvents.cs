using LYBT.Desktop.Core.Models.Consultation;
using Prism.Events;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 导航到指定工作流步骤事件。
    /// </summary>
    public class NavigateToStepEvent : PubSubEvent<WorkflowStep>
    {
    }

    /// <summary>
    /// 保存指定步骤数据事件。
    /// </summary>
    public class SaveStepDataEvent : PubSubEvent<WorkflowStep>
    {
    }

    /// <summary>
    /// 步骤校验请求事件（仍保留，便于将来扩展）。
    /// </summary>
    public class StepValidationRequestEvent : PubSubEvent<WorkflowStep>
    {
    }

    /// <summary>
    /// 业务数据变更事件。
    /// </summary>
    public class DataChangedEvent : PubSubEvent<DataChangedEventArgs>
    {
    }

    /// <summary>
    /// 视图导航事件。
    /// </summary>
    public class NavigationEvent : PubSubEvent<NavigationInfo>
    {
    }

    /// <summary>
    /// 业务数据变更事件参数。
    /// </summary>
    public class DataChangedEventArgs
    {
        public string DataType { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? Source { get; set; }
        public DateTime ChangeTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 导航事件参数。
    /// </summary>
    public class NavigationEventArgs
    {
        public string NavigationPath { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
        public object? Parameters { get; set; }
        public string? Source { get; set; }

        public NavigationEventArgs()
        {
        }

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
    /// 导航信息（从步骤、到步骤、病历ID等）。
    /// </summary>
    public class NavigationInfo
    {
        public string FromStep { get; set; } = string.Empty;
        public string ToStep { get; set; } = string.Empty;
        public Guid MedicalCaseId { get; set; } = Guid.Empty;
        public DateTime NavigatedAt { get; set; } = DateTime.Now;
    }
}

