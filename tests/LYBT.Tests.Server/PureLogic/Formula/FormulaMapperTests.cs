using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Module.Formulas.Mapping;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Xunit;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Tests.Server.PureLogic.Formula;

/// <summary>
/// FormulaMapper 单元测试
/// 测试 Mapperly 编译时生成的映射逻辑
/// AntiMock: 纯映射测试，无依赖
/// </summary>
public class FormulaMapperTests
{
    private readonly FormulaMapper _mapper = new();

    #region ToListDto 测试

    [Fact]
    public void ToListDto_WithValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = CreateTestFormula();

        // Act
        var dto = _mapper.ToListDto(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
        dto.Effect.Should().Be(entity.Effect);
        dto.Indications.Should().Be(entity.Indication); // MapProperty: Indication -> Indications
        dto.Status.Should().Be(entity.Status);
        dto.ValidationStatus.Should().Be(entity.ValidationStatus);
        dto.Category.Should().Be(entity.Category);
        dto.IsShared.Should().Be(entity.IsShared);
    }

    [Fact]
    public void ToListDto_ShouldIgnoreHerbCountAndTotalPrice()
    {
        // Arrange
        var entity = CreateTestFormula();

        // Act
        var dto = _mapper.ToListDto(entity);

        // Assert
        dto.HerbCount.Should().Be(0); // Ignored, default
        dto.TotalPrice.Should().Be(0m); // Ignored, default
    }

    [Fact]
    public void ToListDto_WithNullOptionalFields_ShouldMapNulls()
    {
        // Arrange
        var entity = new FormulaEntity
        {
            Id = Guid.NewGuid(),
            Name = "小柴胡汤",
            Status = CommonStatus.Enabled,
            ValidationStatus = FormulaValidationStatus.Draft,
            FormulaType = FormulaType.Classic,
            // Optional fields null
            Effect = null,
            Indication = null,
            Category = null
        };

        // Act
        var dto = _mapper.ToListDto(entity);

        // Assert
        dto.Effect.Should().BeNull();
        dto.Indications.Should().BeNull();
        dto.Category.Should().BeNull();
    }

    #endregion

    #region ToListDtos 测试

