using LYBT.Shared.Models.Contracts.Common;
using System;
using Prism.Events;
using LYBT.Desktop.Core.Models.Consultation;

namespace LYBT.Desktop.Core.Events
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