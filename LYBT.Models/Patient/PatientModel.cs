using LYBT.Common.Enums;
using LYBT.Common.Enums.Patient;
using System.ComponentModel.DataAnnotations;


namespace LYBT.Module.Patients.Models {
    /// <summary>
    /// 患者信息实体
    /// </summary>
    public class PatientModel {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Required, MaxLength(64)]
        public required string Name { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        [MaxLength(8)]
        public Gender Gender { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// 手机号，唯一
        /// </summary>
        [Required, MaxLength(20)]
        public required string PhoneNumber { get; set; }

        /// <summary>
        /// 身份证号，唯一
        /// </summary>
        [Required, MaxLength(32)]
        public required string IDNumber { get; set; }

        /// <summary>
        /// 家庭住址
        /// </summary>
        [MaxLength(256)]
        public required string Address { get; set; }

        /// <summary>
        /// 状态（激活/禁用）
        /// </summary>
        [Required]
        public PatientStatus Status { get; set; } = PatientStatus.Active;

        /// <summary>
        /// 禁用原因（可空，仅禁用时填写）
        /// </summary>
        [MaxLength(128)]
        public string DisableReason { get; set; } = string.Empty;

        /// <summary>
        /// 是否特殊患者（隐私保护，0-普通，1-特殊）
        /// </summary>
        public bool IsSpecial { get; set; } = false;

        /// <summary>
        /// 备注
        /// </summary>
        [MaxLength(256)]
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string PinyinCode { get; set; }
    }
}


