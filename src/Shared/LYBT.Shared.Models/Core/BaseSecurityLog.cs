using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Core
{
    /// <summary>
    /// 安全日志基础模型 - 前后端共享核心字段
    /// 用于记录系统安全相关事件和审计信息
    /// </summary>
    public class BaseSecurityLog
    {
        /// <summary>日志记录唯一标识</summary>
        [DisplayName("日志ID")]
        public Guid Id { get; set; }

        /// <summary>事件类型</summary>
        [DisplayName("事件类型")]
        public AuthEventType EventType { get; set; }

        /// <summary>事件描述</summary>
        [DisplayName("事件描述")]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>事件时间</summary>
        [DisplayName("事件时间")]
        public DateTime EventTime { get; set; }

        /// <summary>用户ID（如果相关）</summary>
        [DisplayName("用户ID")]
        public Guid? UserId { get; set; }

        /// <summary>用户名（如果相关）</summary>
        [DisplayName("用户名")]
        [StringLength(32)]
        public string? Username { get; set; }

        /// <summary>客户端IP地址</summary>
        [DisplayName("客户端IP")]
        [StringLength(45)]
        public string? ClientIp { get; set; }

        /// <summary>用户代理字符串</summary>
        [DisplayName("用户代理")]
        [StringLength(512)]
        public string? UserAgent { get; set; }

        /// <summary>安全级别</summary>
        [DisplayName("安全级别")]
        public SecurityLevel Level { get; set; } = SecurityLevel.Low;

        /// <summary>受影响的资源</summary>
        [DisplayName("受影响资源")]
        [StringLength(200)]
        public string? AffectedResource { get; set; }

        /// <summary>操作结果</summary>
        [DisplayName("操作结果")]
        public OperationResult Result { get; set; } = OperationResult.Success;

        /// <summary>详细信息（JSON格式）</summary>
        [DisplayName("详细信息")]
        [StringLength(2000)]
        public string? Details { get; set; }

        /// <summary>会话ID（如果相关）</summary>
        [DisplayName("会话ID")]
        public Guid? SessionId { get; set; }

        /// <summary>是否需要通知</summary>
        [DisplayName("需要通知")]
        public bool RequiresNotification { get; set; } = false;

        /// <summary>是否已处理</summary>
        [DisplayName("已处理")]
        public bool IsProcessed { get; set; } = false;

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}