using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Registration {

    /// <summary>
    /// 挂号基础DTO
    /// </summary>
    public class RegistrationDto {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public RegistrationType RegistrationType { get; set; }
        public RegistrationStatus Status { get; set; }
        public DateTime RegistrationTime { get; set; }
        public int QueueNumber { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 挂号详情DTO
    /// </summary>
    public class RegistrationDetailDto : RegistrationDto {
        public bool IsFromDoctor { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string? DoctorSpecialty { get; set; }
        public decimal RegistrationFee { get; set; }
    }

    /// <summary>
    /// 创建挂号DTO
    /// </summary>
    public class RegistrationCreateDto {
        [Required]
        public Guid PatientId { get; set; }
        
        [Required]
        public Guid DoctorId { get; set; }
        
        public RegistrationType RegistrationType { get; set; } = RegistrationType.Regular;
        
        [StringLength(200)]
        public string? Remark { get; set; }
        
        /// <summary>
        /// 患者姓名（用于快速创建）
        /// </summary>
        public string? PatientName { get; set; }
        
        /// <summary>
        /// 医生姓名（用于显示）
        /// </summary>
        public string? DoctorName { get; set; }
    }

    /// <summary>
    /// 编辑挂号DTO
    /// </summary>
    public class RegistrationEditDto {
        [Required]
        public Guid Id { get; set; }
        
        public RegistrationType RegistrationType { get; set; }
        
        [StringLength(200)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 医生挂号统计DTO
    /// </summary>
    public class DoctorRegistrationStatDto {
        public Guid DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public int TotalCount { get; set; }
        public int WaitingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int CanceledCount { get; set; }
        public decimal CompletionRate => TotalCount > 0 ? (decimal)CompletedCount / TotalCount * 100 : 0;
    }

    /// <summary>
    /// 挂号查询DTO
    /// </summary>
    public class RegistrationQueryDto {
        public Guid? PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public RegistrationStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchKeyword { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}