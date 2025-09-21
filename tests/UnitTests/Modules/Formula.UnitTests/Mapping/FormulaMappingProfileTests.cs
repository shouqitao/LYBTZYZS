using System;
using System.Collections.Generic;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Formula;
using LYBT.Module.Formula.Mapping;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
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
            });

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new FormulaMappingProfile());
            });

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
                Effect = "疏肝解郁，健脾和营",
                Usage = "水煎服，每日2次",
                Property = "和解剂",
                Remark = "肝郁脾虚证",
                Status = CommonStatus.Enabled,
                IsShared = true,
                Herbs = new List<FormulaHerbItem>()
            };

            // Act
            var dto = _mapper.Map<FormulaDto>(formula);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(formula.Id);
            dto.Name.Should().Be(formula.Name);
            dto.Effect.Should().Be(formula.Effect);
            dto.Usage.Should().Be(formula.Usage);
            dto.Property.Should().Be(formula.Property);
            dto.Remark.Should().Be(formula.Remark);
            dto.IsShared.Should().Be(formula.IsShared);
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
                Effect = "益气健脾",
                Usage = "水煎服",
                Property = "补气剂",
                Remark = "脾胃气虚证",
                Status = CommonStatus.Enabled,
                IsShared = false,
                Herbs = new List<FormulaHerbItem>()
            };

            // Act
            var detailDto = _mapper.Map<FormulaDetailDto>(formula);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(formula.Id);
            detailDto.Name.Should().Be(formula.Name);
            detailDto.Effect.Should().Be(formula.Effect);
            detailDto.Usage.Should().Be(formula.Usage);
            detailDto.Property.Should().Be(formula.Property);
            detailDto.Remark.Should().Be(formula.Remark);
            detailDto.IsShared.Should().Be(formula.IsShared);
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
                Effect = "滋阴补肾",
                Usage = "口服，每次8粒",
                IsShared = false,
                Instructions = "温开水送服",
                Indications = "头晕耳鸣，肾阴亏损证",
                Contraindications = "脾虚湿重者慎用",
                Preparation = "蜜丸",
                Remark = "小儿药证直诀",
                Herbs = new List<FormulaHerbItemCreateDto>()
            };

            // Act
            var formula = _mapper.Map<LYBT.Entities.Formula.Formula>(createDto);

            // Assert
            formula.Should().NotBeNull();
            formula.Name.Should().Be(createDto.Name);
            formula.Effect.Should().Be(createDto.Effect);
            formula.Usage.Should().Be(createDto.Usage);
            formula.IsShared.Should().Be(createDto.IsShared);
            formula.Remark.Should().Be(createDto.Remark);

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
                Effect = "更新功效",
                Usage = "更新用法",
                IsShared = true,
                Instructions = "更新说明",
                Indications = "更新适应症",
                Contraindications = "更新禁忌症",
                Preparation = "更新制法",
                Remark = "更新备注",
                Herbs = new List<FormulaHerbItemUpdateDto>()
            };

            // Act
            var formula = _mapper.Map<LYBT.Entities.Formula.Formula>(updateDto);

            // Assert
            formula.Should().NotBeNull();
            formula.Name.Should().Be(updateDto.Name);
            formula.Effect.Should().Be(updateDto.Effect);
            formula.Usage.Should().Be(updateDto.Usage);
            formula.IsShared.Should().Be(updateDto.IsShared);
            formula.Remark.Should().Be(updateDto.Remark);

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
                HerbId = Guid.NewGuid(),
                HerbName = "当归",
                Quantity = 12,
                Unit = "g",
                Usage = "后下",
                Remark = "君药"
            };

            // Act
            var dto = _mapper.Map<FormulaHerbItemDto>(herbItem);

            // Assert
            dto.Should().NotBeNull();
            dto.HerbId.Should().Be(herbItem.HerbId);
            dto.HerbName.Should().Be(herbItem.HerbName);
            dto.Quantity.Should().Be(herbItem.Quantity);
            dto.Unit.Should().Be(herbItem.Unit);
            dto.Usage.Should().Be(herbItem.Usage);
        }

        [Fact]
        public void Map_FormulaHerbItemCreateDto_To_FormulaHerbItem_Should_Success()
        {
            // Arrange
            var createDto = new FormulaHerbItemCreateDto
            {
                HerbId = Guid.NewGuid(),
                Quantity = 9.0m,
                Preparation = "酒炒",
                Usage = "先煎",
                SortOrder = 1
            };

            // Act
            var herbItem = _mapper.Map<FormulaHerbItem>(createDto);

            // Assert
            herbItem.Should().NotBeNull();
            herbItem.HerbId.Should().Be(createDto.HerbId);
            herbItem.Quantity.Should().Be((int)createDto.Quantity);
            herbItem.Usage.Should().Be(createDto.Usage);
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
                Preparation = "醋炙",
                Usage = "后下",
                SortOrder = 2
            };

            // Act
            var herbItem = _mapper.Map<FormulaHerbItem>(updateDto);

            // Assert
            herbItem.Should().NotBeNull();
            herbItem.HerbId.Should().Be(updateDto.HerbId);
            herbItem.Quantity.Should().Be((int)updateDto.Quantity);
            herbItem.Usage.Should().Be(updateDto.Usage);
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
                Effect = null,
                Usage = null,
                Property = null,
                Remark = null,
                IsShared = false,
                Status = CommonStatus.Enabled,
                Herbs = new List<FormulaHerbItem>()
            };

            // Act
            var dto = _mapper.Map<FormulaDto>(formula);

            // Assert
            dto.Should().NotBeNull();
            dto.Name.Should().Be("简单方剂");
            dto.Effect.Should().BeNull();
            dto.Usage.Should().BeNull();
            dto.Property.Should().BeNull();
            dto.Remark.Should().BeNull();
            dto.IsShared.Should().BeFalse();
            dto.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void Map_FormulaHerbItem_With_SmallQuantity_Should_Success()
        {
            // Arrange
            var herbItem = new FormulaHerbItem
            {
                HerbId = Guid.NewGuid(),
                HerbName = "甘草",
                Quantity = 2,
                Unit = "g",
                Usage = "另煎",
                Remark = "使药"
            };

            // Act
            var dto = _mapper.Map<FormulaHerbItemDto>(herbItem);

            // Assert
            dto.Should().NotBeNull();
            dto.Quantity.Should().Be(2);
            dto.Unit.Should().Be("g");
            dto.Usage.Should().Be("另煎");
        }

        [Fact]
        public void Map_FormulaCreateDto_With_NullFields_Should_Success()
        {
            // Arrange
            var createDto = new FormulaCreateDto
            {
                Name = "最简方剂",
                Effect = "",
                Usage = "",
                Instructions = null,
                Indications = null,
                Contraindications = null,
                Preparation = null,
                Remark = null,
                Herbs = new List<FormulaHerbItemCreateDto>()
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
                Effect = "新功效",
                Usage = "新用法",
                Instructions = null,
                Indications = null,
                Contraindications = null,
                Preparation = null,
                Remark = "新来源",
                Herbs = new List<FormulaHerbItemUpdateDto>()
            };

            // Act
            var formula = _mapper.Map<LYBT.Entities.Formula.Formula>(updateDto);

            // Assert
            formula.Should().NotBeNull();
            formula.Name.Should().Be("部分更新方剂");
            formula.Effect.Should().Be("新功效");
            formula.Usage.Should().Be("新用法");
            formula.Remark.Should().Be("新来源");
        }
    }
}