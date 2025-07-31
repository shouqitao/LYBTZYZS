using LYBT.Common.Enums.Patients;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Patients {

    /// <summary>
    /// 患者档案信息实体 - 诊所患者档案基础信息管理，支持软删除策略
    /// </summary>
    public class PatientModel {

        /// <summary>
        /// 患者档案唯一标识（主键）
        /// </summary>
        [Key]
        [DisplayName("患者档案ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 患者档案姓名
        /// </summary>
        [Required, StringLength(64)]
        [DisplayName("患者档案姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 姓名拼音简码（用于快速搜索）
        /// </summary>
        [StringLength(32)]
        [DisplayName("拼音码")]
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>
        /// 姓名五笔码（用于快速搜索）
        /// </summary>
        [StringLength(32)]
        [DisplayName("五笔码")]
        public string WuBiCode { get; set; } = string.Empty;

        /// <summary>
        /// 患者档案性别
        /// </summary>
        [Required]
        [DisplayName("性别")]
        public Gender Gender { get; set; }

        /// <summary>
        /// 出生日期
        /// </summary>
        [DisplayName("出生日期")]
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// 患者档案年龄（可自动计算或手工录入）
        /// </summary>
        [DisplayName("年龄")]
        public int? Age { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [Required, StringLength(20)]
        [DisplayName("联系电话")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// 身份证号码
        /// </summary>
        [Required, StringLength(32)]
        [DisplayName("身份证号")]
        public string IDNumber { get; set; } = string.Empty;

        /// <summary>
        /// 患者档案地址
        /// </summary>
        [StringLength(256)]
        [DisplayName("患者档案地址")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 患者档案状态（启用/禁用，支持软删除策略）
        /// </summary>
        [Required]
        [DisplayName("患者档案状态")]
        public PatientStatus Status { get; set; } = PatientStatus.Active;

        /// <summary>
        /// 禁用原因（软删除时记录原因）
        /// </summary>
        [StringLength(128)]
        [DisplayName("禁用原因")]
        public string DisableReason { get; set; } = string.Empty;

        /// <summary>
        /// 备注信息
        /// </summary>
        [StringLength(256)]
        [DisplayName("备注信息")]
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Required]
        [DisplayName("更新时间")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}