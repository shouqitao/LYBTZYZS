using LYBT.Desktop.Core.Models.Consultation;
using Prism.Events;

namespace LYBT.Desktop.Core.Events {

    /// <summary>
    /// 步骤验证响应事件
    /// </summary>
    public class StepValidationResponseEvent : PubSubEvent<StepValidationResponse> {
    }

    /// <summary>
    /// 步骤验证响应数据
    /// </summary>
    public class StepValidationResponse {
        public Guid RequestId { get; set; } = Guid.Empty;
        public WorkflowStep Step { get; set; } = 0;
        public bool IsValid { get; set; } = false;
        public string? ErrorMessage { get; set; }
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Info;
    }

    /// <summary>
    /// 验证严重程度
    /// </summary>
    public enum ValidationSeverity {
        Info,
        Warning,
        Error
    }
}
