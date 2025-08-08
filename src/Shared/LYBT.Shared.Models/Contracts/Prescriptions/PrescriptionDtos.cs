using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Prescriptions
{

    /// <summary>
    /// 处方基础DTO
    /// </summary>
    public class PrescriptionDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string? PatientName { get; set; }
        public Guid DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? Diagnosis { get; set; }
        public int DosageCount { get; set; }
        public decimal SingleDosePrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TotalWeight { get; set; }
        public PrescriptionStatus Status { get; set; }
        public DateTime CreateTime { get; set; }
        public string? Advice { get; set; }
        public List<PrescriptionItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// 处方详情DTO
    /// </summary>
    public class PrescriptionDetailDto : PrescriptionDto
    {
        public string? FormulaSource { get; set; }
        public string? DuplicateWarning { get; set; }
        public string? MissingDrugWarning { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 创建处方DTO
    /// </summary>
    public class PrescriptionCreateDto
    {
        [Required]
        public Guid PatientId { get; set; }

        [Required]
        public Guid DoctorId { get; set; }

        /// <summary>看诊ID</summary>
        public Guid? ConsultationId { get; set; }

        [Required]
        [StringLength(500)]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>剂型</summary>
        [StringLength(50)]
        public string? DosageForm { get; set; }

        [Range(1, 30)]
        public int DosageCount { get; set; } = 7;

        /// <summary>剂数</summary>
        [Range(1, 100)]
        public int Quantity { get; set; } = 7;

        /// <summary>用法说明</summary>
        [StringLength(200)]
        public string? Usage { get; set; }

        /// <summary>总金额</summary>
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string? Advice { get; set; }

        [StringLength(100)]
        public string? FormulaSource { get; set; }

        public List<PrescriptionItemCreateDto> Items { get; set; } = new();

        [StringLength(200)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 编辑处方DTO
    /// </summary>
    public class PrescriptionEditDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Diagnosis { get; set; } = string.Empty;

        [Range(1, 30)]
        public int DosageCount { get; set; }

        [StringLength(500)]
        public string? Advice { get; set; }

        public List<PrescriptionItemCreateDto> Items { get; set; } = new();

        [StringLength(200)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 处方项目DTO
    /// </summary>
    public class PrescriptionItemDto
    {
        public Guid Id { get; set; }
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TotalWeight { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 创建处方项目DTO
    /// </summary>
    public class PrescriptionItemCreateDto
    {
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

        /// <summary>小计金额</summary>
        [Range(0, double.MaxValue)]
        public decimal Subtotal { get; set; }

        /// <summary>用法说明</summary>
        [StringLength(200)]
        public string? Usage { get; set; }

        /// <summary>备注（Note别名）</summary>
        [StringLength(200)]
        public string? Note { get; set; }

        [StringLength(100)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 快速处方DTO（用于快速保存）
    /// </summary>
    public class QuickPrescriptionDto
    {
        [Required]
        [StringLength(500)]
        public string Diagnosis { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Advice { get; set; }

        [Range(1, 30)]
        public int DosageCount { get; set; } = 7;
    }

    /// <summary>
    /// 处方统计DTO
    /// </summary>
    public class PrescriptionStatisticsDto
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int PendingCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public DateTime StatisticsTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 处方查询DTO
    /// </summary>
    public class PrescriptionQueryDto
    {
        public Guid? PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public PrescriptionStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchKeyword { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string OrderBy { get; set; } = "CreateTime";
        public bool IsAscending { get; set; } = false;
    }
}