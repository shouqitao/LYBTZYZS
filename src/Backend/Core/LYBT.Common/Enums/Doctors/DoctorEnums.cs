namespace LYBT.Common.Enums.Doctors {

    /// <summary>
    /// 医生状态
    /// </summary>
    public enum DoctorStatus {

        /// <summary>
        /// 激活
        /// </summary>
        Active = 1,

        /// <summary>
        /// 停用
        /// </summary>
        Inactive = 0,

        /// <summary>
        /// 已删除
        /// </summary>
        Deleted = -1
    }

    /// <summary>
    /// 医生职称
    /// </summary>
    public enum DoctorTitle {

        /// <summary>
        /// 主任医师
        /// </summary>
        ChiefPhysician = 1,

        /// <summary>
        /// 副主任医师
        /// </summary>
        AssociateChiefPhysician = 2,

        /// <summary>
        /// 主治医师
        /// </summary>
        AttendingPhysician = 3,

        /// <summary>
        /// 住院医师
        /// </summary>
        ResidentPhysician = 4,

        /// <summary>
        /// 医师
        /// </summary>
        Physician = 5,

        /// <summary>
        /// 实习医师
        /// </summary>
        InternPhysician = 6,

        /// <summary>
        /// 初级职称
        /// </summary>
        Junior = 7
    }

    /// <summary>
    /// 医生工作状态
    /// </summary>
    public enum DoctorWorkStatus {

        /// <summary>
        /// 在岗
        /// </summary>
        OnDuty = 1,

        /// <summary>
        /// 离岗
        /// </summary>
        OffDuty = 0,

        /// <summary>
        /// 休假
        /// </summary>
        OnLeave = 2,

        /// <summary>
        /// 外出
        /// </summary>
        Away = 3,

        /// <summary>
        /// 门诊
        /// </summary>
        Clinic = 4
    }
}