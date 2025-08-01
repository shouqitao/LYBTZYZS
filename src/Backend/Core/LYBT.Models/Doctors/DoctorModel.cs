using LYBT.Shared.Models.Enums;
using LYBT.Models.Users;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Doctors {

    /// <summary>
    /// 医生信息实体 - 医生基础信息管理，关联用户系统，支持软删除策略
    /// </summary>
    public class DoctorModel {

        /// <summary>
        /// 医生唯一标识（主键）
        /// </summary>
        [Key]
        [DisplayName("医生ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 医生性别
        /// </summary>
        [Required]
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>
        /// 医生年龄
        /// </summary>
        [DisplayName("年龄")]
        public int Age { get; set; } = 0;

        /// <summary>
        /// 医生职称
        /// </summary>
        [Required]
        [DisplayName("职称")]
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        /// <summary>
        /// 专科特长
        /// </summary>
        [StringLength(64)]
        [DisplayName("专科特长")]
        public string Specialty { get; set; } = string.Empty;

        /// <summary>
        /// 医生状态（启用/禁用，支持软删除策略）
        /// </summary>
        [Required]
        [DisplayName("医生状态")]
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        /// <summary>
        /// 工作状态（坐诊/休息/请假等）
        /// </summary>
        [Required]
        [DisplayName("工作状态")]
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注信息
        /// </summary>
        [StringLength(256)]
        [DisplayName("备注信息")]
        public string? Remark { get; set; }

        /// <summary>
        /// 出生日期
        /// </summary>
        [DisplayName("出生日期")]
        public DateTime? Birthday { get; set; }

        /// <summary>
        /// 执业证书编号
        /// </summary>
        [StringLength(32)]
        [DisplayName("执业证书编号")]
        public string? LicenseNumber { get; set; }

        /// <summary>
        /// 医生姓名拼音简码（用于快速搜索，从关联用户获取）
        /// </summary>
        [StringLength(32)]
        [DisplayName("拼音码")]
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>
        /// 医生姓名五笔码（用于快速搜索）
        /// </summary>
        [StringLength(32)]
        [DisplayName("五笔码")]
        public string WuBiCode { get; set; } = string.Empty;

        /// <summary>
        /// 医生联系电话
        /// </summary>
        [StringLength(32)]
        [DisplayName("联系电话")]
        public string? ContactNumber { get; set; }

        /// <summary>
        /// 关联用户ID（外键）
        /// </summary>
        [Required]
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>
        /// 关联的用户实体（导航属性）
        /// </summary>
        [Required]
        [DisplayName("关联用户")]
        public virtual UserModel User { get; set; } = null!;
    }
}