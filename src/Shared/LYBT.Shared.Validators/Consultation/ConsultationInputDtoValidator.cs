using FluentValidation;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Shared.Validators.Consultation
{
    /// <summary>
    /// 诊疗创建/更新DTO验证器（精简版 - OpenSpec: refactor-diagnosis-fields）
    /// Issue #2231: PatientId和UserId不验证必填，因为：
    /// 1. ConsultationInputDto主要用于Update操作（Consultation在MedicalCase创建时自动生成）
    /// 2. Consultation实体没有这两个字段（通过MedicalCase关联获取）
    /// 3. 这些字段在DTO中定义但仅用于某些特殊场景，不应该在Update时验证为必填
    /// </summary>
    public class ConsultationInputDtoValidator : AbstractValidator<ConsultationInputDto>
    {
        public ConsultationInputDtoValidator()
        {
            // TCMDiagnosis是唯一必填字段
            RuleFor(x => x.TCMDiagnosis)
                .NotEmpty().WithMessage("中医诊断不能为空")
                .MaximumLength(500).WithMessage("中医诊断长度不能超过500个字符");
        }
    }
}
