using FluentValidation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Validators.Common;

namespace LYBT.Shared.Validators.Prescriptions
{
    /// <summary>
    /// 处方聚合DTO验证器
    /// OpenSpec: refactor-medicalcase-aggregate-crud (PERSIST-002)
    /// </summary>
    /// <remarks>
    /// 验证规则：
    /// 1. 当NeedsPrescription=true时，Items必须有有效项目
    /// 2. DosageCount必须在有效范围内
    /// 3. Items中的每个项目使用嵌套验证器
    /// </remarks>
    public class PrescriptionAggregateDtoValidator : AbstractValidator<PrescriptionAggregateDto>
    {
        public PrescriptionAggregateDtoValidator()
        {
            // ========== 基础字段验证 ==========

            // 剂数：必须在有效范围内
            RuleFor(x => x.DosageCount)
                .InclusiveBetween(
                    ValidationConstants.DosageCountMinValue,
                    ValidationConstants.DosageCountMaxValue)
                .WithMessage($"剂数必须在{ValidationConstants.DosageCountMinValue}-{ValidationConstants.DosageCountMaxValue}之间");

            // 折扣：必须在0-1之间
            RuleFor(x => x.Discount)
                .InclusiveBetween(0, 1)
                .WithMessage("折扣必须在0到1之间");

            // ========== 可选字段验证 ==========

            // 用法：可选，有值时验证长度（200字符）
            RuleFor(x => x.Usage)
                .MaximumLength(200)
                .WithMessage("用法说明长度不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));

            // 医嘱：可选，有值时验证长度
            RuleFor(x => x.Advice)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"医嘱长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Advice));

            // 验方来源：可选，有值时验证长度（200字符）
            RuleFor(x => x.FormulaSource)
                .MaximumLength(200)
                .WithMessage("验方来源长度不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.FormulaSource));

            // 引用验方：可选，有值时验证长度（500字符）
            RuleFor(x => x.ReferencedFormulas)
                .MaximumLength(500)
                .WithMessage("引用验方名称长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.ReferencedFormulas));

            // 主治：可选，有值时验证长度（500字符）
            RuleFor(x => x.Indication)
                .MaximumLength(500)
                .WithMessage("主治长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Indication));

            // 备注：可选，有值时验证长度
            RuleFor(x => x.Remark)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"备注长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            // ========== 条件验证：开处方时需要有处方项目 ==========

            // 当NeedsPrescription=true时，Items必须有有效项目
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("开处方时必须添加至少一项药材")
                .Must(items => items != null && items.Any(i => i.HerbId != Guid.Empty && i.Quantity > 0))
                .WithMessage("处方必须包含至少一项有效药材（药材ID和数量不能为空）")
                .When(x => x.NeedsPrescription);

            // 处方项目：有值时使用嵌套验证器
            RuleForEach(x => x.Items)
                .SetValidator(new PrescriptionItemInputDtoValidator())
                .When(x => x.Items != null && x.Items.Any());
        }
    }
}
