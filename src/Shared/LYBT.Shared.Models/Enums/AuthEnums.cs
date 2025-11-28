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
    /// 用户角色枚举 - 三角色体系（SuperAdmin/Admin/Doctor）
    /// Issue #1909: 重构为三角色体系以解决权限管理严谨性问题
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        /// <summary>超级管理员（最高权限，可以管理Admin，系统初始化创建）</summary>
        [Description("超级管理员")]
        SuperAdmin = 100,

        /// <summary>管理员（系统管理、用户管理、系统配置，可以管理Doctor但不能管理Admin）</summary>
        [Description("管理员")]
        Admin = 10,

        /// <summary>医生（诊疗、记录、查询等业务操作）</summary>
        [Description("医生")]
        Doctor = 1
    }
}
