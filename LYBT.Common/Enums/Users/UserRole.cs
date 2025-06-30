namespace LYBT.Common.Enums.Users {

    /// <summary>
    /// 系统用户角色枚举
    /// </summary>
    public enum UserRole {

        /// <summary>系统管理员</summary>
        Admin = 0,

        /// <summary>看诊医生</summary>
        DiagnosingDoctor = 1,

        /// <summary>诊疗室医生（执行治疗任务）</summary>
        TreatmentDoctor = 2,

        /// <summary>药房工作人员</summary>
        PharmacyStaff = 3,

        /// <summary>挂号/前台工作人员</summary>
        RegistrationStaff = 4,

        /// <summary>收费人员</summary>
        BillingStaff = 5
    }
}