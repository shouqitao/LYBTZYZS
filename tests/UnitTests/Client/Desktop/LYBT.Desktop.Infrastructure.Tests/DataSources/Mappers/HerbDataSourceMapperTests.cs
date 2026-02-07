using FluentAssertions;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Tests.DataSources.Mappers;

/// <summary>
/// HerbDataSourceMapper 单元测试
/// Phase 4.4: Infrastructure P2 测试
/// </summary>
public class HerbDataSourceMapperTests
{
    private readonly HerbDataSourceMapper _mapper = new();

    #region ToEntity(HerbDetailDto) Tests

    [Fact]
    public void ToEntity_FromDetailDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new HerbDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "黄芪",
            PinYinCode = "HQ",
            Category = "补气药",
            Origin = "内蒙古",
            Spec = "片",
            Unit = "g",
            Price = 25.50m,
            CostPrice = 15.00m,
            Effect = "补气升阳",
            Usage = "煎服",
            Remark = "测试备注",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now.AddDays(-10),
            CreatedBy = Guid.NewGuid()
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Id.Should().Be(dto.Id);
        entity.Name.Should().Be(dto.Name);
        entity.PinYinCode.Should().Be(dto.PinYinCode);
        entity.Category.Should().Be(dto.Category);
        entity.Origin.Should().Be(dto.Origin);
        entity.Spec.Should().Be(dto.Spec);
        entity.Unit.Should().Be(dto.Unit);
        entity.Price.Should().Be(dto.Price);
        entity.CostPrice.Should().Be(dto.CostPrice);
        entity.Effect.Should().Be(dto.Effect);
        entity.Usage.Should().Be(dto.Usage);
        entity.Remark.Should().Be(dto.Remark);
        entity.Status.Should().Be(dto.Status);
    }

    #endregion

    #region ToDetailDto Tests

    [Fact]
    public void ToDetailDto_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "当归",
            PinYinCode = "DG",
            Category = "补血药",
            Origin = "甘肃",
            Spec = "片",
            Unit = "g",
            Price = 30.00m,
            CostPrice = 18.00m,
            Effect = "补血活血",
            Usage = "煎服",
            Remark = "备注信息",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now.AddDays(-5),
            CreatedBy = Guid.NewGuid()
        };

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
        dto.PinYinCode.Should().Be(entity.PinYinCode);
        dto.Category.Should().Be(entity.Category);
        dto.Origin.Should().Be(entity.Origin);
        dto.Price.Should().Be(entity.Price);
        dto.CostPrice.Should().Be(entity.CostPrice);
        dto.Effect.Should().Be(entity.Effect);
        dto.Status.Should().Be(entity.Status);
    }

    #endregion

    #region RoundTrip Tests

    [Fact]
    public void RoundTrip_DetailDto_ShouldPreserveData()
    {
        // Arrange
        var originalEntity = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "人参",
            PinYinCode = "RS",
            Category = "补气药",
            Origin = "吉林",
            Spec = "根",
            Unit = "g",
            Price = 100.00m,
            CostPrice = 60.00m,
            Effect = "大补元气",
            Usage = "煎服或研粉",
            Remark = "贵重药材",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now,
            CreatedBy = Guid.NewGuid()
        };

        // Act
        var dto = _mapper.ToDetailDto(originalEntity);
        var roundTrippedEntity = _mapper.ToEntity(dto);

        // Assert
        roundTrippedEntity.Id.Should().Be(originalEntity.Id);
        roundTrippedEntity.Name.Should().Be(originalEntity.Name);
        roundTrippedEntity.PinYinCode.Should().Be(originalEntity.PinYinCode);
        roundTrippedEntity.Category.Should().Be(originalEntity.Category);
        roundTrippedEntity.Origin.Should().Be(originalEntity.Origin);
        roundTrippedEntity.Price.Should().Be(originalEntity.Price);
        roundTrippedEntity.Status.Should().Be(originalEntity.Status);
    }

    #endregion

    #region ToEntity(HerbInputDto) Tests

    [Fact]
    public void ToEntity_FromInputDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new HerbInputDto
        {
            Id = Guid.NewGuid(),
            Name = "白术",
            PinYinCode = "BZ",
            Category = "补气药",
            Origin = "浙江",
            Spec = "片",
            Unit = "g",
            Price = 20.00m,
            CostPrice = 12.00m,
            Effect = "健脾益气",
            Usage = "煎服",
            Remark = "燥湿利水"
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Id.Should().Be(dto.Id!.Value);
        entity.Name.Should().Be(dto.Name);
        entity.PinYinCode.Should().Be(dto.PinYinCode);
        entity.Category.Should().Be(dto.Category);
        entity.Price.Should().Be(dto.Price);
        entity.CostPrice.Should().Be(dto.CostPrice);
    }

    [Fact]
    public void ToInputDto_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "茯苓",
            PinYinCode = "FL",
            Category = "利水渗湿药",
            Origin = "云南",
            Spec = "块",
            Unit = "g",
            Price = 15.00m,
            CostPrice = 8.00m,
            Effect = "利水渗湿",
            Usage = "煎服",
            Remark = "健脾安神"
        };

        // Act
        var dto = _mapper.ToInputDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
        dto.PinYinCode.Should().Be(entity.PinYinCode);
        dto.Category.Should().Be(entity.Category);
        dto.Price.Should().Be(entity.Price);
        dto.CostPrice.Should().Be(entity.CostPrice);
    }

    #endregion
}
