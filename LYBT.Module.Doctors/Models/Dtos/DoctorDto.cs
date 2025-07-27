using LYBT.Common.Enums.Doctors;
using LYBT.Common.Enums.System;
using System.ComponentModel;

namespace LYBT.Module.Doctors.Models.Dtos {

    /// <summary>
    /// 医生列表 DTO
    /// </summary>
    public class DoctorDto {

        [DisplayName("医生ID")]
        public Guid Id { get; set; }

        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        [DisplayName("出生日期")]
        public DateTime Birthday { get; set; }

        [DisplayName("职称")]
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        [DisplayName("执业证号")]
        public string? LicenseNumber { get; set; }

        [DisplayName("专科")]
        public string Specialty { get; set; } = string.Empty;

        [DisplayName("在职状态")]
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        [DisplayName("工作状态")]
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;

        [DisplayName("拼音码")]
        public string PinyinCode { get; set; } = string.Empty;

        [DisplayName("备注")]
        public string? Remark { get; set; }

        [DisplayName("联系电话")]
        public string? ContactNumber { get; set; }

        [DisplayName("性别")]
        public Gender Gender { get; set; }

        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; }

        // 用户信息（只读）
        [DisplayName("用户名")]
        public string? UserName { get; set; }

        [DisplayName("真实姓名")]
        public string? RealName { get; set; }

        [DisplayName("手机号")]
        public string? PhoneNumber { get; set; }

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