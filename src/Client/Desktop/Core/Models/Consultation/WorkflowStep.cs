namespace LYBT.Desktop.Core.Models.Consultation
{
    /// <summary>
    /// 诊疗流程步骤（客户端工作流枚举）。
    /// 仅用于事件通信与导航指示。
    /// </summary>
    public enum WorkflowStep
    {
        PatientSelection,
        FourDiagnosis,
        Differentiation,
        Prescription
    }
}

