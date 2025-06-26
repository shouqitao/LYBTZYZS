using LYBT.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Doctors.Dtos {

    /// <summary>
    /// 编辑医生 DTO
    /// </summary>
    public class DoctorEditDto {

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>医生姓名</summary>
        [Required(ErrorMessage = "姓名不能为空")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [Required(ErrorMessage = "性别不能为空")]
        public Gender Gender { get; set; } = Gender.Unknown;

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

        /// <summary>职称</summary>
        public DoctorTitle Title { get; set; }

        /// <summary>医生当前状态</summary>
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        /// <summary>备注</summary>
        public string Remark { get; set; } = string.Empty;
    }
}