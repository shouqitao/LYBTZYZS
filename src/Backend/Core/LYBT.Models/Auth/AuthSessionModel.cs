using LYBT.Shared.Models.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Auth
{
    /// <summary>
    /// 认证会话数据模型 - 继承共享基础模型，数据库映射
    /// 用于管理用户登录会话的完整生命周期，包含令牌管理和安全控制
    /// </summary>
    public class AuthSessionModel : BaseAuthSession
    {
        /// <summary>JWT令牌哈希（用于撤销验证）</summary>
        [DisplayName("JWT令牌哈希")]
        [StringLength(256)]
        public string? JwtTokenHash { get; set; }

        /// <summary>令牌过期时间</summary>
        [DisplayName("令牌过期时间")]
        public DateTime? TokenExpiryTime { get; set; }

        /// <summary>是否已撤销令牌</summary>
        [DisplayName("令牌已撤销")]
        public bool IsTokenRevoked { get; set; } = false;

        /// <summary>撤销原因</summary>
        [DisplayName("撤销原因")]
        [StringLength(200)]
        public string? RevokeReason { get; set; }

        /// <summary>撤销时间</summary>
        [DisplayName("撤销时间")]
        public DateTime? RevokeTime { get; set; }

        /// <summary>撤销操作者ID</summary>
        [DisplayName("撤销操作者")]
        public Guid? RevokedBy { get; set; }

        /// <summary>令牌刷新次数</summary>
        [DisplayName("刷新次数")]
        public int RefreshCount { get; set; } = 0;

        /// <summary>最后刷新时间</summary>
        [DisplayName("最后刷新时间")]
        public DateTime? LastRefreshTime { get; set; }

        /// <summary>原始刷新令牌哈希</summary>
        [DisplayName("刷新令牌哈希")]
        [StringLength(256)]
        public string? RefreshTokenHash { get; set; }

        /// <summary>会话扩展数据（JSON格式）</summary>
        [DisplayName("扩展数据")]
        [StringLength(1000)]
        public string? ExtendedData { get; set; }

        /// <summary>服务器信息</summary>
        [DisplayName("服务器信息")]
        [StringLength(100)]
        public string? ServerInfo { get; set; }

        /// <summary>地理位置信息</summary>
        [DisplayName("地理位置")]
        [StringLength(200)]
        public string? GeoLocation { get; set; }

        /// <summary>设备信息</summary>
        [DisplayName("设备信息")]
        [StringLength(200)]
        public string? DeviceInfo { get; set; }

        /// <summary>是否自动登出</summary>
        [DisplayName("自动登出")]
        public bool IsAutoLogout { get; set; } = false;

        /// <summary>异常标记</summary>
        [DisplayName("异常标记")]
        public bool HasAnomalies { get; set; } = false;

        /// <summary>异常描述</summary>
        [DisplayName("异常描述")]
        [StringLength(500)]
        public string? AnomaliesDescription { get; set; }
    }
}