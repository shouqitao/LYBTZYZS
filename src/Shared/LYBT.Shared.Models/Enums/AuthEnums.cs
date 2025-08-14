using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 认证会话状态枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthSessionStatus
    {
        /// <summary>活跃中</summary>
        [Description("活跃中")]
        Active = 0,

        /// <summary>已过期</summary>
        [Description("已过期")]
        Expired = 1,

        /// <summary>已登出</summary>
        [Description("已登出")]
        LoggedOut = 2,

        /// <summary>已撤销</summary>
        [Description("已撤销")]
        Revoked = 3,

        /// <summary>被锁定</summary>
        [Description("被锁定")]
        Locked = 4
    }

    /// <summary>
    /// 安全级别枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SecurityLevel
    {
        /// <summary>低级</summary>
        [Description("低级")]
        Low = 0,

        /// <summary>中级</summary>
        [Description("中级")]
        Medium = 1,

        /// <summary>高级</summary>
        [Description("高级")]
        High = 2,

        /// <summary>严重</summary>
        [Description("严重")]
        Critical = 3,

        /// <summary>紧急</summary>
        [Description("紧急")]
        Emergency = 4
    }

    /// <summary>
    /// 认证事件类型枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthEventType
    {
        /// <summary>登录成功</summary>
        [Description("登录成功")]
        LoginSuccess = 0,

        /// <summary>登录失败</summary>
        [Description("登录失败")]
        LoginFailed = 1,

        /// <summary>登出</summary>
        [Description("登出")]
        Logout = 2,

        /// <summary>令牌刷新</summary>
        [Description("令牌刷新")]
        TokenRefresh = 3,

        /// <summary>密码修改</summary>
        [Description("密码修改")]
        PasswordChange = 4,

        /// <summary>账户锁定</summary>
        [Description("账户锁定")]
        AccountLocked = 5,

        /// <summary>异常访问</summary>
        [Description("异常访问")]
        SuspiciousAccess = 6,

        /// <summary>权限拒绝</summary>
        [Description("权限拒绝")]
        PermissionDenied = 7,

        /// <summary>数据访问</summary>
        [Description("数据访问")]
        DataAccess = 8,

        /// <summary>可疑活动</summary>
        [Description("可疑活动")]
        SuspiciousActivity = 9,

        /// <summary>系统错误</summary>
        [Description("系统错误")]
        SystemError = 10,

        /// <summary>密码已修改</summary>
        [Description("密码已修改")]
        PasswordChanged = 11,

        /// <summary>安全警报</summary>
        [Description("安全警报")]
        SecurityAlert = 12,

        /// <summary>合规违规</summary>
        [Description("合规违规")]
        ComplianceViolation = 13,

        /// <summary>令牌撤销</summary>
        [Description("令牌撤销")]
        TokenRevoked = 14,

        /// <summary>账户解锁</summary>
        [Description("账户解锁")]
        AccountUnlocked = 15,

        /// <summary>数据修改</summary>
        [Description("数据修改")]
        DataModification = 16
    }

    /// <summary>
    /// 登录类型枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LoginType
    {
        /// <summary>密码登录</summary>
        [Description("密码登录")]
        Password = 0,

        /// <summary>微信登录</summary>
        [Description("微信登录")]
        WeChat = 1,

        /// <summary>短信验证码</summary>
        [Description("短信验证码")]
        SmsCode = 2,

        /// <summary>二维码</summary>
        [Description("二维码")]
        QrCode = 3,

        /// <summary>指纹</summary>
        [Description("指纹")]
        Fingerprint = 4,

        /// <summary>人脸识别</summary>
        [Description("人脸识别")]
        FaceRecognition = 5,

        /// <summary>双因子认证</summary>
        [Description("双因子认证")]
        TwoFactor = 6
    }

    /// <summary>
    /// 用户角色枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        /// <summary>管理员</summary>
        [Description("管理员")]
        Admin = 0,

        /// <summary>医生</summary>
        [Description("医生")]
        Doctor = 1,

        /// <summary>护士</summary>
        [Description("护士")]
        Nurse = 2,

        /// <summary>药师</summary>
        [Description("药师")]
        Pharmacist = 3,

        /// <summary>前台</summary>
        [Description("前台")]
        Receptionist = 4
    }
}