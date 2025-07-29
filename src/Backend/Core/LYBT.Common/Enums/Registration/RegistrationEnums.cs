namespace LYBT.Common.Enums.Registration {

    /// <summary>
    /// 挂号类型
    /// </summary>
    public enum RegistrationType {

        /// <summary>
        /// 普通号
        /// </summary>
        Regular = 1,

        /// <summary>
        /// 专家号
        /// </summary>
        Expert = 2,

        /// <summary>
        /// 急诊号
        /// </summary>
        Emergency = 3,

        /// <summary>
        /// 预约号
        /// </summary>
        Appointment = 4,

        /// <summary>
        /// 普通号（别名）
        /// </summary>
        Normal = 1
    }

    /// <summary>
    /// 挂号状态
    /// </summary>
    public enum RegistrationStatus {

        /// <summary>
        /// 已预约
        /// </summary>
        Scheduled = 0,

        /// <summary>
        /// 已到达
        /// </summary>
        Arrived = 1,

        /// <summary>
        /// 就诊中
        /// </summary>
        InConsultation = 2,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 3,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = -1,

        /// <summary>
        /// 爽约
        /// </summary>
        NoShow = -2,

        /// <summary>
        /// 已过期
        /// </summary>
        Expired = -3
    }
}