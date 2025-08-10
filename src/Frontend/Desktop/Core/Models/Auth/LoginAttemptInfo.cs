using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.WPF.Client.Core.Models.Auth
{
    /// <summary>
    /// 登录尝试显示信息模型 - 继承共享基础模型，UI显示专用
    /// 用于WPF界面中显示登录尝试记录，包含格式化文本和风险评估显示
    /// </summary>
    public class LoginAttemptInfo : BaseLoginAttempt
    {
        /// <summary>格式化的尝试时间显示文本</summary>
        [DisplayName("尝试时间文本")]
        public string FormattedAttemptTime => AttemptTime.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>尝试时间相对显示（多久前）</summary>
        [DisplayName("相对时间")]
        public string RelativeTime
        {
            get
            {
                var diff = DateTime.Now - AttemptTime;
                if (diff.TotalMinutes < 1)
                    return "刚刚";
                if (diff.TotalMinutes < 60)
                    return $"{(int)diff.TotalMinutes}分钟前";
                if (diff.TotalHours < 24)
                    return $"{(int)diff.TotalHours}小时前";
                if (diff.TotalDays < 7)
                    return $"{(int)diff.TotalDays}天前";
                if (diff.TotalDays < 30)
                    return $"{(int)(diff.TotalDays / 7)}周前";

                return AttemptTime.ToString("MM-dd");
            }
        }

        /// <summary>尝试结果显示文本</summary>
        [DisplayName("结果文本")]
        public string ResultText => IsSuccess ? "成功" : "失败";

        /// <summary>尝试结果图标</summary>
        [DisplayName("结果图标")]
        public string ResultIcon => IsSuccess ? "✅" : "❌";

        /// <summary>结果状态颜色（用于UI绑定）</summary>
        [DisplayName("结果颜色")]
        public string ResultColor => IsSuccess ? "#4CAF50" : "#F44336";

        /// <summary>登录类型显示文本</summary>
        [DisplayName("登录方式文本")]
        public string LoginTypeText => LoginType switch
        {
            LoginType.Password => "密码",
            LoginType.WeChat => "微信",
            LoginType.SmsCode => "短信",
            LoginType.QrCode => "扫码",
            LoginType.Fingerprint => "指纹",
            LoginType.FaceRecognition => "人脸",
            LoginType.TwoFactor => "双因子",
            _ => "其他"
        };

        /// <summary>风险级别显示文本</summary>
        [DisplayName("风险级别文本")]
        public string RiskLevelText => RiskLevel switch
        {
            SecurityLevel.Low => "低风险",
            SecurityLevel.Medium => "中风险",
            SecurityLevel.High => "高风险",
            SecurityLevel.Critical => "严重风险",
            SecurityLevel.Emergency => "紧急风险",
            _ => "未知"
        };

        /// <summary>风险级别图标</summary>
        [DisplayName("风险级别图标")]
        public string RiskLevelIcon => RiskLevel switch
        {
            SecurityLevel.Low => "🟢",
            SecurityLevel.Medium => "🟡",
            SecurityLevel.High => "🟠",
            SecurityLevel.Critical => "🔴",
            SecurityLevel.Emergency => "🚨",
            _ => "⚪"
        };

        /// <summary>风险级别颜色</summary>
        [DisplayName("风险颜色")]
        public string RiskLevelColor => RiskLevel switch
        {
            SecurityLevel.Low => "#4CAF50",
            SecurityLevel.Medium => "#FFC107",
            SecurityLevel.High => "#FF9800",
            SecurityLevel.Critical => "#F44336",
            SecurityLevel.Emergency => "#D32F2F",
            _ => "#9E9E9E"
        };

        /// <summary>客户端信息简要显示</summary>
        [DisplayName("客户端简介")]
        public string ClientBrief
        {
            get
            {
                if (string.IsNullOrEmpty(UserAgent))
                    return "未知";

                if (UserAgent.Contains("Windows"))
                    return "💻 Windows";
                if (UserAgent.Contains("Mac"))
                    return "💻 Mac";
                if (UserAgent.Contains("Mobile") || UserAgent.Contains("Android"))
                    return "📱 移动端";
                if (UserAgent.Contains("iPhone"))
                    return "📱 iPhone";

                return "💻 其他";
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
                    return $"{parts[0]}.{parts[1]}.*.* ";

                return ClientIp.Length > 8 ? $"{ClientIp[..4]}****" : ClientIp;
            }
        }

        /// <summary>失败原因友好显示</summary>
        [DisplayName("失败原因")]
        public string FriendlyFailureReason
        {
            get
            {
                if (IsSuccess || string.IsNullOrEmpty(FailureReason))
                    return string.Empty;

                return FailureReason switch
                {
                    "InvalidCredentials" => "用户名或密码错误",
                    "AccountLocked" => "账户已锁定",
                    "AccountDisabled" => "账户已禁用",
                    "TooManyAttempts" => "尝试次数过多",
                    "IPBlocked" => "IP地址被封锁",
                    "TokenExpired" => "令牌已过期",
                    _ => FailureReason
                };
            }
        }

        /// <summary>是否为可疑活动（用于突出显示）</summary>
        [DisplayName("可疑标记")]
        public bool ShouldHighlight => IsSuspicious || RiskLevel >= SecurityLevel.High || (!IsSuccess && AttemptTime > DateTime.Now.AddHours(-1));

        /// <summary>设备指纹简化显示</summary>
        [DisplayName("设备标识")]
        public string DeviceIdentifier
        {
            get
            {
                if (string.IsNullOrEmpty(DeviceFingerprint))
                    return "未知设备";

                return DeviceFingerprint.Length > 12 
                    ? $"设备-{DeviceFingerprint[..8]}..." 
                    : $"设备-{DeviceFingerprint}";
            }
        }

        /// <summary>地理位置友好显示</summary>
        [DisplayName("位置")]
        public string FriendlyLocation
        {
            get
            {
                if (string.IsNullOrEmpty(Location))
                    return "未知位置";

                // 如果包含经纬度格式，转换为友好显示
                if (Location.Contains(",") && Location.Contains("."))
                    return "📍 定位";

                return $"📍 {Location}";
            }
        }

        /// <summary>尝试优先级（用于排序）</summary>
        [DisplayName("优先级")]
        public int Priority => (IsSuspicious ? 1000 : 0) + 
                              ((int)RiskLevel * 100) + 
                              (IsSuccess ? 0 : 50) + 
                              (int)(DateTime.MaxValue - AttemptTime).TotalMinutes;

        /// <summary>操作建议文本</summary>
        [DisplayName("建议操作")]
        public string SuggestedAction
        {
            get
            {
                if (IsSuccess)
                    return string.Empty;

                if (IsSuspicious || RiskLevel >= SecurityLevel.High)
                    return "🔍 需要审查";

                if (RiskLevel >= SecurityLevel.Medium)
                    return "⚠️ 关注";

                return string.Empty;
            }
        }
    }
}