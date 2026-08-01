using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Auth
{

    /// <summary>
    /// 认证会话实体 - UltraThink v2.0架构简化版
    /// 用于基本用户登录会话管理，适合20人以下诊所使用
    /// </summary>
    [Table("AuthSessions")]
    public class AuthSession
    {

        /// <summary>会话ID</summary>
        [Key]
        [DisplayName("会话ID")]
        public Guid Id { get; set; }

        /// <summary>用户ID</summary>
        [Required]
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        /// <summary>会话令牌哈希</summary>
        [Required]
        [StringLength(256)]
        [DisplayName("会话令牌哈希")]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>登录时间</summary>
        [DisplayName("登录时间")]
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;

        /// <summary>登出时间</summary>
        [DisplayName("登出时间")]
        public DateTime? LogoutTime { get; set; }

        /// <summary>过期时间</summary>
        [DisplayName("过期时间")]
        public DateTime ExpiryTime { get; set; }

        /// <summary>IP地址</summary>
        [Required]
        [StringLength(45)]
        [DisplayName("IP地址")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>用户代理</summary>
        [StringLength(500)]
        [DisplayName("用户代理")]
        public string? UserAgent { get; set; }

        /// <summary>是否已撤销</summary>
        [DisplayName("已撤销")]
        public bool IsRevoked { get; set; } = false;

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>
        /// 创建新的认证会话。
        /// </summary>
        public static AuthSession Create(
            Guid userId,
            string tokenHash,
            DateTime expiryTime,
            string ipAddress,
            string? userAgent = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new ArgumentException("令牌哈希不能为空", nameof(tokenHash));
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP地址不能为空", nameof(ipAddress));

            return new AuthSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                LoginTime = DateTime.UtcNow,
                ExpiryTime = expiryTime,
                IpAddress = ipAddress.Trim(),
                UserAgent = userAgent?.Trim(),
                Status = CommonStatus.Enabled
            };
        }

        /// <summary>
        /// 登出会话。
        /// </summary>
        public void Logout()
        {
            if (IsRevoked)
                throw new InvalidOperationException("会话已被撤销");

            LogoutTime = DateTime.UtcNow;
            Status = CommonStatus.Disabled;
        }

        /// <summary>
        /// 撤销会话（强制登出）。
        /// </summary>
        public void Revoke()
        {
            IsRevoked = true;
            LogoutTime = DateTime.UtcNow;
            Status = CommonStatus.Disabled;
        }

        /// <summary>
        /// 检查会话是否有效。
        /// </summary>
        public bool IsValid()
        {
            return !IsRevoked
                && Status == CommonStatus.Enabled
                && LogoutTime == null
                && DateTime.UtcNow < ExpiryTime;
        }

        /// <summary>
        /// 检查会话是否已过期。
        /// </summary>
        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiryTime;
        }
    }
}


