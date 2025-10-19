namespace LYBT.Desktop.MedicalCase.Models
{
    /// <summary>
    /// 医案流程步骤枚举（Epic #1494 - 4步流程）
    /// </summary>
    public enum FlowStep
    {
        /// <summary>
        /// Step 1: 患者选择 - 自动创建MedicalCase（DDD聚合根）
        /// </summary>
        SelectPatient = 1,

        /// <summary>
        /// Step 2: 填写诊断 - 录入Consultation信息
        /// </summary>
        FillConsultation = 2,

        /// <summary>
        /// Step 3: 填写处方 - 录入Prescription信息
        /// </summary>
        FillPrescription = 3,

        /// <summary>
        /// Step 4: 完成医案 - 设置MedicalCase.Status=Completed
        /// </summary>
        CompleteMedicalCase = 4
    }
}
