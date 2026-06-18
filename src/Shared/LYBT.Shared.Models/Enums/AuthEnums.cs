using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 用户角色枚举 - 四角色体系（SuperAdmin/Admin/Doctor/Receptionist）
    /// Issue #1909: 重构为分层权限体系
    /// refactor-auth-role-system Phase 2.2: 添加Receptionist角色
    /// OpenSpec: unify-enums-to-shared - 移除冗余JsonConverter（已全局配置）
    /// </summary>
    public enum UserRole
    {
        /// <summary>前台接待（患者登记、预约管理）</summary>
        /// <remarks>refactor-auth-role-system Phase 2.2.1</remarks>
        [Description("前台接待")]
        Receptionist = 0,

        /// <summary>医生（诊疗、记录、查询等业务操作）</summary>
        [Description("医生")]
        Doctor = 1,

        /// <summary>管理员（系统管理、用户管理、系统配置，可以管理Doctor但不能管理Admin）</summary>
        [Description("管理员")]
        Admin = 10,

        /// <summary>超级管理员（最高权限，可以管理Admin，系统初始化创建）</summary>
        [Description("超级管理员")]
        SuperAdmin = 100
    }

}
