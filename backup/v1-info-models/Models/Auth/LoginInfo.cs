using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Desktop.Core.Models.Auth
{
    /// <summary>
    /// 登录信息模型 - 前端专用，整合登录请求和响应
    /// UltraThink架构Layer 4: Info模型，专为WPF桌面UI设计
    /// </summary>
    public class LoginInfo
    {
        #region 登录请求属性
        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>密码</summary>
        [DisplayName("密码")]
        public string Password { get; set; } = string.Empty;

        /// <summary>客户端IP</summary>
        [DisplayName("客户端IP")]
        public string? ClientIp { get; set; }

        /// <summary>用户代理</summary>
        [DisplayName("用户代理")]
        public string? UserAgent { get; set; }

        /// <summary>登录类型</summary>
        [DisplayName("登录类型")]
        public string? LoginType { get; set; } = "Password";

        /// <summary>记住我</summary>
        [DisplayName("记住我")]
        public bool RememberMe { get; set; } = false;
        #endregion

        #region 登录响应属性
        /// <summary>JWT令牌</summary>
        [DisplayName("访问令牌")]
        public string Token { get; set; } = string.Empty;

        /// <summary>用户信息</summary>
        [DisplayName("用户信息")]
        public BaseUser? User { get; set; }
        #endregion

        #region UI状态属性
        /// <summary>是否正在登录中</summary>
        [DisplayName("登录中")]
        public bool IsLoggingIn { get; set; }

        /// <summary>是否已登录成功</summary>
        [DisplayName("已登录")]
        public bool IsLoggedIn { get; set; }

        /// <summary>是否有保存的密码</summary>
        [DisplayName("有保存密码")]
        public bool HasSavedPassword { get; set; }

        /// <summary>API是否在线</summary>
        [DisplayName("API在线")]
        public bool IsApiOnline { get; set; }

        /// <summary>登录错误信息</summary>
        [DisplayName("错误信息")]
        public string? ErrorMessage { get; set; }

        /// <summary>登录状态信息</summary>
        [DisplayName("状态信息")]
        public string? StatusMessage { get; set; }
        #endregion

        #region 显示逻辑属性
        /// <summary>登录类型显示文本</summary>
        [DisplayName("登录方式")]
        public string LoginTypeDisplay => LoginType switch
        {
            "Password" => "密码登录",
            "WeChat" => "微信登录",
            "SmsCode" => "短信验证",
            "QrCode" => "二维码登录",
            "Fingerprint" => "指纹登录",
            "FaceRecognition" => "人脸识别",
            "TwoFactor" => "双因子认证",
            _ => "其他方式"
        };

        /// <summary>登录方式图标</summary>
        [DisplayName("登录图标")]
        public string LoginTypeIcon => LoginType switch
        {
            "Password" => "🔑",
            "WeChat" => "💬",
            "SmsCode" => "📱",
            "QrCode" => "📱",
            "Fingerprint" => "👆",
            "FaceRecognition" => "😊",
            "TwoFactor" => "🔐",
            _ => "🔧"
        };

        /// <summary>API状态显示文本</summary>
        [DisplayName("API状态")]
        public string ApiStatusDisplay => IsApiOnline ? "✅ API连接正常" : "❌ API服务不可用";

        /// <summary>用户显示名称</summary>
        [DisplayName("用户显示名")]
        public string UserDisplayName => User?.RealName ?? User?.Username ?? Username;

        /// <summary>角色显示文本</summary>
        [DisplayName("角色")]
        public string RoleDisplay => User?.Role.ToString() ?? "未知角色"; // GetDescription()方法不存在，使用ToString()替代

        /// <summary>是否可以登录</summary>
        [DisplayName("可登录")]
        public bool CanLogin => !IsLoggingIn && 
                                 !string.IsNullOrWhiteSpace(Username) && 
                                 !string.IsNullOrWhiteSpace(Password) && 
                                 IsApiOnline;

        /// <summary>登录按钮文本</summary>
        [DisplayName("登录按钮文本")]
        public string LoginButtonText => IsLoggingIn ? "登录中..." : "登录";

        /// <summary>登录状态颜色</summary>
        [DisplayName("状态颜色")]
        public string StatusColor => !string.IsNullOrEmpty(ErrorMessage) ? "Red" : 
                                     IsLoggedIn ? "Green" : 
                                     IsLoggingIn ? "Orange" : "Gray";

        /// <summary>安全等级文本</summary>
        [DisplayName("安全等级")]
        public string SecurityLevel => LoginType switch
        {
            "TwoFactor" => "🔒 高",
            "FaceRecognition" or "Fingerprint" => "🟡 中",
            "WeChat" or "SmsCode" => "🟠 普通",
            "Password" => "🔴 基础",
            _ => "❓ 未知"
        };
        #endregion

        #region 辅助方法
        /// <summary>
        /// 清除登录状态
        /// </summary>
        public void ClearLoginState()
        {
            Token = string.Empty;
            User = null;
            IsLoggedIn = false;
            IsLoggingIn = false;
            ErrorMessage = null;
            StatusMessage = null;
        }

        /// <summary>
        /// 设置登录成功状态
        /// </summary>
        public void SetLoginSuccess(string token, BaseUser user)
        {
            Token = token;
            User = user;
            IsLoggedIn = true;
            IsLoggingIn = false;
            ErrorMessage = null;
            StatusMessage = "登录成功";
        }

        /// <summary>
        /// 设置登录失败状态
        /// </summary>
        public void SetLoginFailure(string errorMessage)
        {
            Token = string.Empty;
            User = null;
            IsLoggedIn = false;
            IsLoggingIn = false;
            ErrorMessage = errorMessage;
            StatusMessage = null;
        }

        /// <summary>
        /// 验证登录信息
        /// </summary>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(Username))
                return (false, "请输入用户名");

            if (string.IsNullOrWhiteSpace(Password))
                return (false, "请输入密码");

            if (!IsApiOnline)
                return (false, "API服务不可用，请稍后重试");

            return (true, null);
        }
        #endregion
    }
}