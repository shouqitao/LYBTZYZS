using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Core {
    /// <summary>
    /// 医生基础模型 - 简化版（根据需求只保留核心字段）
    /// </summary>
    public class BaseDoctorModel {
        /// <summary>医生唯一标识</summary>
        [DisplayName("医生ID")]
        public Guid Id { get; set; }

        /// <summary>关联用户ID</summary>
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>医生姓名（必填）</summary>
        [Required]
        [DisplayName("医生姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>专长（必填）</summary>
        [Required]
        [DisplayName("专长")]
        public string Specialty { get; set; } = string.Empty;

        /// <summary>挂号费（必填）</summary>
        [Required]
        [DisplayName("挂号费")]
        public decimal RegistrationFee { get; set; }

        /// <summary>执业证书号（必填）</summary>
        [Required]
        [DisplayName("执业证书号")]
        public string LicenseNumber { get; set; } = string.Empty;

        /// <summary>拼音码（用于快速搜索）</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>联系电话（选填）</summary>
        [DisplayName("联系电话")]
        public string? ContactNumber { get; set; }

        /// <summary>简介（选填）</summary>
        [DisplayName("简介")]
        public string? Introduction { get; set; }

        /// <summary>医生状态</summary>
        [DisplayName("状态")]
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 状态显示文本（计算属性）
        /// </summary>
        [DisplayName("状态")]
        public string StatusDisplayName => Status.GetDescription();

        /// <summary>
        /// 是否可用（计算属性）
        /// </summary>
        [DisplayName("是否可用")]
        public bool IsActive => Status == DoctorStatus.Active;

        /// <summary>
        /// 医生完整信息（计算属性）
        /// </summary>
        [DisplayName("医生信息")]
        public string FullInfo => $"{Name} - {Specialty}";
    }
}