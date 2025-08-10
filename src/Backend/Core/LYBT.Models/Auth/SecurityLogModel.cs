using LYBT.Shared.Models.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Auth
{
    /// <summary>
    /// 安全日志数据模型 - 继承共享基础模型，数据库映射
    /// 用于全面记录系统安全事件，支持审计、分析和响应
    /// </summary>
    public class SecurityLogModel : BaseSecurityLog
    {
        /// <summary>异常堆栈跟踪</summary>
        [DisplayName("异常堆栈")]
        [StringLength(4000)]
        public string? StackTrace { get; set; }

        /// <summary>请求数据快照</summary>
        [DisplayName("请求数据")]
        [StringLength(2000)]
        public string? RequestData { get; set; }

        /// <summary>响应数据快照</summary>
        [DisplayName("响应数据")]
        [StringLength(2000)]
        public string? ResponseData { get; set; }

        /// <summary>HTTP方法</summary>
        [DisplayName("HTTP方法")]
        [StringLength(10)]
        public string? HttpMethod { get; set; }

        /// <summary>请求路径</summary>
        [DisplayName("请求路径")]
        [StringLength(500)]
        public string? RequestPath { get; set; }

        /// <summary>HTTP状态码</summary>
        [DisplayName("HTTP状态码")]
        public int? HttpStatusCode { get; set; }

        /// <summary>请求ID</summary>
        [DisplayName("请求ID")]
        [StringLength(64)]
        public string? RequestId { get; set; }

        /// <summary>处理时间（毫秒）</summary>
        [DisplayName("处理时间")]
        public long ProcessingTimeMs { get; set; } = 0;

        /// <summary>处理人员ID</summary>
        [DisplayName("处理人员")]
        public Guid? ProcessedBy { get; set; }

        /// <summary>处理时间</summary>
        [DisplayName("处理时间")]
        public DateTime? ProcessedTime { get; set; }

        /// <summary>处理备注</summary>
        [DisplayName("处理备注")]
        [StringLength(1000)]
        public string? ProcessingNotes { get; set; }

        /// <summary>是否已通知</summary>
        [DisplayName("已通知")]
        public bool IsNotified { get; set; } = false;

        /// <summary>通知时间</summary>
        [DisplayName("通知时间")]
        public DateTime? NotifiedTime { get; set; }

        /// <summary>通知方式</summary>
        [DisplayName("通知方式")]
        [StringLength(50)]
        public string? NotificationMethod { get; set; }

        /// <summary>关联事件数量</summary>
        [DisplayName("关联事件数")]
        public int RelatedEventsCount { get; set; } = 0;

        /// <summary>事件分类标签</summary>
        [DisplayName("分类标签")]
        [StringLength(200)]
        public string? CategoryTags { get; set; }

        /// <summary>风险评估分数（0-100）</summary>
        [DisplayName("风险评分")]
        public int RiskScore { get; set; } = 0;

        /// <summary>自动分析结果</summary>
        [DisplayName("自动分析")]
        [StringLength(1000)]
        public string? AutoAnalysisResult { get; set; }

        /// <summary>补救建议</summary>
        [DisplayName("补救建议")]
        [StringLength(1000)]
        public string? RemediationSuggestions { get; set; }

        /// <summary>是否需要升级</summary>
        [DisplayName("需要升级")]
        public bool RequiresEscalation { get; set; } = false;

        /// <summary>升级级别</summary>
        [DisplayName("升级级别")]
        public int EscalationLevel { get; set; } = 0;

        /// <summary>合规性标记</summary>
        [DisplayName("合规性标记")]
        [StringLength(200)]
        public string? ComplianceFlags { get; set; }

        /// <summary>数据保留到期时间</summary>
        [DisplayName("保留到期")]
        public DateTime? RetentionExpiry { get; set; }

        /// <summary>是否归档</summary>
        [DisplayName("已归档")]
        public bool IsArchived { get; set; } = false;

        /// <summary>归档时间</summary>
        [DisplayName("归档时间")]
        public DateTime? ArchivedTime { get; set; }
    }
}