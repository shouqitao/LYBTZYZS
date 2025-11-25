using FluentValidation;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Shared.Validators.Consultation
{
    /// <summary>
    /// 诊疗创建/更新DTO验证器
    /// Issue #2231: PatientId和UserId不验证必填，因为：
    /// 1. ConsultationInputDto主要用于Update操作（Consultation在MedicalCase创建时自动生成）
    /// 2. Consultation实体没有这两个字段（通过MedicalCase关联获取）
    /// 3. 这些字段在DTO中定义但仅用于某些特殊场景，不应该在Update时验证为必填
    /// </summary>
    public class ConsultationInputDtoValidator : AbstractValidator<ConsultationInputDto>
    {
        public ConsultationInputDtoValidator()
        {
            // Issue #2231: 移除PatientId和UserId的必填验证
            // 原因：Consultation实体没有这两个字段，它们通过MedicalCase关联获取
            // RuleFor(x => x.PatientId)
            //     .NotEmpty().WithMessage("患者ID不能为空");
            //
            // RuleFor(x => x.UserId)
            //     .NotEmpty().WithMessage("用户ID不能为空");

            RuleFor(x => x.ChiefComplaint)
                .MaximumLength(500).WithMessage("主诉长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));
        }
    }
}
