using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.TreatmentRoom {

    /// <summary>
    /// 治疗记录基础DTO
    /// </summary>
    public class TreatmentDto {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string? PatientName { get; set; }
        public Guid DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public Guid? RegistrationId { get; set; }
        public string TreatmentType { get; set; } = string.Empty; // 针灸、推拿、拔罐、电疗等
        public string Status { get; set; } = string.Empty; // Waiting、InProgress、Completed、Cancelled
        public DateTime CreateTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? TherapistName { get; set; }
        public decimal Duration { get; set; } // 治疗时长（分钟）
        public decimal Price { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// 治疗记录详情DTO
    /// </summary>
    public class TreatmentDetailDto : TreatmentDto {
        public List<TreatmentItemDto> Items { get; set; } = new();
        public string? Symptoms { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? TreatmentResult { get; set; }
        public string? NextVisitAdvice { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string? Remark { get; set; }
        public List<TreatmentImageDto> Images { get; set; } = new();
    }

    /// <summary>
    /// 创建治疗记录DTO
    /// </summary>
    public class TreatmentCreateDto {
        [Required]
        public Guid PatientId { get; set; }
        
        [Required]
        public string PatientName { get; set; } = string.Empty;
        
        [Required]
        public Guid DoctorId { get; set; }
        
        [Required]
        public string DoctorName { get; set; } = string.Empty;
        
        public Guid? RegistrationId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string TreatmentType { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Symptoms { get; set; }
        
        [StringLength(1000)]
        public string? TreatmentPlan { get; set; }
        
        [Range(0, 10000)]
        public decimal Price { get; set; }
        
        [StringLength(200)]
        public string? Notes { get; set; }
        
        public List<TreatmentItemCreateDto> Items { get; set; } = new();
    }

    /// <summary>
    /// 更新治疗记录DTO
    /// </summary>
    public class TreatmentUpdateDto {
        [Required]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? TreatmentResult { get; set; }
        
        [StringLength(500)]
        public string? NextVisitAdvice { get; set; }
        
        [StringLength(200)]
        public string? Notes { get; set; }
        
        public List<TreatmentItemCreateDto> Items { get; set; } = new();
    }

    /// <summary>
    /// 治疗项目DTO
    /// </summary>
    public class TreatmentItemDto {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = string.Empty; // 项目名称
        public string ItemType { get; set; } = string.Empty; // 项目类型
        public decimal Quantity { get; set; } // 数量
        public string Unit { get; set; } = string.Empty; // 单位
        public decimal UnitPrice { get; set; } // 单价
        public decimal TotalPrice { get; set; } // 总价
        public string? Description { get; set; }
    }

    /// <summary>
    /// 创建治疗项目DTO
    /// </summary>
    public class TreatmentItemCreateDto {
        [Required]
        [StringLength(100)]
        public string ItemName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string ItemType { get; set; } = string.Empty;
        
        [Range(0.1, 100)]
        public decimal Quantity { get; set; } = 1;
        
        [Required]
        [StringLength(10)]
        public string Unit { get; set; } = "次";
        
        [Range(0, 10000)]
        public decimal UnitPrice { get; set; }
        
        [StringLength(200)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 治疗图片DTO
    /// </summary>
    public class TreatmentImageDto {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ImageType { get; set; } = string.Empty; // Before、After、During
        public DateTime CreateTime { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// 治疗队列DTO
    /// </summary>
    public class TreatmentQueueDto {
        public Guid Id { get; set; }
        public string QueueNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string TreatmentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public TimeSpan WaitingTime => DateTime.Now - CreateTime;
        public int QueuePosition { get; set; }
    }

    /// <summary>
    /// 理疗室状态DTO
    /// </summary>
    public class TreatmentRoomStatusDto {
        public int RoomNumber { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Available、Occupied、Maintenance
        public string? CurrentPatientName { get; set; }
        public string? CurrentTreatmentType { get; set; }
        public DateTime? CurrentStartTime { get; set; }
        public string? TherapistName { get; set; }
    }

    /// <summary>
    /// 治疗统计DTO
    /// </summary>
    public class TreatmentStatisticsDto {
        public int TotalTreatments { get; set; }
        public int WaitingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrice { get; set; }
        public double AverageDuration { get; set; }
        public Dictionary<string, int> TreatmentTypeStats { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// 今日治疗统计DTO
    /// </summary>
    public class TodayTreatmentStatDto {
        public int TotalCount { get; set; }
        public int WaitingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageDuration { get; set; }
        public Dictionary<string, int> TreatmentTypeCount { get; set; } = new();
        public List<TreatmentRoomStatusDto> RoomStatus { get; set; } = new();
    }

    /// <summary>
    /// 治疗查询DTO
    /// </summary>
    public class TreatmentQueryDto {
        public Guid? PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public string? TreatmentType { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchKeyword { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string OrderBy { get; set; } = "CreateTime";
        public bool IsAscending { get; set; } = false;
    }

    /// <summary>
    /// 开始治疗DTO
    /// </summary>
    public class StartTreatmentDto {
        [Required]
        [StringLength(50)]
        public string TherapistName { get; set; } = string.Empty;
        
        public int? RoomNumber { get; set; }
        
        [StringLength(200)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// 完成治疗DTO
    /// </summary>
    public class CompleteTreatmentDto {
        [StringLength(1000)]
        public string? TreatmentResult { get; set; }
        
        [StringLength(500)]
        public string? NextVisitAdvice { get; set; }
        
        [Range(0, 1440)]
        public decimal? ActualDuration { get; set; }
        
        [StringLength(200)]
        public string? Notes { get; set; }
    }
}