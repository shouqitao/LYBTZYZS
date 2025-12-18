using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Validators.Prescriptions;
using Xunit;

namespace LYBT.Shared.Validators.Tests.Prescriptions;

/// <summary>
/// PrescriptionAggregateInputDtoValidator 单元测试
/// OpenSpec: refactor-medicalcase-aggregate-crud (PERSIST-002)
/// </summary>
public class PrescriptionAggregateInputDtoValidatorTests
{
    private readonly PrescriptionAggregateInputDtoValidator _validator;

    public PrescriptionAggregateInputDtoValidatorTests()
    {
        _validator = new PrescriptionAggregateInputDtoValidator();
    }

    #region 基础字段验证

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = true,
            DosageCount = 7,
            Discount = 1.0m,
            Items = new List<PrescriptionItemInputDto>
            {
                new() { HerbId = Guid.NewGuid(), Dosage = 10 }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]  // 小于最小值1
    [InlineData(101)] // 大于最大值100
    public void Validate_WithInvalidDosageCount_FailsValidation(int dosageCount)
    {
        // Arrange
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = false,
            DosageCount = dosageCount,
            Discount = 1.0m
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DosageCount);
    }

    [Theory]
    [InlineData(1)]   // 最小值
    [InlineData(7)]   // 默认值
    [InlineData(100)] // 最大值
    public void Validate_WithValidDosageCount_PassesValidation(int dosageCount)
    {
        // Arrange
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = false,
            DosageCount = dosageCount,
            Discount = 1.0m
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DosageCount);
    }

    [Theory]
    [InlineData(-0.1)]  // 小于0
    [InlineData(1.1)]   // 大于1
    public void Validate_WithInvalidDiscount_FailsValidation(double discount)
    {
        // Arrange
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = false,
            DosageCount = 7,
            Discount = (decimal)discount
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Discount);
    }

    [Theory]
    [InlineData(0)]    // 最小值
    [InlineData(0.5)]  // 中间值
    [InlineData(1)]    // 最大值
    public void Validate_WithValidDiscount_PassesValidation(double discount)
    {
        // Arrange
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = false,
            DosageCount = 7,
            Discount = (decimal)discount
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Discount);
    }

    #endregion

    #region 可选字段验证

    [Fact]
    public void Validate_WithUsageTooLong_FailsValidation()
    {
        // Arrange
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = false,
            DosageCount = 7,
            Usage = new string('a', 201) // 超过200字符
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Usage);
    }

    [Fact]
    public void Validate_WithAdviceTooLong_FailsValidation()
    {
        // Arrange
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = false,
            DosageCount = 7,
            Advice = new string('a', 1001) // 超过1000字符
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Advice);
    }

    [Fact]
    public void Validate_WithFormulaSourceTooLong_FailsValidation()
    {
        // Arrange
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = false,
            DosageCount = 7,
            FormulaSource = new string('a', 201) // 超过200字符
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FormulaSource);
    }

    [Fact]
    public void Validate_WithValidOptionalFields_PassesValidation()
    {
        // Arrange
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = false,
            DosageCount = 7,
            Usage = "每日一剂，早晚分服",
            Advice = "忌辛辣刺激",
            FormulaSource = "伤寒论"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region NeedsPrescription条件验证

    [Fact]
    public void Validate_WhenNeedsPrescriptionTrue_WithEmptyItems_FailsValidation()
    {
        // Arrange - PERSIST-002: NeedsPrescription=true时必须有Items
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = true,
            DosageCount = 7,
            Items = new List<PrescriptionItemInputDto>() // 空列表
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("开处方时必须添加至少一项药材");
    }

    [Fact]
    public void Validate_WhenNeedsPrescriptionTrue_WithInvalidItems_FailsValidation()
    {
        // Arrange - 有Items但HerbId为空
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = true,
            DosageCount = 7,
            Items = new List<PrescriptionItemInputDto>
            {
                new() { HerbId = Guid.Empty, Dosage = 10 } // 无效的HerbId
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("处方必须包含至少一项有效药材（药材ID和数量不能为空）");
    }

    [Fact]
    public void Validate_WhenNeedsPrescriptionTrue_WithZeroDosageItem_FailsValidation()
    {
        // Arrange - 有Items但Dosage为0
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = true,
            DosageCount = 7,
            Items = new List<PrescriptionItemInputDto>
            {
                new() { HerbId = Guid.NewGuid(), Dosage = 0 } // 无效的Dosage
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_WhenNeedsPrescriptionFalse_WithEmptyItems_PassesValidation()
    {
        // Arrange - PERSIST-001: 不开处方时空Items应该通过
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = false,
            DosageCount = 7,
            Items = new List<PrescriptionItemInputDto>()
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_WhenNeedsPrescriptionFalse_WithNullItems_PassesValidation()
    {
        // Arrange
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = false,
            DosageCount = 7,
            Items = null!
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - NeedsPrescription=false时不验证Items
        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    #endregion

    #region 嵌套Items验证

    [Fact]
    public void Validate_WithValidItems_PassesValidation()
    {
        // Arrange
        var dto = new PrescriptionAggregateInputDto
        {
            NeedsPrescription = true,
            DosageCount = 7,
            Items = new List<PrescriptionItemInputDto>
            {
                new() { HerbId = Guid.NewGuid(), HerbName = "天麻", Dosage = 15 },
                new() { HerbId = Guid.NewGuid(), HerbName = "钩藤", Dosage = 10 }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
