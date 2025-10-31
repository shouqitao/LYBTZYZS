using FluentValidation;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Validators
{
    /// <summary>
    /// 处方创建DTO验证器
    /// </summary>
    public class PrescriptionCreateDtoValidator : AbstractValidator<PrescriptionCreateDto>
    {
        public PrescriptionCreateDtoValidator()
        {
            // 患者ID必填
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("患者ID不能为空");

            // 医生ID必填
            RuleFor(x => x.DoctorId)
                .NotEmpty().WithMessage("医生ID不能为空");

            // 处方编号长度限制（可选）
            RuleFor(x => x.PrescriptionNumber)
                .MaximumLength(50).WithMessage("处方编号长度不能超过50个字符")
                .When(x => !string.IsNullOrEmpty(x.PrescriptionNumber));

            // 患者姓名长度限制（可选）
            RuleFor(x => x.PatientName)
                .MaximumLength(50).WithMessage("患者姓名长度不能超过50个字符")
                .When(x => !string.IsNullOrEmpty(x.PatientName));

            // 医生姓名长度限制（可选）
            RuleFor(x => x.DoctorName)
                .MaximumLength(50).WithMessage("医生姓名长度不能超过50个字符")
                .When(x => !string.IsNullOrEmpty(x.DoctorName));

            // 诊断长度限制（可选）
            RuleFor(x => x.Diagnosis)
                .MaximumLength(500).WithMessage("诊断长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Diagnosis));

            // 剂型长度限制（可选）
            RuleFor(x => x.DosageForm)
                .MaximumLength(50).WithMessage("剂型长度不能超过50个字符")
                .When(x => !string.IsNullOrEmpty(x.DosageForm));

            // 剂数范围验证
            RuleFor(x => x.Quantity)
                .InclusiveBetween(1, 100).WithMessage("剂数必须在1-100之间");

            // 用法说明长度限制（可选）
            RuleFor(x => x.Usage)
                .MaximumLength(200).WithMessage("用法说明不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));

            // 总金额范围验证
            RuleFor(x => x.TotalAmount)
                .GreaterThanOrEqualTo(0).WithMessage("总金额必须大于等于0");

            // 方剂来源长度限制（可选）
            RuleFor(x => x.FormulaSource)
                .MaximumLength(100).WithMessage("方剂来源不能超过100个字符")
                .When(x => !string.IsNullOrEmpty(x.FormulaSource));

            // 用药建议长度限制（可选）
            RuleFor(x => x.Advice)
                .MaximumLength(500).WithMessage("用药建议不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Advice));

            // 备注长度限制（可选）
            RuleFor(x => x.Remark)
                .MaximumLength(500).WithMessage("备注不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            // Notes长度限制（可选）
            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Notes));

            // 验证处方项目（可选）
            When(x => x.Items != null && x.Items.Count > 0, () =>
            {
                RuleForEach(x => x.Items).SetValidator(new PrescriptionItemInputDtoValidator());
            });
        }
    }

    /// <summary>
    /// 处方项目创建DTO验证器
    /// </summary>
    public class PrescriptionItemInputDtoValidator : AbstractValidator<PrescriptionItemInputDto>
    {
        public PrescriptionItemInputDtoValidator()
        {
            // 药材ID必填
            RuleFor(x => x.HerbId)
                .NotEmpty().WithMessage("中药材ID不能为空");

            // 药材名称必填且长度限制
            RuleFor(x => x.HerbName)
                .NotEmpty().WithMessage("中药材名称不能为空")
                .MaximumLength(100).WithMessage("中药材名称长度不能超过100个字符");

            // 用量范围验证
            RuleFor(x => x.Quantity)
                .InclusiveBetween(0.1m, 1000m).WithMessage("用量必须在0.1-1000之间");

            // 单位必填且长度限制
            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("单位不能为空")
                .MaximumLength(10).WithMessage("单位长度不能超过10个字符");

            // 单价范围验证
            RuleFor(x => x.UnitPrice)
                .InclusiveBetween(0m, 10000m).WithMessage("单价必须在0-10000之间");

            // 小计金额范围验证
            RuleFor(x => x.Subtotal)
                .GreaterThanOrEqualTo(0).WithMessage("小计金额必须大于等于0");

            // 用法长度限制（可选）
            RuleFor(x => x.Usage)
                .MaximumLength(200).WithMessage("用法长度不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));

            // Note长度限制（可选）
            RuleFor(x => x.Note)
                .MaximumLength(200).WithMessage("备注长度不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Note));

            // Remark长度限制（可选）
            RuleFor(x => x.Remark)
                .MaximumLength(100).WithMessage("备注长度不能超过100个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));
        }
    }
}
