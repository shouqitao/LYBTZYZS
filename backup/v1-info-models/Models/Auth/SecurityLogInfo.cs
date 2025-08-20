using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Desktop.Core.Models.Auth
{
    /// <summary>
    /// 安全日志显示信息模型 - 继承共享基础模型，UI显示专用
    /// 用于WPF界面中显示安全日志记录，包含格式化文本和严重性等级显示
    /// </summary>
    public class SecurityLogInfo : BaseSecurityLog
    {
        /// <summary>格式化的事件时间显示文本</summary>
        [DisplayName("事件时间文本")]
        public string FormattedEventTime => EventTime.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>事件时间相对显示（多久前）</summary>
        [DisplayName("相对时间")]
        public string RelativeTime
        {
            get
            {
                var diff = DateTime.Now - EventTime;
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

                return EventTime.ToString("MM-dd");
            }
        }

        /// <summary>事件类型显示文本</summary>
        [DisplayName("事件类型文本")]
        public string EventTypeText => EventType switch
        {
            AuthEventType.LoginSuccess => "登录成功",
            AuthEventType.LoginFailed => "登录失败",
            AuthEventType.Logout => "用户登出",
            AuthEventType.TokenRefresh => "令牌刷新",
            AuthEventType.TokenRevoked => "令牌撤销",
            AuthEventType.PasswordChanged => "密码变更",
            AuthEventType.AccountLocked => "账户锁定",
            AuthEventType.AccountUnlocked => "账户解锁",
            AuthEventType.PermissionDenied => "权限拒绝",
            AuthEventType.SuspiciousActivity => "可疑活动",
            AuthEventType.DataAccess => "数据访问",
            AuthEventType.DataModification => "数据修改",
            AuthEventType.SystemError => "系统错误",
            AuthEventType.SecurityAlert => "安全警报",
            AuthEventType.ComplianceViolation => "合规违规",
            _ => "其他事件"
        };

        /// <summary>事件类型图标</summary>
        [DisplayName("事件类型图标")]
        public string EventTypeIcon => EventType switch
        {
            AuthEventType.LoginSuccess => "✅",
            AuthEventType.LoginFailed => "❌",
            AuthEventType.Logout => "🚪",
            AuthEventType.TokenRefresh => "🔄",
            AuthEventType.TokenRevoked => "🚫",
            AuthEventType.PasswordChanged => "🔑",
            AuthEventType.AccountLocked => "🔒",
            AuthEventType.AccountUnlocked => "🔓",
            AuthEventType.PermissionDenied => "🛡️",
            AuthEventType.SuspiciousActivity => "⚠️",
            AuthEventType.DataAccess => "📊",
            AuthEventType.DataModification => "✏️",
            AuthEventType.SystemError => "🔥",
            AuthEventType.SecurityAlert => "🚨",
            AuthEventType.ComplianceViolation => "📋",
            _ => "❓"
        };

        /// <summary>安全级别显示文本</summary>
        [DisplayName("安全级别文本")]
        public string LevelText => Level switch
        {
            SecurityLevel.Low => "低",
            SecurityLevel.Medium => "中",
            SecurityLevel.High => "高",
            SecurityLevel.Critical => "严重",
            SecurityLevel.Emergency => "紧急",
            _ => "未知"
        };

        /// <summary>安全级别图标</summary>
        [DisplayName("安全级别图标")]
        public string LevelIcon => Level switch
        {
            SecurityLevel.Low => "🟢",
            SecurityLevel.Medium => "🟡",
            SecurityLevel.High => "🟠",
            SecurityLevel.Critical => "🔴",
            SecurityLevel.Emergency => "🚨",
            _ => "⚪"
        };

        /// <summary>安全级别颜色</summary>
        [DisplayName("安全级别颜色")]
        public string LevelColor => Level switch
        {
            SecurityLevel.Low => "#4CAF50",
            SecurityLevel.Medium => "#FFC107",
            SecurityLevel.High => "#FF9800",
            SecurityLevel.Critical => "#F44336",
            SecurityLevel.Emergency => "#D32F2F",
            _ => "#9E9E9E"
        };

        /// <summary>操作结果显示文本</summary>
        [DisplayName("操作结果文本")]
        public string ResultText => Result switch
        {
            OperationResult.Success => "成功",
            OperationResult.Failed => "失败",
            OperationResult.Warning => "警告",
            OperationResult.Error => "错误",
            OperationResult.Cancelled => "取消",
            OperationResult.Timeout => "超时",
            OperationResult.Unauthorized => "未授权",
            OperationResult.Forbidden => "被禁止",
            _ => "未知"
        };

        /// <summary>操作结果图标</summary>
        [DisplayName("操作结果图标")]
        public string ResultIcon => Result switch
        {
            OperationResult.Success => "✅",
            OperationResult.Failed => "❌",
            OperationResult.Warning => "⚠️",
            OperationResult.Error => "🔥",
            OperationResult.Cancelled => "⏹️",
            OperationResult.Timeout => "⏰",
            OperationResult.Unauthorized => "🔐",
            OperationResult.Forbidden => "🛡️",
            _ => "❓"
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

        /// <summary>用户信息显示</summary>
        [DisplayName("用户显示")]
        public string UserDisplay
        {
            get
            {
                if (!string.IsNullOrEmpty(Username))
                    return $"👤 {Username}";
                if (UserId.HasValue)
                    return $"👤 用户-{UserId.Value.ToString()[..8]}...";
                return "👤 匿名";
            }
        }

        /// <summary>是否需要立即关注</summary>
        [DisplayName("需要关注")]
        public bool RequiresAttention => Level >= SecurityLevel.High || 
                                       Result == OperationResult.Error || 
                                       RequiresNotification ||
                                       EventType == AuthEventType.SuspiciousActivity ||
                                       EventType == AuthEventType.SecurityAlert;

        /// <summary>处理状态显示文本</summary>
        [DisplayName("处理状态")]
        public string ProcessingStatusText
        {
            get
            {
                if (IsProcessed)
                    return "✅ 已处理";
                if (RequiresNotification)
                    return "📢 待通知";
                if (RequiresAttention)
                    return "⚠️ 待处理";
                return "📝 记录";
            }
        }

        /// <summary>受影响资源友好显示</summary>
        [DisplayName("资源")]
        public string ResourceDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(AffectedResource))
                    return "系统";

                // 简化资源路径显示
                if (AffectedResource.StartsWith("/api/"))
                    return $"📡 {AffectedResource.Replace("/api/", "")}";
                if (AffectedResource.Contains("User"))
                    return "👤 用户管理";
                if (AffectedResource.Contains("Patient"))
                    return "🏥 患者管理";
                if (AffectedResource.Contains("Herb"))
                    return "🌿 药材管理";

                return $"📄 {AffectedResource}";
            }
        }

        /// <summary>详细信息预览（限制长度）</summary>
        [DisplayName("详情预览")]
        public string DetailsPreview
        {
            get
            {
                if (string.IsNullOrEmpty(Details))
                    return string.Empty;

                return Details.Length > 100 ? $"{Details[..100]}..." : Details;
            }
        }

        /// <summary>事件优先级（用于排序）</summary>
        [DisplayName("优先级")]
        public int Priority => ((int)Level * 1000) + 
                              (RequiresAttention ? 500 : 0) + 
                              (!IsProcessed ? 100 : 0) + 
                              (int)(DateTime.MaxValue - EventTime).TotalMinutes;

        /// <summary>操作建议文本</summary>
        [DisplayName("建议操作")]
        public string SuggestedAction
        {
            get
            {
                if (IsProcessed)
                    return string.Empty;

                if (Level >= SecurityLevel.Emergency)
                    return "🚨 立即响应";
                if (Level >= SecurityLevel.Critical)
                    return "🔴 紧急处理";
                if (Level >= SecurityLevel.High)
                    return "🟠 优先处理";
                if (RequiresNotification)
                    return "📢 发送通知";

                return "📝 记录备案";
            }
        }

        /// <summary>事件分类标签（用于过滤和分组）</summary>
        [DisplayName("分类标签")]
        public List<string> CategoryTags
        {
            get
            {
                var tags = new List<string>();

                // 基于事件类型的标签
                if (EventType.ToString().Contains("Login"))
                    tags.Add("登录");
                if (EventType.ToString().Contains("Token"))
                    tags.Add("令牌");
                if (EventType.ToString().Contains("Account"))
                    tags.Add("账户");
                if (EventType.ToString().Contains("Data"))
                    tags.Add("数据");

                // 基于严重性的标签
                if (Level >= SecurityLevel.High)
                    tags.Add("高危");
                if (RequiresAttention)
                    tags.Add("关注");
                if (IsProcessed)
                    tags.Add("已处理");

                return tags;
            }
        }
    }
}