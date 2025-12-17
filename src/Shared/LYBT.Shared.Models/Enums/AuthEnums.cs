using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 登录类型枚举（简化版本，仅保留基础功能）
    /// OpenSpec: unify-enums-to-shared - 移除冗余JsonConverter（已全局配置）
    /// </summary>
    public enum LoginType
    {
        /// <summary>密码登录</summary>
        [Description("密码登录")]
        Password = 0
        // 移除企业级认证方式：微信、短信、二维码、指纹、人脸识别、双因子认证
    }

    /// <summary>
    /// 认证会话状态枚举
    /// OpenSpec: unify-enums-to-shared - 移除冗余JsonConverter（已全局配置）
    /// </summary>
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
    /// OpenSpec: unify-enums-to-shared - 移除冗余JsonConverter（已全局配置）
    /// </summary>
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

    /// <summary>
    /// 认证错误码枚举
    /// Issue #1864: 统一错误码体系，便于客户端处理和国际化
    /// OpenSpec: unify-enums-to-shared - 移除冗余JsonConverter（已全局配置）
    /// </summary>
    public enum AuthErrorCode
    {
        /// <summary>无错误</summary>
        [Description("操作成功")]
        None = 0,

        // ========== 认证错误 1xx ==========

        /// <summary>凭据无效（用户名或密码错误）</summary>
        [Description("用户名或密码错误")]
        InvalidCredentials = 101,

        /// <summary>用户不存在</summary>
        [Description("用户不存在")]
        UserNotFound = 102,

        /// <summary>用户已禁用</summary>
        [Description("用户账号已被禁用")]
        UserDisabled = 103,

        /// <summary>密码已过期</summary>
        [Description("密码已过期，请修改密码")]
        PasswordExpired = 104,

        /// <summary>密码强度不足</summary>
        [Description("密码不符合安全要求")]
        WeakPassword = 105,

        // ========== Token错误 2xx ==========

        /// <summary>AccessToken已过期</summary>
        [Description("登录已过期，请重新登录")]
        TokenExpired = 201,

        /// <summary>Token无效（格式错误或签名验证失败）</summary>
        [Description("登录凭据无效")]
        TokenInvalid = 202,

        /// <summary>Token已被撤销</summary>
        [Description("登录已失效，请重新登录")]
        TokenRevoked = 203,

        /// <summary>RefreshToken已过期</summary>
        [Description("会话已过期，请重新登录")]
        RefreshTokenExpired = 204,

        /// <summary>RefreshToken无效</summary>
        [Description("刷新凭据无效")]
        RefreshTokenInvalid = 205,

        // ========== 会话错误 3xx ==========

        /// <summary>会话不存在</summary>
        [Description("会话不存在")]
        SessionNotFound = 301,

        /// <summary>会话绝对过期（超过最大存活时间）</summary>
        [Description("会话已到期，请重新登录")]
        SessionExpired = 302,

        /// <summary>并发会话数超限</summary>
        [Description("登录设备数超过限制")]
        ConcurrentSessionLimit = 303,

        // ========== 系统错误 9xx ==========

        /// <summary>内部服务器错误</summary>
        [Description("服务器内部错误")]
        InternalError = 901,

        /// <summary>服务不可用</summary>
        [Description("服务暂时不可用")]
        ServiceUnavailable = 902
    }
}
