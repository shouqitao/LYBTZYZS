using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    /// <summary>
    /// 处方更新DTO
    /// </summary>
    public class PrescriptionUpdateDto
    {
        /// <summary>
        /// 处方编号
        /// </summary>
        [DisplayName("处方编号")]
        [StringLength(50)]
        public string? PrescriptionNumber { get; set; }

        /// <summary>
        /// 诊断
        /// </summary>
        [DisplayName("诊断")]
        [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>
        /// 医嘱
        /// </summary>
        [DisplayName("医嘱")]
        [StringLength(500, ErrorMessage = "医嘱长度不能超过500个字符")]
        public string? Advice { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        /// <summary>
        /// 备注（Notes兼容）
        /// </summary>
        [DisplayName("备注")]
        [StringLength(500)]
        public string? Notes { get => Remark; set => Remark = value; }

        /// <summary>
        /// 原有的备注字段（兼容性）
        /// </summary>
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remarks { get; set; }

        /// <summary>
        /// 折扣
        /// </summary>
        [DisplayName("折扣")]
        [Range(0, 1, ErrorMessage = "折扣必须在0-1之间")]
        public decimal Discount { get; set; } = 1.0m;

        /// <summary>
        /// 处方项目列表
        /// </summary>
        public List<PrescriptionItemInputDto>? Items { get; set; }

        /// <summary>
        /// 剂数
        /// </summary>
        [DisplayName("剂数")]
        public int DosageCount { get; set; } = 1;

        /// <summary>
        /// 用法
        /// </summary>
        [DisplayName("用法")]
        [StringLength(200)]
        public string? Usage { get; set; }
    }
}
