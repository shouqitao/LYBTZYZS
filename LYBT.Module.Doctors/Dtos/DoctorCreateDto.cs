using System;
using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums;

namespace LYBT.Module.Doctors.Dtos {

    /// <summary>
    /// 新增医生 DTO（仅包含医生专属字段）
    /// </summary>
    public class DoctorCreateDto {

        [Required(ErrorMessage = "用户ID不能为空")]
        public Guid UserId { get; set; }

        /// <summary>出生日期</summary>
        public DateTime? Birthday { get; set; }

        /// <summary>职称</summary>
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        /// <summary>执业证书号</summary>
        public string? LicenseNumber { get; set; }

        /// <summary>专长</summary>
        public string Specialty { get; set; } = string.Empty;

        /// <summary>医生当前状态</summary>
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        /// <summary>工作状态</summary>
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;

        /// <summary>姓名拼音码</summary>
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}