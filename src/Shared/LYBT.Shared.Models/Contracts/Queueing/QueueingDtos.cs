using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Queueing {

    /// <summary>
    /// 排队基础DTO
    /// </summary>
    public class QueueingDto {
        public Guid Id { get; set; }
        public Guid? RegistrationId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int QueueNumber { get; set; }
        public string QueueType { get; set; } = "普通";
        public DateTime QueueTime { get; set; }
        public QueueStatus Status { get; set; }
        public DateTime? ActualTime { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 排队详情DTO
    /// </summary>
    public class QueueingDetailDto : QueueingDto {
        public DateTime? EstimatedTime { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public int WaitingCount { get; set; }
        public double EstimatedWaitingMinutes { get; set; }
    }

    /// <summary>
    /// 创建排队DTO
    /// </summary>
    public class QueueingCreateDto {
        public Guid? RegistrationId { get; set; }
        
        [Required]
        public Guid PatientId { get; set; }
        
        [Required]
        public string PatientName { get; set; } = string.Empty;
        
        [Required]
        public Guid DoctorId { get; set; }
        
        [Required]
        public string DoctorName { get; set; } = string.Empty;
        
        public string QueueType { get; set; } = "普通";
        
        [StringLength(200)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 编辑排队DTO
    /// </summary>
    public class QueueingEditDto {
        [Required]
        public Guid Id { get; set; }
        
        public string QueueType { get; set; } = "普通";
        
        [StringLength(200)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 排队统计DTO
    /// </summary>
    public class QueueStatisticsDto {
        public int TotalCount { get; set; }
        public int WaitingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int MissedCount { get; set; }
        public int CancelledCount { get; set; }
        public double AverageWaitingMinutes { get; set; }
        public DateTime StatisticsTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 叫号显示DTO
    /// </summary>
    public class CallDisplayDto {
        public int QueueNumber { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public DateTime CallTime { get; set; }
        public string DisplayText => $"请 {QueueNumber} 号 {PatientName} 到 {Room} 就诊";
    }

    /// <summary>
    /// 排队查询DTO
    /// </summary>
    public class QueueQueryDto {
        public Guid? DoctorId { get; set; }
        public QueueStatus? Status { get; set; }
        public DateTime? Date { get; set; }
        public string? SearchKeyword { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string OrderBy { get; set; } = "QueueNumber";
        public bool IsAscending { get; set; } = true;
    }

    /// <summary>
    /// 排队位置DTO
    /// </summary>
    public class QueuePositionDto {
        public Guid QueueId { get; set; }
        public int QueueNumber { get; set; }
        public int CurrentPosition { get; set; }
        public int WaitingCount { get; set; }
        public double EstimatedWaitingMinutes { get; set; }
        public string Message => WaitingCount > 0 
            ? $"您前面还有 {WaitingCount} 位患者，预计等待 {EstimatedWaitingMinutes:F0} 分钟" 
            : "即将为您叫号";
    }
}