    [Fact]
    public void ToListDtos_WithMultipleEntities_ShouldMapAll()
    {
        // Arrange
        var entities = new List<FormulaEntity>
        {
            CreateTestFormula("小柴胡汤"),
            CreateTestFormula("四君子汤"),
            CreateTestFormula("六味地黄丸")
        };

        // Act
        var dtos = _mapper.ToListDtos(entities);

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].Name.Should().Be("小柴胡汤");
        dtos[1].Name.Should().Be("四君子汤");
        dtos[2].Name.Should().Be("六味地黄丸");
    }

    [Fact]
    public void ToListDtos_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var entities = new List<FormulaEntity>();

        // Act
        var dtos = _mapper.ToListDtos(entities);

        // Assert
        dtos.Should().BeEmpty();
    }

    #endregion

    #region ToDetailDto 测试

    [Fact]
    public void ToDetailDto_WithValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = CreateTestFormula();

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
        dto.Effect.Should().Be(entity.Effect);
        dto.Indications.Should().Be(entity.Indication); // MapProperty
        dto.Usage.Should().Be(entity.Usage);
        dto.Remark.Should().Be(entity.Remark);
        dto.Property.Should().Be(entity.Property);
        dto.Status.Should().Be(entity.Status);
        dto.IsShared.Should().Be(entity.IsShared);
        dto.ValidationStatus.Should().Be(entity.ValidationStatus);
        dto.Category.Should().Be(entity.Category);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
        dto.CreatedBy.Should().Be(entity.CreatedBy);
    }

    [Fact]
    public void ToDetailDto_ShouldIgnoreCalculatedFields()
    {
        // Arrange
        var entity = CreateTestFormula();

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert
        dto.HerbCount.Should().Be(0); // Ignored
        dto.TotalPrice.Should().Be(0m); // Ignored
        dto.Description.Should().BeNull(); // Ignored
        dto.Source.Should().BeNull(); // Ignored
        dto.Contraindications.Should().BeNull(); // Ignored
    }

    #endregion

    #region ToDetailDtos 测试

    [Fact]
    public void ToDetailDtos_WithMultipleEntities_ShouldMapAll()
    {
        // Arrange
        var entities = new List<FormulaEntity>
        {
            CreateTestFormula("小柴胡汤"),
            CreateTestFormula("四君子汤")
        };

        // Act
        var dtos = _mapper.ToDetailDtos(entities);

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].Name.Should().Be("小柴胡汤");
        dtos[1].Name.Should().Be("四君子汤");
    }

    #endregion

    #region ToHerbItemDto 测试

    [Fact]
    public void ToHerbItemDto_WithValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = CreateTestFormulaHerbItem();

        // Act
        var dto = _mapper.ToHerbItemDto(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.HerbId.Should().Be(entity.HerbId);
        dto.HerbName.Should().Be(entity.HerbName);
        dto.OriginalHerbName.Should().Be(entity.OriginalHerbName);
        dto.IsValidated.Should().Be(entity.IsValidated);
        dto.Dosage.Should().Be(entity.Dosage);
        dto.Unit.Should().Be(entity.Unit);
        dto.Usage.Should().Be(entity.Usage);
        dto.ProcessingMethod.Should().Be(entity.ProcessingMethod);
    }

    [Fact]
    public void ToHerbItemDto_ShouldIgnoreServiceCalculatedFields()
    {
        // Arrange
        var entity = CreateTestFormulaHerbItem();

        // Act
        var dto = _mapper.ToHerbItemDto(entity);

        // Assert
        dto.SpecialInstructions.Should().BeNull(); // Ignored
        dto.SortOrder.Should().Be(0); // Ignored
        dto.Processing.Should().Be(entity.ProcessingMethod); // Aliased from ProcessingMethod
        dto.Price.Should().Be(0m); // Ignored
        dto.Preparation.Should().BeNull(); // Ignored
        dto.Herb.Should().BeNull(); // Ignored
    }

    [Fact]
    public void ToHerbItemDto_WithNullHerbId_ShouldMapNull()
    {
        // Arrange
        var entity = new FormulaHerbItem
        {
            Id = Guid.NewGuid(),
            HerbId = null, // Delayed binding
            HerbName = "柴胡",
            OriginalHerbName = "柴胡",
            IsValidated = false,
            Dosage = 24,
            Unit = "g"
        };

        // Act
        var dto = _mapper.ToHerbItemDto(entity);

        // Assert
        dto.HerbId.Should().BeNull();
        dto.IsValidated.Should().BeFalse();
    }

    #endregion

    #region ToHerbItemDtos 测试

    [Fact]
    public void ToHerbItemDtos_WithMultipleEntities_ShouldMapAll()
    {
        // Arrange
        var entities = new List<FormulaHerbItem>
        {
            CreateTestFormulaHerbItem("柴胡", 24),
            CreateTestFormulaHerbItem("黄芩", 9),
            CreateTestFormulaHerbItem("半夏", 12)
        };

        // Act
        var dtos = _mapper.ToHerbItemDtos(entities);

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].HerbName.Should().Be("柴胡");
        dtos[1].HerbName.Should().Be("黄芩");
        dtos[2].HerbName.Should().Be("半夏");
    }

    #endregion

    #region ToEntity 测试

    [Fact]
    public void ToEntity_WithValidDto_ShouldMapNonIgnoredProperties()
    {
        // Arrange
        var dto = new FormulaInputDto
        {
            Id = Guid.NewGuid(), // Should be ignored
            Name = "小柴胡汤",
            Effect = "和解少阳",
            Usage = "水煎服",
            Remark = "经典名方",
            IsShared = true,
            Category = "和解剂"
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Should().NotBeNull();
        entity.Id.Should().NotBe(dto.Id!.Value); // Ignored
        entity.Name.Should().Be(dto.Name);
        entity.Effect.Should().Be(dto.Effect);
        entity.Usage.Should().Be(dto.Usage);
        entity.Remark.Should().Be(dto.Remark);
        entity.IsShared.Should().Be(dto.IsShared);
        entity.Category.Should().Be(dto.Category);
    }

    [Fact]
    public void ToEntity_ShouldIgnoreManyFields()
    {
        // Arrange
        var dto = new FormulaInputDto
        {
            Name = "小柴胡汤"
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Status.Should().Be(CommonStatus.Enabled); // Constructor default, not mapped
        entity.Property.Should().BeNull(); // Ignored
        entity.Herbs.Should().BeEmpty(); // Ignored (ICollection default)
        entity.CreatedAt.Should().BeAfter(DateTime.MinValue); // Constructor default = UtcNow
        entity.CreatedBy.Should().BeNull(); // Ignored
        entity.UpdatedAt.Should().BeNull(); // Ignored
        entity.UpdatedBy.Should().BeNull(); // Ignored
        entity.RowVersion.Should().BeNull(); // Ignored
        entity.IsDeleted.Should().Be(false); // Ignored (default)
        entity.ValidationStatus.Should().Be(FormulaValidationStatus.Draft); // Constructor default
        entity.UserId.Should().BeNull(); // Ignored
        entity.Indication.Should().BeNull(); // Ignored
        entity.FormulaType.Should().Be(FormulaType.Experience); // Constructor default
    }

    #endregion

    #region UpdateEntity 测试

    [Fact]
    public void UpdateEntity_WithValidDto_ShouldUpdateNonIgnoredProperties()
    {
        // Arrange
        var existingEntity = CreateTestFormula();
        var originalId = existingEntity.Id;
        var originalCreatedAt = existingEntity.CreatedAt;

        var dto = new FormulaInputDto
        {
            Id = Guid.NewGuid(), // Should be ignored
            Name = "更新后的小柴胡汤",
            Effect = "更新功效",
            Usage = "更新用法",
            Remark = "更新备注",
            IsShared = false,
            Category = "更新分类"
        };

        // Act
        _mapper.UpdateEntity(dto, existingEntity);

        // Assert
        existingEntity.Id.Should().Be(originalId); // Ignored
        existingEntity.Name.Should().Be(dto.Name);
        existingEntity.Effect.Should().Be(dto.Effect);
        existingEntity.Usage.Should().Be(dto.Usage);
        existingEntity.Remark.Should().Be(dto.Remark);
        existingEntity.IsShared.Should().Be(dto.IsShared);
        existingEntity.Category.Should().Be(dto.Category);
    }

    [Fact]
    public void UpdateEntity_ShouldIgnoreAuditFields()
    {
        // Arrange
        var existingEntity = CreateTestFormula();
        var originalCreatedAt = existingEntity.CreatedAt;
        var originalCreatedBy = existingEntity.CreatedBy;

        var dto = new FormulaInputDto
        {
            Name = "小柴胡汤"
        };

        // Act
        _mapper.UpdateEntity(dto, existingEntity);

        // Assert
        existingEntity.Status.Should().Be(CommonStatus.Enabled); // Ignored
        existingEntity.Property.Should().NotBeNull(); // Ignored (unchanged)
        existingEntity.CreatedAt.Should().Be(originalCreatedAt); // Ignored
        existingEntity.CreatedBy.Should().Be(originalCreatedBy); // Ignored
        existingEntity.UpdatedAt.Should().BeNull(); // Ignored
        existingEntity.UpdatedBy.Should().BeNull(); // Ignored
        existingEntity.RowVersion.Should().BeNull(); // Ignored
        existingEntity.IsDeleted.Should().Be(false); // Ignored
    }

    #endregion

    #region MapProperty 测试

    [Fact]
    public void ToListDto_ShouldMapIndicationToIndications()
    {
        // Arrange
        var entity = new FormulaEntity
        {
            Id = Guid.NewGuid(),
            Name = "小柴胡汤",
            Indication = "和解少阳，扶正祛邪",
            Status = CommonStatus.Enabled,
            ValidationStatus = FormulaValidationStatus.Validated,
            FormulaType = FormulaType.Classic
        };

        // Act
        var dto = _mapper.ToListDto(entity);

        // Assert
        entity.Indication.Should().Be("和解少阳，扶正祛邪");
        dto.Indications.Should().Be("和解少阳，扶正祛邪");
    }

    [Fact]
    public void ToDetailDto_ShouldMapIndicationToIndications()
    {
        // Arrange
        var entity = new FormulaEntity
        {
            Id = Guid.NewGuid(),
            Name = "小柴胡汤",
            Indication = "和解少阳，扶正祛邪",
            Status = CommonStatus.Enabled,
            ValidationStatus = FormulaValidationStatus.Validated,
            FormulaType = FormulaType.Classic
        };

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert
        entity.Indication.Should().Be("和解少阳，扶正祛邪");
        dto.Indications.Should().Be("和解少阳，扶正祛邪");
    }

    #endregion

    #region 辅助方法

    private static FormulaEntity CreateTestFormula(string name = "小柴胡汤")
    {
        return new FormulaEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Effect = "和解少阳，扶正祛邪",
            Indication = "少阳病，寒热往来",
            Usage = "水煎服，日三次",
            Remark = "《伤寒论》经典名方",
            Property = "性平，归肝、胆经",
            Status = CommonStatus.Enabled,
            IsShared = true,
            ValidationStatus = FormulaValidationStatus.Validated,
            Category = "和解剂",
            FormulaType = FormulaType.Classic,
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            Herbs = new List<FormulaHerbItem>()
        };
    }

    private static FormulaHerbItem CreateTestFormulaHerbItem(string herbName = "柴胡", int dosage = 24)
    {
        return new FormulaHerbItem
        {
            Id = Guid.NewGuid(),
            HerbId = Guid.NewGuid(),
            HerbName = herbName,
            OriginalHerbName = herbName,
            IsValidated = true,
            Dosage = dosage,
            Unit = "g",
            Usage = "君药",
            ProcessingMethod = "生用"
        };
    }

    #endregion
}
