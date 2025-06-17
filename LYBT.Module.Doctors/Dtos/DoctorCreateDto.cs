using System;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Doctors.Dtos {
    /// <summary>
    /// 新增医生 DTO
    /// </summary>
    public class DoctorCreateDto {
        /// <summary>医生姓名</summary>
        [Required(ErrorMessage = "姓名不能为空")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [Required(ErrorMessage = "性别不能为空")]
        public string Gender { get; set; } = string.Empty;

        /// <summary>出生日期</summary>
        [Required(ErrorMessage = "出生日期不能为空")]
        public DateTime Birthday { get; set; }

        /// <summary>联系电话</summary>
        [Required(ErrorMessage = "联系电话不能为空")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>姓名拼音码</summary>
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>执业证书号</summary>
        public string? LicenseNumber { get; set; }

        /// <summary>职称（如“主任医师”、“主治医师”）</summary>
        public string? Title { get; set; }

        /// <summary>医生当前状态（如“在职”、“离职”）</summary>
        public string Status { get; set; } = "在职";

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}
