using System.ComponentModel;

namespace LYBT.Common.Enums.Users {

    /// <summary>
    /// 系统用户角色枚举
    /// </summary>
    public enum UserRole {

        /// <summary>系统管理员</summary>
        [Description("系统管理员")]
        Admin = 0,

        /// <summary>主治医生</summary>
        [Description("主治医生")]
        DiagnosingDoctor = 1,

        /// <summary>理疗师</summary>
        [Description("理疗师")]
        TreatmentDoctor = 2,

        /// <summary>药剂师</summary>
        [Description("药剂师")]
        PharmacyStaff = 3,

        /// <summary>挂号人员</summary>
        [Description("挂号人员")]
        RegistrationStaff = 4,

        /// <summary>收费人员</summary>
        [Description("收费人员")]
        BillingStaff = 5
    }
}