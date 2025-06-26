namespace LYBT.Common.Enums.Prescriptions {

    /// <summary>
    /// 处方状态枚举
    /// </summary>
    public enum PrescriptionStatus {
        Draft = 0,      // 草稿
        Submitted = 1,  // 已提交
        Audited = 2,    // 已审核
        Dispensed = 3,  // 已发药
        Cancelled = 4   // 已作废
    }
}