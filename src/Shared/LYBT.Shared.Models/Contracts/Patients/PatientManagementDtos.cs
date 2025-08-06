using LYBT.Shared.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Patients {

    /// <summary>
    /// 患者就诊历史DTO
    /// </summary>
    public class PatientVisitHistoryDto {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int TotalVisits { get; set; }
        public DateTime? FirstVisitDate { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public List<VisitRecordDto> VisitRecords { get; set; } = new();
        
        /// <summary>
        /// 平均就诊间隔（天）
        /// </summary>
        public double AverageVisitInterval { get; set; }
    }

    /// <summary>
    /// 就诊记录DTO
    /// </summary>
    public class VisitRecordDto {
        public Guid Id { get; set; }
        public DateTime VisitDate { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string ChiefComplaint { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public string? Prescription { get; set; }
    }

    /// <summary>
    /// 患者导入DTO
    /// </summary>
    public class PatientImportDto {
        [Required]
        public string Name { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public int? Age { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? IdNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? AllergyHistory { get; set; }
    }

    /// <summary>
    /// 患者导入结果DTO
    /// </summary>
    public class PatientImportResultDto {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int DuplicateCount { get; set; }
        public List<string> FailedRecords { get; set; } = new();
        public List<string> DuplicateRecords { get; set; } = new();
        public string? ImportBatchId { get; set; }
    }

    /// <summary>
    /// 患者导出查询DTO
    /// </summary>
    public class PatientExportQueryDto {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Gender? Gender { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public bool IncludeInactive { get; set; }
        public string? ExportFormat { get; set; } = "Excel"; // Excel, CSV, PDF
    }

    /// <summary>
    /// 患者导出DTO
    /// </summary>
    public class PatientExportDto {
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? IdNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? AllergyHistory { get; set; }
        public int VisitCount { get; set; }
        public DateTime? LastVisitTime { get; set; }
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 患者标签DTO
    /// </summary>
    public class PatientTagDto {
        public Guid Id { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string? TagColor { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 患者高级搜索DTO
    /// </summary>
    public class PatientAdvancedSearchDto {
        // 基础信息
        public string? Name { get; set; }
        public string? PinYinCode { get; set; }
        public string? IdNumber { get; set; }
        public string? PhoneNumber { get; set; }
        
        // 人口统计
        public Gender? Gender { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public string? Occupation { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Ethnicity { get; set; }
        
        // 就诊信息
        public int? MinVisitCount { get; set; }
        public int? MaxVisitCount { get; set; }
        public DateTime? LastVisitFrom { get; set; }
        public DateTime? LastVisitTo { get; set; }
        
        // 档案信息
        public DateTime? CreateDateFrom { get; set; }
        public DateTime? CreateDateTo { get; set; }
        public bool? HasAllergyHistory { get; set; }
        public List<string>? Tags { get; set; }
        
        // 分页
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        
        // 排序
        public string? SortBy { get; set; } = "CreateTime";
        public bool IsDescending { get; set; } = true;
    }
}