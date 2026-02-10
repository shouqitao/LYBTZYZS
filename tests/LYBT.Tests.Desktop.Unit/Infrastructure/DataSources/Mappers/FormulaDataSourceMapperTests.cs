using FluentAssertions;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Desktop.Infrastructure.Tests.DataSources.Mappers;

/// <summary>
/// FormulaDataSourceMapper 单元测试
/// Phase 4.4: Infrastructure P2 测试
/// </summary>
public class FormulaDataSourceMapperTests
{
    private readonly FormulaDataSourceMapper _mapper = new();

    #region ToEntity(FormulaDetailDto) Tests

    [Fact]
    public void ToEntity_FromDetailDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new FormulaDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "四君子汤",
            Effect = "益气健脾",
            Usage = "水煎服",
            Remark = "补气代表方",
            Property = "性温",
            Status = CommonStatus.Enabled,
            IsShared = true,
            ValidationStatus = FormulaValidationStatus.Validated,
            Category = "补益剂",
            CreatedAt = DateTime.Now.AddMonths(-6)
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Id.Should().Be(dto.Id);
        entity.Name.Should().Be(dto.Name);
        entity.Effect.Should().Be(dto.Effect);
        entity.Usage.Should().Be(dto.Usage);
        entity.Remark.Should().Be(dto.Remark);
        entity.Property.Should().Be(dto.Property);
        entity.Status.Should().Be(dto.Status);
        entity.IsShared.Should().Be(dto.IsShared);
        entity.ValidationStatus.Should().Be(dto.ValidationStatus);
        entity.Category.Should().Be(dto.Category);
    }

    #endregion

    #region ToDetailDto Tests

    [Fact]
    public void ToDetailDto_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new FormulaEntity
        {
            Id = Guid.NewGuid(),
            Name = "六味地黄丸",
            Effect = "滋阴补肾",
            Usage = "水丸服",
            Remark = "滋阴代表方",
            Property = "性平",
            Status = CommonStatus.Enabled,
            IsShared = true,
            ValidationStatus = FormulaValidationStatus.Validated,
            Category = "补益剂",
            CreatedAt = DateTime.Now.AddMonths(-12)
        };

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
        dto.Effect.Should().Be(entity.Effect);
        dto.Usage.Should().Be(entity.Usage);
        dto.Remark.Should().Be(entity.Remark);
        dto.Property.Should().Be(entity.Property);
        dto.Status.Should().Be(entity.Status);
        dto.IsShared.Should().Be(entity.IsShared);
        dto.Category.Should().Be(entity.Category);
    }

    #endregion

    #region RoundTrip Tests

    [Fact]
    public void RoundTrip_DetailDto_ShouldPreserveData()
    {
        // Arrange
        var originalEntity = new FormulaEntity
        {
            Id = Guid.NewGuid(),
            Name = "补中益气汤",
            Effect = "补中益气升阳",
            Usage = "水煎服",
            Remark = "脾胃气虚",
            Property = "性温",
            Status = CommonStatus.Enabled,
            IsShared = false,
            ValidationStatus = FormulaValidationStatus.Draft,
            Category = "补益剂",
            CreatedAt = DateTime.Now
        };

        // Act
        var dto = _mapper.ToDetailDto(originalEntity);
        var roundTrippedEntity = _mapper.ToEntity(dto);

        // Assert
        roundTrippedEntity.Id.Should().Be(originalEntity.Id);
        roundTrippedEntity.Name.Should().Be(originalEntity.Name);
        roundTrippedEntity.Effect.Should().Be(originalEntity.Effect);
        roundTrippedEntity.Usage.Should().Be(originalEntity.Usage);
        roundTrippedEntity.Status.Should().Be(originalEntity.Status);
        roundTrippedEntity.Category.Should().Be(originalEntity.Category);
    }

    #endregion

    #region FormulaHerbItem Tests

    [Fact]
    public void ToEntity_FromHerbItemDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new FormulaHerbItemDto
        {
            Id = Guid.NewGuid(),
            HerbId = Guid.NewGuid(),
            HerbName = "人参",
            OriginalHerbName = "人参",
            IsValidated = true,
            Dosage = 10,
            Unit = "g",
            Usage = "后下",
            ProcessingMethod = "切片",
            DecocteMethod = DecocteMethod.PostAdd
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Id.Should().Be(dto.Id);
        entity.HerbId.Should().Be(dto.HerbId);
        entity.HerbName.Should().Be(dto.HerbName);
        entity.Dosage.Should().Be(dto.Dosage);
        entity.Unit.Should().Be(dto.Unit);
        entity.Usage.Should().Be(dto.Usage);
        entity.ProcessingMethod.Should().Be(dto.ProcessingMethod);
        entity.DecocteMethod.Should().Be(dto.DecocteMethod);
    }

    [Fact]
    public void ToDto_FromHerbItem_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new FormulaHerbItem
        {
            Id = Guid.NewGuid(),
            HerbId = Guid.NewGuid(),
            HerbName = "黄芪",
            OriginalHerbName = "黄芪",
            IsValidated = true,
            Dosage = 15,
            Unit = "g",
            Usage = "先煎",
            ProcessingMethod = "炙用",
            DecocteMethod = DecocteMethod.PreDecoct
        };

        // Act
        var dto = _mapper.ToDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.HerbId.Should().Be(entity.HerbId);
        dto.HerbName.Should().Be(entity.HerbName);
        dto.Dosage.Should().Be(entity.Dosage);
        dto.Unit.Should().Be(entity.Unit);
        dto.Usage.Should().Be(entity.Usage);
        dto.ProcessingMethod.Should().Be(entity.ProcessingMethod);
    }

    #endregion
}
