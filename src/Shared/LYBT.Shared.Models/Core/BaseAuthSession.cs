using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Core {

    /// <summary>
    /// 认证会话基础模型 - 前后端共享核心字段
    /// 用于管理用户登录会话的生命周期和状态
    /// </summary>
    public class BaseAuthSession {

        /// <summary>会话唯一标识</summary>
        [DisplayName("会话ID")]
        public Guid Id { get; set; }

        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        [StringLength(32)]
        public string Username { get; set; } = string.Empty;

        /// <summary>用户ID</summary>
        [DisplayName("用户ID")]
        public Guid? UserId { get; set; }

        /// <summary>登录类型</summary>
        [DisplayName("登录类型")]
        public LoginType LoginType { get; set; } = LoginType.Password;

        /// <summary>登录时间</summary>
        [DisplayName("登录时间")]
        public DateTime LoginTime { get; set; }

        /// <summary>登出时间</summary>
        [DisplayName("登出时间")]
        public DateTime? LogoutTime { get; set; }

        /// <summary>客户端IP地址</summary>
        [DisplayName("客户端IP")]
        [StringLength(45)]
        public string? ClientIp { get; set; }

        /// <summary>用户代理字符串</summary>
        [DisplayName("用户代理")]
        [StringLength(512)]
        public string? UserAgent { get; set; }

        /// <summary>会话状态</summary>
        [DisplayName("会话状态")]
        public AuthSessionStatus Status { get; set; } = AuthSessionStatus.Active;

        /// <summary>最后活动时间</summary>
        [DisplayName("最后活动时间")]
        public DateTime? LastActivityTime { get; set; }

        /// <summary>会话持续时长（秒）</summary>
        [DisplayName("持续时长")]
        public int? DurationSeconds { get; set; }

        /// <summary>是否记住我</summary>
        [DisplayName("记住我")]
        public bool RememberMe { get; set; } = false;

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }
    }
}
