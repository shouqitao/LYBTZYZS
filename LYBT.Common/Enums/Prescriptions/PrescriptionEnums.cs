namespace LYBT.Common.Enums.Prescriptions {

    /// <summary>
    /// 处方状态
    /// </summary>
    public enum PrescriptionStatus {

        /// <summary>
        /// 草稿
        /// </summary>
        Draft = 0,

        /// <summary>
        /// 已开具
        /// </summary>
        Issued = 1,

        /// <summary>
        /// 已确认
        /// </summary>
        Confirmed = 2,

        /// <summary>
        /// 已调配
        /// </summary>
        Dispensed = 3,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 4,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = -1,

        /// <summary>
        /// 已作废
        /// </summary>
        Voided = -2
    }
}