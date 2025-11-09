using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 病案输入DTO - 统一创建和更新
    /// </summary>
    /// <remarks>
    /// Epic #1961: FluentValidation统一设计重构
    ///
    /// 设计理念：
    /// 1. 使用nullable Guid? Id字段区分创建（Id=null）和更新（Id有值）
    /// 2. 统一验证规则，避免CreateDto和UpdateDto的重复代码
    /// 3. 所有验证规则由MedicalCaseInputDtoValidator统一管理
    ///
    /// 相关文档：
    /// - 设计文档：docs/explanation/fluentvalidation-unified-design.md
    /// - 任务文档：docs/tasks/fluentvalidation-unified-tasks.md
    /// - GitHub Epic：Issue #1961
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
        /// 医生ID
        /// </summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 就诊日期
        /// </summary>
        [DisplayName("就诊日期")]
        public DateTime VisitDate { get; set; }

        /// <summary>
        /// 主诉
        /// </summary>
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>
        /// 现病史
        /// </summary>
        [DisplayName("现病史")]
        public string? PresentIllnessHistory { get; set; }

        /// <summary>
        /// 既往史
        /// </summary>
        [DisplayName("既往史")]
        public string? PastMedicalHistory { get; set; }

        /// <summary>
        /// 过敏史
        /// </summary>
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>
        /// 望诊
        /// </summary>
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>
        /// 闻诊
        /// </summary>
        [DisplayName("闻诊")]
        public string? Auscultation { get; set; }

        /// <summary>
        /// 问诊
        /// </summary>
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>
        /// 切诊（脉象）
        /// </summary>
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        /// <summary>
        /// 中医诊断
        /// </summary>
        [DisplayName("中医诊断")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>
        /// 西医诊断
        /// </summary>
        [DisplayName("西医诊断")]
        public string? WesternDiagnosis { get; set; }

        /// <summary>
        /// 治则治法
        /// </summary>
        [DisplayName("治则治法")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}
