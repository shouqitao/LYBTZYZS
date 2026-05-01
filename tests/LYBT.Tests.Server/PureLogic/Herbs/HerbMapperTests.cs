using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Mapping;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Herbs;

/// <summary>
/// HerbMapper 单元测试
/// 测试 Mapperly 编译时生成的映射逻辑
/// AntiMock: 纯映射测试，无依赖
/// </summary>
public class HerbMapperTests
{
    private readonly HerbMapper _mapper = new();

    #region ToListDto 测试

    [Fact]
    public void ToListDto_WithValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = CreateTestHerb();

        // Act
        var dto = _mapper.ToListDto(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
        dto.PinYinCode.Should().Be(entity.PinYinCode);
        dto.Category.Should().Be(entity.Category);
        dto.Origin.Should().Be(entity.Origin);
        dto.Spec.Should().Be(entity.Spec);
        dto.Unit.Should().Be(entity.Unit);
        dto.Price.Should().Be(entity.Price);
        dto.Status.Should().Be(entity.Status);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
    }

    [Fact]
    public void ToListDto_WithNullOptionalFields_ShouldMapNulls()
    {
        // Arrange
        var entity = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "当归",
            Unit = "克",
            Price = 5.50m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow,
            // Optional fields null
            PinYinCode = null,
            Category = null,
            Origin = null,
            Spec = null
        };

        // Act
        var dto = _mapper.ToListDto(entity);

