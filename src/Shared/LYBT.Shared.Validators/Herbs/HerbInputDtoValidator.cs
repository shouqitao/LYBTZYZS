using FluentValidation;
using LYBT.Shared.Models.Constants;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Shared.Validators.Herbs
{
    /// <summary>
    /// 药材输入DTO验证器
    /// Epic #1962 Task 1.3: 实现BR-001到BR-008验证规则
    /// </summary>
    public class HerbInputDtoValidator : AbstractValidator<HerbInputDto>
    {
        public HerbInputDtoValidator()
        {
            // BR-001: 药材名称1-50字符，必填
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("药材名称不能为空")
                .Length(1, ValidationConstants.NameMaxLength)
                .WithMessage($"药材名称长度必须在1-{ValidationConstants.NameMaxLength}个字符之间");

            // BR-003: 拼音码50字符以内（可选）
            RuleFor(x => x.PinYinCode)
                .MaximumLength(ValidationConstants.CodeMaxLength)
                .WithMessage($"拼音码长度不能超过{ValidationConstants.CodeMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.PinYinCode));

            // BR-004: 分类50字符以内（可选）
            RuleFor(x => x.Category)
                .MaximumLength(ValidationConstants.CodeMaxLength)
                .WithMessage($"分类长度不能超过{ValidationConstants.CodeMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Category));

            // 产地100字符以内（可选）
            RuleFor(x => x.Origin)
                .MaximumLength(100)
                .WithMessage("产地长度不能超过100个字符")
                .When(x => !string.IsNullOrEmpty(x.Origin));

            // 规格50字符以内（可选）
            RuleFor(x => x.Spec)
                .MaximumLength(50)
                .WithMessage("规格长度不能超过50个字符")
                .When(x => !string.IsNullOrEmpty(x.Spec));

            // 单位必填，20字符以内
            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("单位不能为空")
                .MaximumLength(20)
                .WithMessage("单位长度不能超过20个字符");

            // BR-005: 单价 > 0
            RuleFor(x => x.Price)
                .GreaterThan(ValidationConstants.PriceMinValue)
                .WithMessage("单价必须大于0")
                .LessThanOrEqualTo(ValidationConstants.PriceMaxValue)
                .WithMessage($"单价不能超过{ValidationConstants.PriceMaxValue}");

            // 功效500字符以内（可选）
            RuleFor(x => x.Effect)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"功效长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Effect));

            // 用法用量200字符以内（可选）
            RuleFor(x => x.Usage)
                .MaximumLength(ValidationConstants.UsageMaxLength)
                .WithMessage($"用法用量长度不能超过{ValidationConstants.UsageMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));

            // 备注500字符以内（可选）
            RuleFor(x => x.Remark)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"备注长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));
        }
    }
}
