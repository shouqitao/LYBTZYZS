using FluentValidation;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Shared.Validators.Formula
{
    /// <summary>
    /// 方剂创建DTO验证器
    /// </summary>
    public class FormulaInputDtoValidator : AbstractValidator<FormulaInputDto>
    {
        public FormulaInputDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("方剂名称不能为空")
                .MaximumLength(100).WithMessage("方剂名称长度不能超过100个字符");

            RuleFor(x => x.Effect)
                .MaximumLength(500).WithMessage("功效长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Effect));

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("描述长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Usage)
                .MaximumLength(500).WithMessage("用法长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));

            RuleFor(x => x.Indications)
                .MaximumLength(500).WithMessage("主治长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Indications));

            RuleFor(x => x.Remark)
                .MaximumLength(1000).WithMessage("备注长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            // T5-P2-35: 强制非空校验（移除 .When 条件，确保 Herbs=null 时也触发校验）
            RuleFor(x => x.Herbs)
                .NotNull().WithMessage("药材列表不能为空")
                .NotEmpty().WithMessage("方剂必须包含至少一味药材");

            RuleForEach(x => x.Herbs)
                .SetValidator(new FormulaHerbItemInputDtoValidator())
                .When(x => x.Herbs != null && x.Herbs.Any());
        }
    }

    /// <summary>
    /// 方剂药材项DTO验证器
    /// Issue #2014: 添加HerbName/Unit/ProcessingMethod验证，移除HerbId必填约束（支持延迟绑定）
    /// </summary>
    public class FormulaHerbItemInputDtoValidator : AbstractValidator<FormulaHerbItemInputDto>
    {
        public FormulaHerbItemInputDtoValidator()
        {
            // HerbId可空（支持延迟绑定），不验证NotEmpty

            // Issue #2014新增：药材名称验证（必填）
            RuleFor(x => x.HerbName)
                .NotEmpty().WithMessage("药材名称不能为空")
                .MaximumLength(100).WithMessage("药材名称长度不能超过100个字符");

            RuleFor(x => x.Dosage)
                .GreaterThan(0).WithMessage("用量必须大于0")
                .LessThanOrEqualTo(1000).WithMessage("用量不能超过1000克");

            // Issue #2014新增：单位验证（必填）
            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("单位不能为空")
                .MaximumLength(10).WithMessage("单位长度不能超过10个字符");

            // Issue #2014新增：加工方法验证（可选）
            RuleFor(x => x.ProcessingMethod)
                .MaximumLength(100).WithMessage("加工方法长度不能超过100个字符")
                .When(x => !string.IsNullOrEmpty(x.ProcessingMethod));

            RuleFor(x => x.Usage)
                .MaximumLength(200).WithMessage("用法长度不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));
        }
    }
}
