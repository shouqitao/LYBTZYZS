using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Constants;

namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    /// <summary>
    /// 处方聚合DTO - 作为MedicalCaseAggregateInputDto的嵌套结构
    /// OpenSpec: refactor-medicalcase-aggregate-crud (PERSIST-002)
    /// </summary>
    /// <remarks>
    /// 设计理念：
    /// 1. NeedsPrescription标志控制是否需要处方
    /// 2. 当NeedsPrescription=false时，不创建空的处方记录
    /// 3. Items为空且NeedsPrescription=true时视为无效状态
    /// </remarks>
    public class PrescriptionAggregateDto
    {
        /// <summary>
        /// 是否需要开处方
        /// </summary>
        [DisplayName("是否开处方")]
        public bool NeedsPrescription { get; set; } = true;

        /// <summary>
        /// 剂数（默认7剂）
        /// </summary>
        [Range(ValidationConstants.PrescriptionDoseMinCount, ValidationConstants.PrescriptionDoseMaxCount,
            ErrorMessage = "剂数必须在{1}-{2}之间")]
        [DisplayName("剂数")]
        public int DosageCount { get; set; } = 7;

        /// <summary>
        /// 用法说明
        /// </summary>
        [StringLength(ValidationConstants.UsageMaxLength, ErrorMessage = "用法说明长度不能超过{1}个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>
        /// 医嘱/用药建议
        /// </summary>
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "医嘱长度不能超过{1}个字符")]
        [DisplayName("医嘱")]
        public string? Advice { get; set; }

        /// <summary>
        /// 验方来源（可选）
        /// </summary>
        [StringLength(ValidationConstants.UsageMaxLength, ErrorMessage = "验方来源长度不能超过{1}个字符")]
        [DisplayName("验方来源")]
        public string? FormulaSource { get; set; }

        /// <summary>
        /// 引用的验方名称列表，逗号分隔
        /// OpenSpec: refactor-medicalcase-aggregate-crud
        /// </summary>
        [StringLength(500, ErrorMessage = "引用验方名称长度不能超过500个字符")]
        [DisplayName("引用验方")]
        public string? ReferencedFormulas { get; set; }

        /// <summary>
        /// 主治/适应症
        /// </summary>
        [StringLength(500, ErrorMessage = "主治长度不能超过500个字符")]
        [DisplayName("主治")]
        public string? Indication { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "备注长度不能超过{1}个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// 折扣（默认1.0，无折扣）
        /// </summary>
        [Range(0, 1, ErrorMessage = "折扣必须在0-1之间")]
        [DisplayName("折扣")]
        public decimal Discount { get; set; } = 1.0m;

        /// <summary>
        /// 处方项目列表
        /// </summary>
        [DisplayName("处方项目")]
        public List<PrescriptionItemInputDto> Items { get; set; } = new();

        /// <summary>
        /// 处方ID（更新时使用，创建时由后端生成）
        /// </summary>
        [DisplayName("处方ID")]
        public Guid? Id { get; set; }
    }
}
