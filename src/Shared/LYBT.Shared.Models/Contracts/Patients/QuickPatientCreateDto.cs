using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Patients
{

    /// <summary>
    /// 快速创建患者档案DTO (医生可以跳过挂号直接创建患者档案并发起看诊)
    /// </summary>
    public class QuickPatientCreateDto
    {

        /// <summary>
        /// 患者档案姓名
        /// </summary>
        [DisplayName("患者档案姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 性别
        /// </summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [DisplayName("年龄")]
        public int? Age { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [DisplayName("联系电话")]
        public string? Phone { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        [DisplayName("手机号")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 身份证号
        /// </summary>
        [DisplayName("身份证号")]
        public string? IDNumber { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>
        /// 主诉
        /// </summary>
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }
    }
}