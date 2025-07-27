namespace LYBT.Common.Enums.Patients {

    /// <summary>
    /// 患者状态
    /// </summary>
    public enum PatientStatus {
        /// <summary>
        /// 正常
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 停用
        /// </summary>
        Inactive = 0,

        /// <summary>
        /// 已删除
        /// </summary>
        Deleted = -1,

        /// <summary>
        /// 黑名单
        /// </summary>
        Blacklisted = -2
    }
}