        // Assert
        dto.PinYinCode.Should().BeNull();
        dto.Category.Should().BeNull();
        dto.Origin.Should().BeNull();
        dto.Spec.Should().BeNull();
    }

    #endregion

    #region ToListDtos 测试

    [Fact]
    public void ToListDtos_WithMultipleEntities_ShouldMapAll()
    {
        // Arrange
        var entities = new List<Herb>
        {
            CreateTestHerb("当归", 5.50m),
            CreateTestHerb("人参", 10.00m),
            CreateTestHerb("黄芪", 3.00m)
        };

        // Act
        var dtos = _mapper.ToListDtos(entities);

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].Name.Should().Be("当归");
        dtos[1].Name.Should().Be("人参");
        dtos[2].Name.Should().Be("黄芪");
    }

    [Fact]
    public void ToListDtos_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var entities = new List<Herb>();

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
        var entity = CreateTestHerb();

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
        dto.PinYinCode.Should().Be(entity.PinYinCode);
        dto.Category.Should().Be(entity.Category);
        dto.Properties.Should().Be(entity.Properties);
        dto.Origin.Should().Be(entity.Origin);
        dto.Spec.Should().Be(entity.Spec);
        dto.Unit.Should().Be(entity.Unit);
        dto.Price.Should().Be(entity.Price);
        dto.CostPrice.Should().Be(entity.CostPrice);
        dto.Effect.Should().Be(entity.Effect);
        dto.Usage.Should().Be(entity.Usage);
        dto.Remark.Should().Be(entity.Remark);
        dto.Status.Should().Be(entity.Status);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
        dto.CreatedBy.Should().Be(entity.CreatedBy);
    }

    [Fact]
    public void ToDetailDto_WithNullOptionalFields_ShouldMapNulls()
    {
        // Arrange
        var entity = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "当归",
            Unit = "克",
            Price = 5.50m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow,
            // Optional fields null
            PinYinCode = null,
            Category = null,
            Properties = null,
            Origin = null,
            Spec = null,
            CostPrice = null,
            Effect = null,
            Usage = null,
            Remark = null,
            UpdatedAt = null,
            CreatedBy = null
        };

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert
        dto.PinYinCode.Should().BeNull();
        dto.Category.Should().BeNull();
        dto.Properties.Should().BeNull();
        dto.Origin.Should().BeNull();
        dto.Spec.Should().BeNull();
        dto.CostPrice.Should().BeNull();
        dto.Effect.Should().BeNull();
        dto.Usage.Should().BeNull();
        dto.Remark.Should().BeNull();
        dto.UpdatedAt.Should().BeNull();
        dto.CreatedBy.Should().BeNull();
    }

    #endregion

    #region ToDetailDtos 测试

    [Fact]
    public void ToDetailDtos_WithMultipleEntities_ShouldMapAll()
    {
        // Arrange
        var entities = new List<Herb>
        {
            CreateTestHerb("当归", 5.50m),
            CreateTestHerb("人参", 10.00m)
        };

        // Act
        var dtos = _mapper.ToDetailDtos(entities);

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].Name.Should().Be("当归");
        dtos[1].Name.Should().Be("人参");
    }

    #endregion

    #region ToEntity 测试

    [Fact]
    public void ToEntity_WithValidDto_ShouldMapNonIgnoredProperties()
    {
        // Arrange
        var dto = new HerbInputDto
        {
            Id = Guid.NewGuid(), // Should be ignored
            Name = "当归",
            PinYinCode = "DG",
            Category = "补血药",
            Properties = "甘、温",
            Origin = "甘肃",
            Spec = "特级",
            Unit = "克",
            Price = 5.50m,
            CostPrice = 3.00m,
            Effect = "补血活血",
            Usage = "3-9克",
            Remark = "贵重药材"
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Should().NotBeNull();
        // Id should be ignored (default Guid)
        entity.Id.Should().NotBe(dto.Id!.Value);
        entity.Name.Should().Be(dto.Name);
        entity.PinYinCode.Should().Be(dto.PinYinCode);
        entity.Category.Should().Be(dto.Category);
        entity.Properties.Should().Be(dto.Properties);
        entity.Origin.Should().Be(dto.Origin);
        entity.Spec.Should().Be(dto.Spec);
        entity.Unit.Should().Be(dto.Unit);
        entity.Price.Should().Be(dto.Price);
        entity.CostPrice.Should().Be(dto.CostPrice);
        entity.Effect.Should().Be(dto.Effect);
        entity.Usage.Should().Be(dto.Usage);
        entity.Remark.Should().Be(dto.Remark);
    }

    [Fact]
    public void ToEntity_ShouldIgnoreAuditFields()
    {
        // Arrange
        var dto = new HerbInputDto
        {
            Name = "当归",
            Unit = "克",
            Price = 5.50m
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Status.Should().Be(CommonStatus.Enabled); // Ignored (constructor default, not mapped)
        entity.CreatedAt.Should().BeAfter(DateTime.MinValue); // Constructor default = UtcNow
        entity.CreatedBy.Should().BeNull(); // Ignored
        entity.UpdatedAt.Should().BeNull(); // Ignored
        entity.UpdatedBy.Should().BeNull(); // Ignored
        entity.RowVersion.Should().BeNull(); // Ignored
        entity.IsDeleted.Should().Be(false); // Ignored (default)
    }

    #endregion

    #region UpdateEntity 测试

    [Fact]
    public void UpdateEntity_WithValidDto_ShouldUpdateNonIgnoredProperties()
    {
        // Arrange
        var existingEntity = CreateTestHerb();
        var originalId = existingEntity.Id;
        var originalCreatedAt = existingEntity.CreatedAt;

        var dto = new HerbInputDto
        {
            Id = Guid.NewGuid(), // Should be ignored
            Name = "更新后的当归",
            PinYinCode = "GXDGH",
            Category = "补血药",
            Properties = "甘、温",
            Origin = "甘肃",
            Spec = "特级",
            Unit = "克",
            Price = 6.00m,
            CostPrice = 3.50m,
            Effect = "补血活血",
            Usage = "3-9克",
            Remark = "更新备注"
        };

        // Act
        _mapper.UpdateEntity(dto, existingEntity);

        // Assert
        existingEntity.Id.Should().Be(originalId); // Ignored
        existingEntity.Name.Should().Be(dto.Name);
        existingEntity.PinYinCode.Should().Be(dto.PinYinCode);
        existingEntity.Category.Should().Be(dto.Category);
        existingEntity.Properties.Should().Be(dto.Properties);
        existingEntity.Origin.Should().Be(dto.Origin);
        existingEntity.Spec.Should().Be(dto.Spec);
        existingEntity.Unit.Should().Be(dto.Unit);
        existingEntity.Price.Should().Be(dto.Price);
        existingEntity.CostPrice.Should().Be(dto.CostPrice);
        existingEntity.Effect.Should().Be(dto.Effect);
        existingEntity.Usage.Should().Be(dto.Usage);
        existingEntity.Remark.Should().Be(dto.Remark);
    }

    [Fact]
    public void UpdateEntity_ShouldIgnoreAuditFields()
    {
        // Arrange
        var existingEntity = CreateTestHerb();
        var originalCreatedAt = existingEntity.CreatedAt;
        var originalCreatedBy = existingEntity.CreatedBy;

        var dto = new HerbInputDto
        {
            Name = "当归",
            Unit = "克",
            Price = 5.50m
        };

        // Act
        _mapper.UpdateEntity(dto, existingEntity);

        // Assert
        existingEntity.Status.Should().Be(CommonStatus.Enabled); // Ignored (existing entity unchanged)
        existingEntity.CreatedAt.Should().Be(originalCreatedAt); // Ignored
        existingEntity.CreatedBy.Should().Be(originalCreatedBy); // Ignored
        existingEntity.UpdatedAt.Should().BeNull(); // Ignored
        existingEntity.UpdatedBy.Should().BeNull(); // Ignored
        existingEntity.RowVersion.Should().BeNull(); // Ignored
        existingEntity.IsDeleted.Should().Be(false); // Ignored
    }

    #endregion

    #region ToEntityFromImport 测试

    [Fact]
    public void ToEntityFromImport_WithValidDto_ShouldMapBasicProperties()
    {
        // Arrange
        var dto = new HerbImportItemDto
        {
            Name = "当归",
            Unit = "克",
            Price = 5.50m,
            Origin = "甘肃",
            Spec = "特级",
            Effect = "补血活血",
            Remark = "贵重药材"
        };

        // Act
        var entity = _mapper.ToEntityFromImport(dto);

        // Assert
        entity.Should().NotBeNull();
        entity.Name.Should().Be(dto.Name);
        entity.Unit.Should().Be(dto.Unit);
        entity.Price.Should().Be(dto.Price);
        entity.Origin.Should().Be(dto.Origin);
        entity.Spec.Should().Be(dto.Spec);
        entity.Effect.Should().Be(dto.Effect);
    }

    [Fact]
    public void ToEntityFromImport_ShouldIgnoreManyFields()
    {
        // Arrange
        var dto = new HerbImportItemDto
        {
            Name = "当归",
            Unit = "克",
            Price = 5.50m
        };

        // Act
        var entity = _mapper.ToEntityFromImport(dto);

        // Assert
        entity.Id.Should().NotBe(Guid.Empty); // Constructor default = Guid.NewGuid()
        entity.Status.Should().Be(CommonStatus.Enabled); // Constructor default, not mapped
        entity.Properties.Should().BeNull(); // Ignored
        entity.Usage.Should().BeNull(); // Ignored
        entity.PinYinCode.Should().BeNull(); // Ignored
        entity.CostPrice.Should().BeNull(); // Ignored
        entity.Category.Should().BeNull(); // Ignored
        entity.CreatedAt.Should().BeAfter(DateTime.MinValue); // Constructor default = UtcNow
        entity.CreatedBy.Should().BeNull(); // Ignored
        entity.UpdatedAt.Should().BeNull(); // Ignored
        entity.UpdatedBy.Should().BeNull(); // Ignored
        entity.RowVersion.Should().BeNull(); // Ignored
        entity.IsDeleted.Should().Be(false); // Ignored (default)
    }

    #endregion

    #region 辅助方法

    private static Herb CreateTestHerb(string name = "当归", decimal price = 5.50m)
    {
        return new Herb
        {
            Id = Guid.NewGuid(),
            Name = name,
            PinYinCode = "DG",
            Category = "补血药",
            Properties = "甘、温",
            Origin = "甘肃",
            Spec = "特级",
            Unit = "克",
            Price = price,
            CostPrice = 3.00m,
            Effect = "补血活血",
            Usage = "3-9克",
            Remark = "贵重药材",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
    }

    #endregion
}
