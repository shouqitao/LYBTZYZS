using System.ComponentModel;

namespace LYBT.WPF.Client.Core.Enums
{
    /// <summary>
    /// 用户角色枚举
    /// </summary>
    public enum UserRole
    {
        /// <summary>超级管理员</summary>
        [Description("超级管理员")]
        SuperAdmin = 1,

        /// <summary>管理员</summary>
        [Description("管理员")]
        Admin = 2,

        /// <summary>医生</summary>
        [Description("医生")]
        DiagnosingDoctor = 3,

        /// <summary>前台</summary>
        [Description("前台")]
        FrontDesk = 4,

        /// <summary>收银员</summary>
        [Description("收银员")]
        Cashier = 5,

        /// <summary>药剂师</summary>
        [Description("药剂师")]
        Pharmacist = 6,

        /// <summary>实习医师</summary>
        [Description("实习医师")]
        InternDoctor = 7,

        /// <summary>护士</summary>
        [Description("护士")]
        Nurse = 8,

        /// <summary>供应商</summary>
        [Description("供应商")]
        Vendor = 9,

        /// <summary>宾客/访客</summary>
        [Description("宾客/访客")]
        Guest = 10
    }
}