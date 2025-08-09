using System;
using Prism.Events;
using LYBT.WPF.Client.Core.Models.Consultation;

namespace LYBT.WPF.Client.Core.Events
{
    /// <summary>
    /// 步骤验证响应事件
    /// </summary>
    public class StepValidationResponseEvent : PubSubEvent<StepValidationResponse>
    {
    }

    /// <summary>
    /// 步骤验证响应数据
    /// </summary>
    public class StepValidationResponse
    {
        public Guid RequestId { get; set; }
        public WorkflowStep Step { get; set; }
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public ValidationSeverity Severity { get; set; }
    }

    /// <summary>
    /// 验证严重程度
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }
}