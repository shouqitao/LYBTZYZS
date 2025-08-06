using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Pharmacy {

    /// <summary>
    /// 药房单基础DTO
    /// </summary>
    public class PharmacyDto {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string? PatientName { get; set; }
        public Guid DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public Guid? PrescriptionId { get; set; }
        public Guid? MedicalCaseId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public DateTime? DispensingTime { get; set; }
        public DateTime? DispenseTime { get; set; }
        public string? DispensingStaff { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 药房单详情DTO
    /// </summary>
    public class PharmacyDetailDto : PharmacyDto {
        public List<PharmacyItemDto> HerbItems { get; set; } = new();
        public DateTime? UpdateTime { get; set; }
        public string? FormulaSource { get; set; }
        public string? DuplicateWarning { get; set; }
        public string? MissingDrugWarning { get; set; }
    }

    /// <summary>
    /// 创建药房单DTO
    /// </summary>
    public class PharmacyCreateDto {
        [Required]
        public Guid PatientId { get; set; }
        
        [Required]
        public string PatientName { get; set; } = string.Empty;
        
        [Required]
        public Guid DoctorId { get; set; }
        
        [Required]
        public string DoctorName { get; set; } = string.Empty;
        
        public Guid? PrescriptionId { get; set; }
        public Guid? MedicalCaseId { get; set; }
        
        [StringLength(200)]
        public string? Remark { get; set; }
        
        public List<PharmacyItemCreateDto> HerbItems { get; set; } = new();
    }

    /// <summary>
    /// 更新药房单DTO
    /// </summary>
    public class PharmacyEditDto {
        [Required]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 药房单项目DTO
    /// </summary>
    public class PharmacyItemDto {
        public Guid Id { get; set; }
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal ActualQuantity { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 创建药房单项目DTO
    /// </summary>
    public class PharmacyItemCreateDto {
        [Required]
        public Guid HerbId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string HerbName { get; set; } = string.Empty;
        
        [Range(0.1, 1000)]
        public decimal Quantity { get; set; }
        
        [Required]
        [StringLength(10)]
        public string Unit { get; set; } = "g";
        
        [Range(0, 10000)]
        public decimal UnitPrice { get; set; }
        
        [StringLength(100)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 药房排队DTO
    /// </summary>
    public class PharmacyQueueDto {
        public Guid Id { get; set; }
        public string QueueNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public TimeSpan WaitingTime => DateTime.Now - CreateTime;
    }

    /// <summary>
    /// 库存检查结果DTO
    /// </summary>
    public class StockCheckResultDto {
        public bool HasSufficientStock { get; set; }
        public List<StockShortageDto> ShortageItems { get; set; } = new();
    }

    /// <summary>
    /// 库存不足项目DTO
    /// </summary>
    public class StockShortageDto {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal ShortageQuantity => RequiredQuantity - AvailableQuantity;
        public string Unit { get; set; } = string.Empty;
    }

    /// <summary>
    /// 药房统计DTO
    /// </summary>
    public class PharmacyStatisticsDto {
        public int TotalPrescriptions { get; set; }
        public int PendingCount { get; set; }
        public int DispensedCount { get; set; }
        public int CancelledCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// 今日药房统计DTO
    /// </summary>
    public class PharmacyTodayStatDto {
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int DispensingCount { get; set; }
        public int DispensedCount { get; set; }
        public int IssuedCount { get; set; }
        public int CancelledCount { get; set; }
        public double AverageDispenseTime { get; set; }
    }

    /// <summary>
    /// 药材配置明细DTO
    /// </summary>
    public class HerbDispenseDetailDto {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    /// <summary>
    /// 药材配置结果DTO
    /// </summary>
    public class HerbDispenseResultDto {
        public Guid HerbId { get; set; }
        public decimal ActualQuantity { get; set; }
        public string? Notes { get; set; }
        public bool IsDispensed { get; set; } = true;
    }

    /// <summary>
    /// 药房查询DTO
    /// </summary>
    public class PharmacyQueryDto {
        public Guid? PatientId { get; set; }
        public Guid? DoctorId { get; set; }
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
    /// 药房配药请求DTO
    /// </summary>
    public class PharmacyDispenseDto
    {
        /// <summary>处方ID</summary>
        public Guid PrescriptionId { get; set; }
        
        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }
        
        /// <summary>配药备注</summary>
        public string Notes { get; set; } = "";
        
        /// <summary>配药人ID</summary>
        public Guid? PharmacistId { get; set; }
        
        /// <summary>配药人姓名</summary>
        public string? PharmacistName { get; set; }
    }

}