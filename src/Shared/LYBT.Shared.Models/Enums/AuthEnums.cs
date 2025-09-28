using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 登录类型枚举（简化版本，仅保留基础功能）
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LoginType
    {
        /// <summary>密码登录</summary>
        [Description("密码登录")]
        Password = 0
        // 移除企业级认证方式：微信、短信、二维码、指纹、人脸识别、双因子认证
    }

    /// <summary>
    /// 认证会话状态枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthSessionStatus
    {
        /// <summary>活跃状态</summary>
        [Description("活跃")]
        Active = 1,

        /// <summary>已过期</summary>
        [Description("已过期")]
        Expired = 2,

        /// <summary>已注销</summary>
        [Description("已注销")]
        LoggedOut = 3,

        /// <summary>已锁定</summary>
        [Description("已锁定")]
        Locked = 4
    }

    /// <summary>
    /// 用户角色枚举 - 统一为 Doctor 主角色模式（Admin/Doctor）
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {

        /// <summary>管理员（系统管理、用户管理、系统配置）</summary>
        [Description("管理员")]
        Admin = 10,

        /// <summary>医生（诊疗、记录、查询等业务操作）</summary>
        [Description("医生")]
        Doctor = 1,

        // 兼容性映射：旧角色保留以避免序列化错误，但标记为过时

        /// <summary>普通用户 - 已统一到Doctor角色</summary>
        [Description("普通用户")]
        [Obsolete("Use Doctor instead. User role unified to Doctor in role unification.", false)]
        User = 20,

        /// <summary>药师 - 已统一到Doctor角色</summary>
        [Description("药师")]
        [Obsolete("Use Doctor instead. Pharmacist role unified to Doctor in role unification.", false)]
        Pharmacist = 2,

        /// <summary>前台 - 已统一到Doctor角色</summary>
        [Description("前台")]
        [Obsolete("Use Doctor instead. Receptionist role unified to Doctor in role unification.", false)]
        Receptionist = 3,

        /// <summary>收银员 - 已统一到Doctor角色</summary>
        [Description("收银员")]
        [Obsolete("Use Doctor instead. Cashier role unified to Doctor in role unification.", false)]
        Cashier = 4,

        /// <summary>理疗师 - 已统一到Doctor角色</summary>
        [Description("理疗师")]
        [Obsolete("Use Doctor instead. Therapist role unified to Doctor in role unification.", false)]
        Therapist = 5
    }
}
