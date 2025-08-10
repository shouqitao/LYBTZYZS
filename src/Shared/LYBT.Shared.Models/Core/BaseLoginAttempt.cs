using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Core
{
    /// <summary>
    /// 登录尝试基础模型 - 前后端共享核心字段
    /// 用于记录和分析用户登录尝试的详细信息
    /// </summary>
    public class BaseLoginAttempt
    {
        /// <summary>尝试记录唯一标识</summary>
        [DisplayName("尝试ID")]
        public Guid Id { get; set; }

        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        [StringLength(32)]
        public string Username { get; set; } = string.Empty;

        /// <summary>用户ID（登录成功时）</summary>
        [DisplayName("用户ID")]
        public Guid? UserId { get; set; }

        /// <summary>尝试时间</summary>
        [DisplayName("尝试时间")]
        public DateTime AttemptTime { get; set; }

        /// <summary>是否成功</summary>
        [DisplayName("是否成功")]
        public bool IsSuccess { get; set; } = false;

        /// <summary>失败原因</summary>
        [DisplayName("失败原因")]
        [StringLength(200)]
        public string? FailureReason { get; set; }

        /// <summary>客户端IP地址</summary>
        [DisplayName("客户端IP")]
        [StringLength(45)]
        public string? ClientIp { get; set; }

        /// <summary>用户代理字符串</summary>
        [DisplayName("用户代理")]
        [StringLength(512)]
        public string? UserAgent { get; set; }

        /// <summary>登录类型</summary>
        [DisplayName("登录类型")]
        public LoginType LoginType { get; set; } = LoginType.Password;

        /// <summary>风险级别</summary>
        [DisplayName("风险级别")]
        public SecurityLevel RiskLevel { get; set; } = SecurityLevel.Low;

        /// <summary>地理位置（可选）</summary>
        [DisplayName("地理位置")]
        [StringLength(100)]
        public string? Location { get; set; }

        /// <summary>设备指纹（可选）</summary>
        [DisplayName("设备指纹")]
        [StringLength(100)]
        public string? DeviceFingerprint { get; set; }

        /// <summary>是否可疑活动</summary>
        [DisplayName("可疑活动")]
        public bool IsSuspicious { get; set; } = false;

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}