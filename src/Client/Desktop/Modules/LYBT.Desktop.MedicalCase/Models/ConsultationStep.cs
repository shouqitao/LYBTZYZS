namespace LYBT.Desktop.MedicalCase.Models
{
    /// <summary>
    /// 看病流程步骤枚举（重构自FlowStep，删除患者选择）
    /// Issue #1567 - 分离患者选择与看病流程
    /// </summary>
    public enum ConsultationStep
    {
        /// <summary>
        /// Step 1: 辨证 - 录入四诊信息、主诉、现病史、诊断结论
        /// </summary>
        Consultation = 1,

        /// <summary>
        /// Step 2: 施治 - 根据诊断结果开具中药处方
        /// </summary>
        Prescription = 2,

        /// <summary>
        /// Step 3: 完成 - 确认诊疗信息并归档
        /// </summary>
        Completion = 3
    }
}
