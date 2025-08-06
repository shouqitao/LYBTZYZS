using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Doctors {

    /// <summary>
    /// 医生信息更新DTO（简化版）
    /// </summary>
    public class DoctorInfoUpdateDto {
        /// <summary>
        /// 专长
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Specialty { get; set; } = string.Empty;
        
        /// <summary>
        /// 职称
        /// </summary>
        [StringLength(50)]
        public string? Title { get; set; }
        
        /// <summary>
        /// 简介
        /// </summary>
        [StringLength(500)]
        public string? Introduction { get; set; }
    }

    /// <summary>
    /// 医生休息记录DTO
    /// </summary>
    public class DoctorRestRecordDto {
        public Guid Id { get; set; }
        public Guid DoctorId { get; set; }
        public string? DoctorName { get; set; }
        
        /// <summary>
        /// 休息日期
        /// </summary>
        public DateTime RestDate { get; set; }
        
        /// <summary>
        /// 是否全天休息
        /// </summary>
        public bool IsFullDay { get; set; } = true;
        
        /// <summary>
        /// 休息原因
        /// </summary>
        [StringLength(200)]
        public string? Reason { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        
        /// <summary>
        /// 操作人
        /// </summary>
        public string? OperatorName { get; set; }
    }

    /// <summary>
    /// 设置医生休息DTO
    /// </summary>
    public class SetDoctorRestDto {
        [Required]
        public DateTime Date { get; set; }
        
        /// <summary>
        /// 是否休息
        /// </summary>
        public bool IsRest { get; set; }
        
        /// <summary>
        /// 休息原因
        /// </summary>
        [StringLength(200)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 简化的医生基本信息DTO
    /// </summary>
    public class SimpleDoctorDto {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string? Title { get; set; }
        public decimal RegistrationFee { get; set; }
        public bool IsAvailable { get; set; }
    }
}