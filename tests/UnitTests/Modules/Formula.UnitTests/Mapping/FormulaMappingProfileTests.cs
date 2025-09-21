using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Formula;
using LYBT.Module.Formula.Mapping;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.Formula.Tests.Mapping
{
    /// <summary>
    /// Formula模块AutoMapper映射配置单元测试
    /// 测试所有映射配置的有效性和正确性
    /// </summary>
    public class FormulaMappingProfileTests
    {
        private readonly IMapper _mapper;

        public FormulaMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new FormulaMappingProfile());
            }, NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new FormulaMappingProfile());
            }, NullLoggerFactory.Instance);

            // Assert
            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void Map_Formula_To_FormulaDto_Should_Success()
        {
            // Arrange
            var formula = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "逍遥散",
                Category = "和解剂",
                Source = "太平惠民和剂局方",
                Composition = "柴胡、当归、白芍、白术、茯苓、甘草、薄荷、生姜",
                Usage = "水煎服，每日2次",
                Function = "疏肝解郁，健脾和营",
                Indication = "肝郁脾虚证",
                Status = CommonStatus.Enabled,
                Property = "和解剂"
            };

            // Act
            var dto = _mapper.Map<FormulaDto>(formula);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(formula.Id);
            dto.Name.Should().Be(formula.Name);
            dto.Category.Should().Be(formula.Category);
            dto.Source.Should().Be(formula.Source);
            dto.Composition.Should().Be(formula.Composition);
            dto.Usage.Should().Be(formula.Usage);
            dto.Function.Should().Be(formula.Function);
            dto.Indication.Should().Be(formula.Indication);
            dto.Status.Should().Be(formula.Status);

            // 验证计算属性被忽略
            dto.HerbCount.Should().Be(0);
            dto.TotalPrice.Should().Be(0);
        }

        [Fact]
        public void Map_Formula_To_FormulaDetailDto_Should_Success()
        {
            // Arrange
            var formula = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "四君子汤",
                Category = "补益剂",
                Source = "太平惠民和剂局方",
                Composition = "人参、白术、茯苓、甘草",
                Usage = "水煎服",
                Function = "益气健脾",
                Indication = "脾胃气虚证",
                Status = CommonStatus.Enabled,
                Property = "补气剂"
            };

            // Act
            var detailDto = _mapper.Map<FormulaDetailDto>(formula);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(formula.Id);
            detailDto.Name.Should().Be(formula.Name);
            detailDto.Category.Should().Be(formula.Category);
            detailDto.Source.Should().Be(formula.Source);
            detailDto.Composition.Should().Be(formula.Composition);
            detailDto.Usage.Should().Be(formula.Usage);
            detailDto.Function.Should().Be(formula.Function);
            detailDto.Indication.Should().Be(formula.Indication);
            detailDto.Status.Should().Be(formula.Status);

            // 验证计算属性被忽略
            detailDto.HerbCount.Should().Be(0);
            detailDto.TotalPrice.Should().Be(0);
            detailDto.Herbs.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Map_FormulaCreateDto_To_Formula_Should_Success()
        {
            // Arrange
            var createDto = new FormulaCreateDto
            {
                Name = "六味地黄丸",
                Category = "补益剂",
                Source = "小儿药证直诀",
                Composition = "地黄、山药、山茱萸、茯苓、泽泻、牡丹皮",
                Usage = "口服，每次8粒",
                Function = "滋阴补肾",
                Indication = "肾阴亏损证",
                // 这些字段应该被忽略，因为实体中不存在
                Instructions = "温开水送服",
                Indications = "头晕耳鸣",
                Contraindications = "脾虚湿重者慎用",
                Preparation = "蜜丸"
            };

            // Act
            var formula = _mapper.Map<LYBT.Entities.Formula.Formula>(createDto);

            // Assert
            formula.Should().NotBeNull();
            formula.Name.Should().Be(createDto.Name);
            formula.Category.Should().Be(createDto.Category);
            formula.Source.Should().Be(createDto.Source);
            formula.Composition.Should().Be(createDto.Composition);
            formula.Usage.Should().Be(createDto.Usage);
            formula.Function.Should().Be(createDto.Function);
            formula.Indication.Should().Be(createDto.Indication);

            // 验证忽略字段和默认值
            formula.Id.Should().Be(Guid.Empty);
            formula.Status.Should().Be(CommonStatus.Enabled);
            formula.Property.Should().BeNull();
        }

        [Fact]
        public void Map_FormulaUpdateDto_To_Formula_Should_Success()
        {
            // Arrange
            var updateDto = new FormulaUpdateDto
            {
                Id = Guid.NewGuid(),
                Name = "更新的方剂",
                Category = "更新类别",
                Source = "更新来源",
                Composition = "更新组成",
                Usage = "更新用法",
                Function = "更新功能",
                Indication = "更新主治",
                // 这些字段应该被忽略
                Instructions = "更新说明",
                Indications = "更新适应症",
                Contraindications = "更新禁忌症",
                Preparation = "更新制法"
            };

            // Act
            var formula = _mapper.Map<LYBT.Entities.Formula.Formula>(updateDto);

            // Assert
            formula.Should().NotBeNull();
            formula.Name.Should().Be(updateDto.Name);
            formula.Category.Should().Be(updateDto.Category);
            formula.Source.Should().Be(updateDto.Source);
            formula.Composition.Should().Be(updateDto.Composition);
            formula.Usage.Should().Be(updateDto.Usage);
            formula.Function.Should().Be(updateDto.Function);
            formula.Indication.Should().Be(updateDto.Indication);

            // 验证忽略字段
            formula.Id.Should().Be(Guid.Empty);
            formula.Status.Should().Be(CommonStatus.Disabled); // 默认值
            formula.Property.Should().BeNull();
            formula.Herbs.Should().BeNull();
        }

        [Fact]
        public void Map_FormulaHerbItem_To_FormulaHerbItemDto_Should_Success()
        {
            // Arrange
            var herbItem = new FormulaHerbItem
            {
                Id = Guid.NewGuid(),
                FormulaId = Guid.NewGuid(),
                HerbId = Guid.NewGuid(),
                Quantity = 12.0m,
                Unit = "g",
                Remark = "君药"
            };

            // Act
            var dto = _mapper.Map<FormulaHerbItemDto>(herbItem);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(herbItem.Id);
            dto.FormulaId.Should().Be(herbItem.FormulaId);
            dto.HerbId.Should().Be(herbItem.HerbId);
            dto.Quantity.Should().Be(herbItem.Quantity);
            dto.Unit.Should().Be(herbItem.Unit);
            dto.Remark.Should().Be(herbItem.Remark);

            // HerbName需要从关联获取，所以为空字符串
            dto.HerbName.Should().Be(string.Empty);
        }

        [Fact]
        public void Map_FormulaHerbItemCreateDto_To_FormulaHerbItem_Should_Success()
        {
            // Arrange
            var createDto = new FormulaHerbItemCreateDto
            {
                HerbId = Guid.NewGuid(),
                Quantity = 9.0m,
                Unit = "g",
                Remark = "臣药"
            };

            // Act
            var herbItem = _mapper.Map<FormulaHerbItem>(createDto);

            // Assert
            herbItem.Should().NotBeNull();
            herbItem.HerbId.Should().Be(createDto.HerbId);
            herbItem.Quantity.Should().Be(createDto.Quantity);
            herbItem.Unit.Should().Be(createDto.Unit);
            herbItem.Remark.Should().Be(createDto.Remark);
        }

        [Fact]
        public void Map_FormulaHerbItemUpdateDto_To_FormulaHerbItem_Should_Success()
        {
            // Arrange
            var updateDto = new FormulaHerbItemUpdateDto
            {
                Id = Guid.NewGuid(),
                HerbId = Guid.NewGuid(),
                Quantity = 6.0m,
                Unit = "g",
                Remark = "佐药"
            };

            // Act
            var herbItem = _mapper.Map<FormulaHerbItem>(updateDto);

            // Assert
            herbItem.Should().NotBeNull();
            herbItem.HerbId.Should().Be(updateDto.HerbId);
            herbItem.Quantity.Should().Be(updateDto.Quantity);
            herbItem.Unit.Should().Be(updateDto.Unit);
            herbItem.Remark.Should().Be(updateDto.Remark);
        }

        [Fact]
        public void Map_Formula_With_DisabledStatus_Should_Success()
        {
            // Arrange
            var formula = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "禁用方剂",
                Status = CommonStatus.Disabled
            };

            // Act
            var dto = _mapper.Map<FormulaDto>(formula);

            // Assert
            dto.Should().NotBeNull();
            dto.Name.Should().Be("禁用方剂");
            dto.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public void Map_Formula_With_NullOptionalFields_Should_Success()
        {
            // Arrange
            var formula = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "简单方剂",
                Category = null,
                Source = null,
                Composition = null,
                Usage = null,
                Function = null,
                Indication = null,
                Status = CommonStatus.Enabled,
                Property = null
            };

            // Act
            var dto = _mapper.Map<FormulaDto>(formula);

            // Assert
            dto.Should().NotBeNull();
            dto.Name.Should().Be("简单方剂");
            dto.Category.Should().BeNull();
            dto.Source.Should().BeNull();
            dto.Composition.Should().BeNull();
            dto.Usage.Should().BeNull();
            dto.Function.Should().BeNull();
            dto.Indication.Should().BeNull();
            dto.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void Map_FormulaHerbItem_With_SmallQuantity_Should_Success()
        {
            // Arrange
            var herbItem = new FormulaHerbItem
            {
                Id = Guid.NewGuid(),
                HerbId = Guid.NewGuid(),
                Quantity = 1.5m,
                Unit = "g",
                Remark = "使药"
            };

            // Act
            var dto = _mapper.Map<FormulaHerbItemDto>(herbItem);

            // Assert
            dto.Should().NotBeNull();
            dto.Quantity.Should().Be(1.5m);
            dto.Unit.Should().Be("g");
            dto.Remark.Should().Be("使药");
        }

        [Fact]
        public void Map_FormulaCreateDto_With_NullFields_Should_Success()
        {
            // Arrange
            var createDto = new FormulaCreateDto
            {
                Name = "最简方剂",
                Category = null,
                Source = null,
                Composition = null,
                Usage = null,
                Function = null,
                Indication = null
            };

            // Act
            var formula = _mapper.Map<LYBT.Entities.Formula.Formula>(createDto);

            // Assert
            formula.Should().NotBeNull();
            formula.Name.Should().Be("最简方剂");
            formula.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void Map_FormulaUpdateDto_With_NullValues_Should_OnlyMapNonNullFields()
        {
            // Arrange
            var updateDto = new FormulaUpdateDto
            {
                Id = Guid.NewGuid(),
                Name = "部分更新方剂",
                Category = null,
                Source = "新来源",
                Composition = null,
                Usage = "新用法",
                Function = null,
                Indication = null
            };

            // Act
            var formula = _mapper.Map<LYBT.Entities.Formula.Formula>(updateDto);

            // Assert
            formula.Should().NotBeNull();
            formula.Name.Should().Be("部分更新方剂");
            formula.Category.Should().BeNull();
            formula.Source.Should().Be("新来源");
            formula.Composition.Should().BeNull();
            formula.Usage.Should().Be("新用法");
            formula.Function.Should().BeNull();
            formula.Indication.Should().BeNull();
        }
    }
}