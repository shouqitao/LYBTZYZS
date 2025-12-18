using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 患者导出DTO
    /// </summary>
    public class PatientExportDto
    {

        /// <summary>患者编号</summary>
        [DisplayName("患者编号")]
        public string PatientCode { get; set; } = string.Empty;

        /// <summary>姓名</summary>
        [DisplayName("姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public string Gender { get; set; } = string.Empty;

        /// <summary>年龄</summary>
        [DisplayName("年龄")]
        public int Age { get; set; }

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public string BirthDate { get; set; } = string.Empty;

        /// <summary>身份证号</summary>
        [DisplayName("身份证号")]
        public string? IdCardNumber { get; set; }

        /// <summary>手机号码</summary>
        [DisplayName("手机号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>地址</summary>
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>紧急联系人姓名</summary>
        [DisplayName("紧急联系人姓名")]
        public string? EmergencyContactName { get; set; }

        /// <summary>紧急联系人电话</summary>
        [DisplayName("紧急联系人电话")]
        public string? EmergencyContactPhone { get; set; }

        /// <summary>首次就诊日期</summary>
        [DisplayName("首次就诊日期")]
        public string FirstVisitDate { get; set; } = string.Empty;

        /// <summary>最后就诊日期</summary>
        [DisplayName("最后就诊日期")]
        public string LastVisitDate { get; set; } = string.Empty;

        /// <summary>就诊次数</summary>
        [DisplayName("就诊次数")]
        public int VisitCount { get; set; }

        /// <summary>过敏史</summary>
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>慢性病史</summary>
        [DisplayName("慢性病史")]
        public string? ChronicDiseases { get; set; }

        /// <summary>最后就诊时间</summary>
        [DisplayName("最后就诊时间")]
        public string? LastVisitTime => LastVisitDate;
    }
}
