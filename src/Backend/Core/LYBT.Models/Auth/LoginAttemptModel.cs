using LYBT.Shared.Models.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Auth
{
    /// <summary>
    /// 登录尝试数据模型 - 继承共享基础模型，数据库映射
    /// 用于详细记录和分析每次登录尝试，支持安全审计和风险分析
    /// </summary>
    public class LoginAttemptModel : BaseLoginAttempt
    {
        /// <summary>服务器信息</summary>
        [DisplayName("服务器信息")]
        [StringLength(100)]
        public string? ServerInfo { get; set; }

        /// <summary>响应时间（毫秒）</summary>
        [DisplayName("响应时间")]
        public long ResponseTimeMs { get; set; } = 0;

        /// <summary>请求处理节点</summary>
        [DisplayName("处理节点")]
        [StringLength(50)]
        public string? ProcessingNode { get; set; }

        /// <summary>详细错误信息</summary>
        [DisplayName("详细错误")]
        [StringLength(1000)]
        public string? DetailedError { get; set; }

        /// <summary>额外数据（JSON格式）</summary>
        [DisplayName("额外数据")]
        [StringLength(2000)]
        public string? AdditionalData { get; set; }

        /// <summary>HTTP状态码</summary>
        [DisplayName("HTTP状态码")]
        public int? HttpStatusCode { get; set; }

        /// <summary>请求ID</summary>
        [DisplayName("请求ID")]
        [StringLength(64)]
        public string? RequestId { get; set; }

        /// <summary>会话ID（成功登录时）</summary>
        [DisplayName("关联会话")]
        public Guid? SessionId { get; set; }

        /// <summary>安全评分（0-100）</summary>
        [DisplayName("安全评分")]
        public int SecurityScore { get; set; } = 100;

        /// <summary>地理位置详情</summary>
        [DisplayName("地理位置详情")]
        [StringLength(300)]
        public string? GeoLocationDetails { get; set; }

        /// <summary>威胁指标</summary>
        [DisplayName("威胁指标")]
        [StringLength(500)]
        public string? ThreatIndicators { get; set; }

        /// <summary>是否被阻止</summary>
        [DisplayName("被阻止")]
        public bool IsBlocked { get; set; } = false;

        /// <summary>阻止原因</summary>
        [DisplayName("阻止原因")]
        [StringLength(200)]
        public string? BlockReason { get; set; }

        /// <summary>用户代理解析结果</summary>
        [DisplayName("UA解析结果")]
        [StringLength(300)]
        public string? UserAgentParsed { get; set; }

        /// <summary>是否需要审查</summary>
        [DisplayName("需要审查")]
        public bool RequiresReview { get; set; } = false;

        /// <summary>审查状态</summary>
        [DisplayName("审查状态")]
        public bool IsReviewed { get; set; } = false;

        /// <summary>审查人员ID</summary>
        [DisplayName("审查人员")]
        public Guid? ReviewedBy { get; set; }

        /// <summary>审查时间</summary>
        [DisplayName("审查时间")]
        public DateTime? ReviewTime { get; set; }

        /// <summary>审查备注</summary>
        [DisplayName("审查备注")]
        [StringLength(500)]
        public string? ReviewNotes { get; set; }
    }
}