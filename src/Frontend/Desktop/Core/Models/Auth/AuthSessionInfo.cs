using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.WPF.Client.Core.Models.Auth
{
    /// <summary>
    /// 认证会话显示信息模型 - 继承共享基础模型，UI显示专用
    /// 用于WPF界面中显示用户会话信息，包含格式化文本和UI状态
    /// </summary>
    public class AuthSessionInfo : BaseAuthSession
    {
        /// <summary>格式化的登录时间显示文本</summary>
        [DisplayName("登录时间文本")]
        public string FormattedLoginTime => LoginTime.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>会话持续时间显示文本</summary>
        [DisplayName("持续时间")]
        public string DurationText 
        {
            get
            {
                var duration = DateTime.Now - LoginTime;
                if (duration.TotalDays >= 1)
                    return $"{(int)duration.TotalDays}天 {duration.Hours}时{duration.Minutes}分";
                if (duration.TotalHours >= 1)
                    return $"{duration.Hours}时{duration.Minutes}分";
                return $"{duration.Minutes}分钟";
            }
        }

        /// <summary>会话状态显示文本</summary>
        [DisplayName("状态文本")]
        public string StatusText => Status switch
        {
            AuthSessionStatus.Active => "活跃",
            AuthSessionStatus.Expired => "已过期",
            AuthSessionStatus.LoggedOut => "已登出",
            AuthSessionStatus.Revoked => "已撤销",
            AuthSessionStatus.Locked => "已锁定",
            _ => "未知"
        };

        /// <summary>会话状态图标</summary>
        [DisplayName("状态图标")]
        public string StatusIcon => Status switch
        {
            AuthSessionStatus.Active => "✅",
            AuthSessionStatus.Expired => "⏰",
            AuthSessionStatus.LoggedOut => "🚪",
            AuthSessionStatus.Revoked => "🚫",
            AuthSessionStatus.Locked => "🔒",
            _ => "❓"
        };

        /// <summary>登录类型显示文本</summary>
        [DisplayName("登录方式文本")]
        public string LoginTypeText => LoginType switch
        {
            LoginType.Password => "密码登录",
            LoginType.WeChat => "微信登录",
            LoginType.SmsCode => "短信验证",
            LoginType.QrCode => "二维码登录",
            LoginType.Fingerprint => "指纹登录",
            LoginType.FaceRecognition => "人脸识别",
            LoginType.TwoFactor => "双因子认证",
            _ => "其他方式"
        };

        /// <summary>登录方式图标</summary>
        [DisplayName("登录方式图标")]
        public string LoginTypeIcon => LoginType switch
        {
            LoginType.Password => "🔑",
            LoginType.WeChat => "💬",
            LoginType.SmsCode => "📱",
            LoginType.QrCode => "📱",
            LoginType.Fingerprint => "👆",
            LoginType.FaceRecognition => "😊",
            LoginType.TwoFactor => "🔐",
            _ => "🔧"
        };

        /// <summary>会话是否需要注意（即将过期或异常）</summary>
        [DisplayName("需要注意")]
        public bool NeedsAttention
        {
            get
            {
                var now = DateTime.Now;
                var sessionDuration = now - LoginTime;
                
                // 会话超过8小时需要注意
                if (sessionDuration.TotalHours > 8)
                    return true;

                // 非活跃状态需要注意
                if (Status != AuthSessionStatus.Active)
                    return true;

                return false;
            }
        }

        /// <summary>会话安全评级文本</summary>
        [DisplayName("安全等级")]
        public string SecurityRatingText
        {
            get
            {
                var points = 0;

                // 基于登录类型评分
                points += LoginType switch
                {
                    LoginType.TwoFactor => 40,
                    LoginType.FaceRecognition => 35,
                    LoginType.Fingerprint => 30,
                    LoginType.WeChat => 25,
                    LoginType.SmsCode => 20,
                    LoginType.QrCode => 15,
                    LoginType.Password => 10,
                    _ => 5
                };

                // 基于会话时长扣分
                var duration = DateTime.Now - LoginTime;
                if (duration.TotalHours > 12) points -= 20;
                else if (duration.TotalHours > 8) points -= 10;

                // 基于状态评分
                if (Status != AuthSessionStatus.Active) points -= 30;

                return points switch
                {
                    >= 70 => "🔒 高",
                    >= 40 => "🟡 中",
                    >= 20 => "🟠 低",
                    _ => "🔴 风险"
                };
            }
        }

        /// <summary>是否显示刷新按钮</summary>
        [DisplayName("可刷新")]
        public bool CanRefresh => Status == AuthSessionStatus.Active && !RememberMe;

        /// <summary>是否显示撤销按钮</summary>
        [DisplayName("可撤销")]
        public bool CanRevoke => Status == AuthSessionStatus.Active;

        /// <summary>客户端信息简要显示</summary>
        [DisplayName("客户端简介")]
        public string ClientBrief
        {
            get
            {
                if (string.IsNullOrEmpty(UserAgent))
                    return "未知设备";

                // 简化UserAgent显示
                if (UserAgent.Contains("Windows"))
                    return "💻 Windows";
                if (UserAgent.Contains("Mac"))
                    return "💻 Mac";
                if (UserAgent.Contains("Mobile"))
                    return "📱 手机";
                if (UserAgent.Contains("Android"))
                    return "📱 Android";
                if (UserAgent.Contains("iPhone"))
                    return "📱 iPhone";

                return "💻 其他设备";
            }
        }

        /// <summary>IP地址显示（隐藏部分）</summary>
        [DisplayName("IP地址")]
        public string MaskedIpAddress
        {
            get
            {
                if (string.IsNullOrEmpty(ClientIp))
                    return "未知";

                var parts = ClientIp.Split('.');
                if (parts.Length == 4)
                    return $"{parts[0]}.{parts[1]}.xxx.xxx";

                return ClientIp.Length > 8 ? $"{ClientIp[..4]}****" : ClientIp;
            }
        }

        /// <summary>会话优先级（用于排序）</summary>
        [DisplayName("优先级")]
        public int Priority => Status switch
        {
            AuthSessionStatus.Active => 1,
            AuthSessionStatus.Expired => 2,
            AuthSessionStatus.Locked => 3,
            AuthSessionStatus.Revoked => 4,
            AuthSessionStatus.LoggedOut => 5,
            _ => 6
        };
    }
}