using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医案完整聚合创建输入DTO - 包含诊疗和可选处方
    /// 作为聚合根统一管理整个诊疗流程
    /// Epic #1961: 使用统一的 MedicalCaseInputDto
    /// </summary>
    public class MedicalCaseCreateInputDto
    {
        /// <summary>医案基础信息</summary>
        [Required(ErrorMessage = "医案信息不能为空")]
        [DisplayName("医案信息")]
        public MedicalCaseInputDto MedicalCase { get; set; } = new();

        /// <summary>诊疗记录信息（必需）</summary>
        [Required(ErrorMessage = "诊疗信息不能为空")]
        [DisplayName("诊疗信息")]
        public ConsultationInputDto Consultation { get; set; } = new();

        /// <summary>处方信息（可选）</summary>
        [DisplayName("处方信息")]
        public PrescriptionInputDto? Prescription { get; set; }
    }
}
