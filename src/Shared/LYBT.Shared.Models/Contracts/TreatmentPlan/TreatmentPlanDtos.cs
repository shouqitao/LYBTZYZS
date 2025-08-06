using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.TreatmentPlan
{
    /// <summary>
    /// 治疗方案基础DTO
    /// </summary>
    public class TreatmentPlanDto
    {
        public Guid Id { get; set; }
        public Guid MedicalCaseId { get; set; }
        public Guid PatientId { get; set; }
        public string? PatientName { get; set; }
        public Guid DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string TreatmentObjective { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public int PrescriptionCount { get; set; }
        public int TreatmentItemCount { get; set; }
    }

    /// <summary>
    /// 治疗方案详情DTO
    /// </summary>
    public class TreatmentPlanDetailDto : TreatmentPlanDto
    {
        public string? TreatmentPrinciple { get; set; }
        public string? Prognosis { get; set; }
        public string? Precautions { get; set; }
        public string? FollowUpPlan { get; set; }
        public List<TreatmentPlanPrescriptionDto> Prescriptions { get; set; } = new();
        public List<TreatmentPlanItemDto> TreatmentItems { get; set; } = new();
        public string? Remark { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Duration { get; set; } // 疗程天数
    }

    /// <summary>
    /// 治疗方案处方关联DTO
    /// </summary>
    public class TreatmentPlanPrescriptionDto
    {
        public Guid Id { get; set; }
        public Guid PrescriptionId { get; set; }
        public string PrescriptionName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int DosageCount { get; set; }
        public string Instructions { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public bool IsPrimary { get; set; } // 是否为主要处方
    }

    /// <summary>
    /// 治疗方案项DTO
    /// </summary>
    public class TreatmentPlanItemDto
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty; // 理疗、康复、手术等
        public int Frequency { get; set; } // 频次
        public string FrequencyUnit { get; set; } = string.Empty; // 次/天、次/周等
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Requirements { get; set; }
        public string Status { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// 创建治疗方案DTO
    /// </summary>
    public class TreatmentPlanCreateDto
    {
        [Required]
        public Guid MedicalCaseId { get; set; }

        [Required]
        public Guid DoctorId { get; set; }

        [Required]
        [StringLength(100)]
        public string PlanName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string TreatmentObjective { get; set; } = string.Empty;

        [StringLength(500)]
        public string? TreatmentPrinciple { get; set; }

        [StringLength(500)]
        public string? Prognosis { get; set; }

        [StringLength(1000)]
        public string? Precautions { get; set; }

        [StringLength(500)]
        public string? FollowUpPlan { get; set; }

        [StringLength(200)]
        public string? Remark { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(1, 365)]
        public int Duration { get; set; } = 7;

        public List<TreatmentPlanPrescriptionCreateDto> Prescriptions { get; set; } = new();
        public List<TreatmentPlanItemCreateDto> TreatmentItems { get; set; } = new();
    }

    /// <summary>
    /// 创建治疗方案处方关联DTO
    /// </summary>
    public class TreatmentPlanPrescriptionCreateDto
    {
        [Required]
        public Guid PrescriptionId { get; set; }

        public bool IsPrimary { get; set; } = false;
    }

    /// <summary>
    /// 创建治疗方案项DTO
    /// </summary>
    public class TreatmentPlanItemCreateDto
    {
        [Required]
        [StringLength(100)]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ItemType { get; set; } = string.Empty;

        [Required]
        [Range(1, 100)]
        public int Frequency { get; set; }

        [Required]
        [StringLength(20)]
        public string FrequencyUnit { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 100000)]
        public decimal UnitPrice { get; set; }

        [StringLength(500)]
        public string? Requirements { get; set; }

        public int SortOrder { get; set; } = 0;
    }

    /// <summary>
    /// 更新治疗方案DTO
    /// </summary>
    public class TreatmentPlanUpdateDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string PlanName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string TreatmentObjective { get; set; } = string.Empty;

        [StringLength(500)]
        public string? TreatmentPrinciple { get; set; }

        [StringLength(500)]
        public string? Prognosis { get; set; }

        [StringLength(1000)]
        public string? Precautions { get; set; }

        [StringLength(500)]
        public string? FollowUpPlan { get; set; }

        [StringLength(200)]
        public string? Remark { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(1, 365)]
        public int Duration { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        public List<TreatmentPlanPrescriptionUpdateDto> Prescriptions { get; set; } = new();
        public List<TreatmentPlanItemUpdateDto> TreatmentItems { get; set; } = new();
    }

    /// <summary>
    /// 更新治疗方案处方关联DTO
    /// </summary>
    public class TreatmentPlanPrescriptionUpdateDto
    {
        public Guid? Id { get; set; }

        [Required]
        public Guid PrescriptionId { get; set; }

        public bool IsPrimary { get; set; }
    }

    /// <summary>
    /// 更新治疗方案项DTO
    /// </summary>
    public class TreatmentPlanItemUpdateDto
    {
        public Guid? Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ItemType { get; set; } = string.Empty;

        [Required]
        [Range(1, 100)]
        public int Frequency { get; set; }

        [Required]
        [StringLength(20)]
        public string FrequencyUnit { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 100000)]
        public decimal UnitPrice { get; set; }

        [StringLength(500)]
        public string? Requirements { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        public int SortOrder { get; set; }
    }

    /// <summary>
    /// 治疗方案查询DTO
    /// </summary>
    public class TreatmentPlanQueryDto
    {
        public Guid? PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public Guid? MedicalCaseId { get; set; }
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
    /// 治疗方案统计DTO
    /// </summary>
    public class TreatmentPlanStatisticsDto
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalAmount { get; set; }
        public Dictionary<string, int> PlanTypeStats { get; set; } = new();
        public Dictionary<string, int> StatusStats { get; set; } = new();
        public Dictionary<string, decimal> DoctorAmountStats { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// 治疗方案执行记录DTO
    /// </summary>
    public class TreatmentExecutionRecordDto
    {
        public Guid Id { get; set; }
        public Guid TreatmentPlanId { get; set; }
        public Guid? PrescriptionId { get; set; }
        public Guid? TreatmentItemId { get; set; }
        public string ExecutionType { get; set; } = string.Empty; // 处方执行、理疗执行等
        public DateTime ExecutionDate { get; set; }
        public string ExecutorName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string? Feedback { get; set; }
        public string? NextSteps { get; set; }
    }

    /// <summary>
    /// 治疗方案模板DTO
    /// </summary>
    public class TreatmentPlanTemplateDto
    {
        public Guid Id { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string DiseaseCategory { get; set; } = string.Empty;
        public string TreatmentObjective { get; set; } = string.Empty;
        public string? TreatmentPrinciple { get; set; }
        public List<string> CommonPrescriptions { get; set; } = new();
        public List<string> CommonTreatmentItems { get; set; } = new();
        public bool IsShared { get; set; }
        public Guid? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreateTime { get; set; }
        public int UsageCount { get; set; }
    }
}