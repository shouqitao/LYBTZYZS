namespace LYBT.Common.Enums.Users {

    /// <summary>
    /// 系统用户角色枚举
    /// </summary>
    public enum UserRole {

        /// <summary>系统管理员</summary>
        Admin = 0,

        /// <summary>主治医生</summary>
        DiagnosingDoctor = 1,

        /// <summary>理疗师</summary>
        TreatmentDoctor = 2,

        /// <summary>药剂师</summary>
        PharmacyStaff = 3,

        /// <summary>挂号人员</summary>
        RegistrationStaff = 4,

        /// <summary>收费人员</summary>
        BillingStaff = 5
    }
}