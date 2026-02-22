using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Primitives.Validation;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医案输入DTO - 统一创建、更新和聚合保存
    /// </summary>
    /// <remarks>
    /// OpenSpec: unify-medicalcase-input-dto, simplify-medicalcase-dataflow
    ///
    /// 设计理念：
    /// 1. 统一创建/更新/聚合保存场景，替代原MedicalCaseAggregateInputDto
    /// 2. 使用nullable Guid? Id字段区分创建（Id=null）和更新（Id有值）
    /// 3. 嵌套Consultation/Prescription支持单次事务保存
    /// 4. 所有验证规则由MedicalCaseInputDtoValidator统一管理
    ///
    /// 使用场景：
    /// - 创建医案：Id=null, PatientId必填, UserId必填
    /// - 更新医案：Id有值, 可选更新Consultation/Prescription
    /// - 聚合保存：Id有值, 同时保存诊断和处方（支持"仅诊断无处方"场景）
    /// </remarks>
    public class MedicalCaseInputDto
    {
        /// <summary>
        /// 医案ID（更新时必填，创建时为null）
        /// </summary>
        [DisplayName("医案ID")]
        public Guid? Id { get; set; }

        /// <summary>
        /// 患者ID（创建时必填）
        /// </summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>
        /// 医生ID（可选，Server端可自动填充当前用户）
        /// OpenSpec: simplify-medicalcase-dataflow - 重命名自DoctorId
        /// </summary>
        [DisplayName("医生ID")]
        public Guid UserId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// 编辑原因（审计用途，非当天本人修改时必填）
        /// OpenSpec: simplify-medicalcase-dataflow - 从MedicalCaseAggregateInputDto迁移
        /// </summary>
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "编辑原因长度不能超过{1}个字符")]
        [DisplayName("编辑原因")]
        public string? EditReason { get; set; }

        /// <summary>
        /// 诊断信息（嵌套，可选）
        /// OpenSpec: simplify-medicalcase-dataflow - 支持聚合保存
        /// </summary>
        [DisplayName("诊断信息")]
        public ConsultationInputDto? Consultation { get; set; }

        /// <summary>
        /// 处方信息（嵌套，可选）
        /// OpenSpec: simplify-medicalcase-dataflow - 支持聚合保存
        /// </summary>
        [DisplayName("处方信息")]
        public PrescriptionInputDto? Prescription { get; set; }

        /// <summary>
        /// 是否开处方标志
        /// OpenSpec: simplify-medicalcase-api - 用于聚合保存时控制处方创建/删除
        /// 当设置为false且Prescription为null时，触发处方软删除
        /// </summary>
        [DisplayName("是否开处方")]
        public bool? NeedsPrescription { get; set; }

        // VisitDate已删除，用BaseEntity.CreatedAt代替
    }

    /// <summary>
    /// 取消医案请求DTO
    /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-011)
    /// </summary>
    public class CancelMedicalCaseRequestDto
    {
        /// <summary>
        /// 取消原因（非当天本人操作时必填）
        /// </summary>
        [DisplayName("取消原因")]
        public string? Reason { get; set; }
    }
}
