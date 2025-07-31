using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 医生状态枚举 - 前后端共享
    /// </summary>
    [Description("医生状态")]
    public enum DoctorStatus
    {
        /// <summary>停用</summary>
        [Description("停用")]
        Inactive = 0,

        /// <summary>激活</summary>
        [Description("激活")]
        Active = 1,

        /// <summary>已删除</summary>
        [Description("已删除")]
        Deleted = -1
    }

    /// <summary>
    /// 医生职称枚举 - 前后端共享
    /// </summary>
    [Description("医生职称")]
    public enum DoctorTitle
    {
        /// <summary>主任医师</summary>
        [Description("主任医师")]
        ChiefPhysician = 1,

        /// <summary>副主任医师</summary>
        [Description("副主任医师")]
        AssociateChiefPhysician = 2,

        /// <summary>主治医师</summary>
        [Description("主治医师")]
        AttendingPhysician = 3,

        /// <summary>住院医师</summary>
        [Description("住院医师")]
        ResidentPhysician = 4,

        /// <summary>医师</summary>
        [Description("医师")]
        Physician = 5,

        /// <summary>实习医师</summary>
        [Description("实习医师")]
        InternPhysician = 6,

        /// <summary>初级职称</summary>
        [Description("初级职称")]
        Junior = 7
    }

    /// <summary>
    /// 医生工作状态枚举 - 前后端共享
    /// </summary>
    [Description("医生工作状态")]
    public enum DoctorWorkStatus
    {
        /// <summary>离岗</summary>
        [Description("离岗")]
        OffDuty = 0,

        /// <summary>在岗</summary>
        [Description("在岗")]
        OnDuty = 1,

        /// <summary>休假</summary>
        [Description("休假")]
        OnLeave = 2,

        /// <summary>外出</summary>
        [Description("外出")]
        Away = 3,

        /// <summary>门诊</summary>
        [Description("门诊")]
        Clinic = 4
    }
}