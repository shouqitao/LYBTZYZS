using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 病案输入DTO - 统一创建和更新
    /// </summary>
    /// <remarks>
    /// OpenSpec: unify-medicalcase-input-dto
    ///
    /// 设计理念：
    /// 1. 仅包含创建/更新医案的核心字段
    /// 2. 使用nullable Guid? Id字段区分创建（Id=null）和更新（Id有值）
    /// 3. 诊断字段应使用ConsultationInputDto通过聚合保存API提交
    /// 4. 所有验证规则由MedicalCaseInputDtoValidator统一管理
    ///
    /// 相关OpenSpec:
    /// - unify-medicalcase-input-dto: 简化InputDto，移除未使用的诊断字段
    /// - consolidate-medicalcase-queries: 整合医案查询到聚合根模式
    /// </remarks>
    public class MedicalCaseInputDto
    {
        /// <summary>
        /// 病案ID（更新时必填，创建时为null）
        /// </summary>
        [DisplayName("病案ID")]
        public Guid? Id { get; set; }

        /// <summary>
        /// 患者ID
        /// </summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>
        /// 医生ID（可选，Server端可自动填充当前用户）
        /// </summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 就诊日期
        /// </summary>
        [DisplayName("就诊日期")]
        public DateTime VisitDate { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
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
