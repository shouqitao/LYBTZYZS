using System;
using Prism.Events;
using LYBT.WPF.Client.Core.Models.Consultation;

namespace LYBT.WPF.Client.Core.Events
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
    /// 工作流完成数据
    /// </summary>
    public class WorkflowCompletionData
    {
        public DateTime CompletionTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }
}