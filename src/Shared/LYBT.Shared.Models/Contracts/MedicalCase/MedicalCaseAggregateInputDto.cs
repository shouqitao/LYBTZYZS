using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Constants;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医案聚合根输入DTO - 统一保存诊断和处方
    /// OpenSpec: refactor-medicalcase-aggregate-crud (PERSIST-001, PERSIST-002)
    /// </summary>
    /// <remarks>
    /// 设计理念：
    /// 1. 作为聚合根，在单次事务中保存Consultation和Prescription
    /// 2. 支持"仅诊断无处方"场景（NeedsPrescription=false）
    /// 3. 简化前端保存逻辑，由工作区协调器统一收集数据
    /// </remarks>
    public class MedicalCaseAggregateInputDto
    {
        /// <summary>
        /// 医案ID（必填，用于更新现有医案）
        /// </summary>
        [Required(ErrorMessage = "医案ID不能为空")]
        [DisplayName("医案ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 医案备注
        /// </summary>
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "备注长度不能超过{1}个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// 编辑原因（审计用途，非当天本人修改时必填）
        /// </summary>
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "编辑原因长度不能超过{1}个字符")]
        [DisplayName("编辑原因")]
        public string? EditReason { get; set; }

        /// <summary>
        /// 诊断信息（嵌套）
        /// </summary>
        [DisplayName("诊断信息")]
        public ConsultationInputDto? Consultation { get; set; }

        /// <summary>
        /// 处方信息（嵌套）
        /// </summary>
        [DisplayName("处方信息")]
        public PrescriptionAggregateDto? Prescription { get; set; }
    }
}
