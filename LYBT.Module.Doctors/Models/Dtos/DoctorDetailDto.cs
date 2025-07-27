using LYBT.Common.Enums.Doctors;
using LYBT.Common.Enums.System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Doctors.Models.Dtos {

    /// <summary>
    /// 医生详情 DTO
    /// </summary>
    public class DoctorDetailDto {

        [DisplayName("医生ID")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "关联用户ID不能为空")]
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "性别不能为空")]
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        [Required(ErrorMessage = "出生日期不能为空")]
        [DisplayName("出生日期")]
        public DateTime Birthday { get; set; }

        [Required(ErrorMessage = "职称不能为空")]
        [DisplayName("职称")]
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        [StringLength(32, ErrorMessage = "执业证号长度不能超过32个字符")]
        [DisplayName("执业证号")]
        public string? LicenseNumber { get; set; }

        [Required(ErrorMessage = "专科不能为空")]
        [StringLength(64, ErrorMessage = "专科长度不能超过64个字符")]
        [DisplayName("专科")]
        public string Specialty { get; set; } = string.Empty;

        [DisplayName("在职状态")]
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        [DisplayName("工作状态")]
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;

        [StringLength(32, ErrorMessage = "拼音码长度不能超过32个字符")]
        [DisplayName("拼音码")]
        public string PinyinCode { get; set; } = string.Empty;

        [StringLength(256, ErrorMessage = "备注长度不能超过256个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        [Phone(ErrorMessage = "联系电话格式不正确")]
        [StringLength(32, ErrorMessage = "联系电话长度不能超过32个字符")]
        [DisplayName("联系电话")]
        public string? ContactNumber { get; set; }

        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; }

        // 关联的用户信息（只读）
        [DisplayName("用户名")]
        public string? UserName { get; set; }

        [DisplayName("真实姓名")]
        public string? RealName { get; set; }

        [Phone(ErrorMessage = "手机号格式不正确")]
        [DisplayName("手机号")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [DisplayName("邮箱")]
        public string? Email { get; set; }

        /// <summary>
        /// 计算年龄
        /// </summary>
        public int Age {
            get {
                var today = DateTime.Today;
                var age = today.Year - Birthday.Year;
                if (Birthday.Date > today.AddYears(-age))
                    age--;
                return age;
            }
        }
    }
}