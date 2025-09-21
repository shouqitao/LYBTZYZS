using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Mapping;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.Herbs.Tests.Mapping
{
    /// <summary>
    /// Herbs模块AutoMapper映射配置单元测试
    /// 测试所有映射配置的有效性和正确性
    /// </summary>
    public class HerbMappingProfileTests
    {
        private readonly IMapper _mapper;

        public HerbMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new HerbMappingProfile());
            }, NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new HerbMappingProfile());
            }, NullLoggerFactory.Instance);

            // Assert
            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void Map_Herb_To_HerbDetailDto_Should_Success()
        {
            // Arrange
            var herb = new Herb
            {
                Id = Guid.NewGuid(),
                Name = "当归",
                PinYinCode = "DG",
                Category = "补血药",
                Nature = "温",
                Flavor = "甘、辛",
                Meridian = "肝、心、脾经",
                Efficacy = "补血活血，调经止痛",
                MainFunction = "血虚萎黄，月经不调",
                Usage = "煎服，6-12g",
                Precautions = "孕妇慎用",
                UnitPrice = 0.8m,
                Unit = "g",
                Remark = "当归身补血，当归尾活血"
            };

            // Act
            var detailDto = _mapper.Map<HerbDetailDto>(herb);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(herb.Id);
            detailDto.Name.Should().Be(herb.Name);
            detailDto.PinYinCode.Should().Be(herb.PinYinCode);
            detailDto.Category.Should().Be(herb.Category);
            detailDto.Nature.Should().Be(herb.Nature);
            detailDto.Flavor.Should().Be(herb.Flavor);
            detailDto.Meridian.Should().Be(herb.Meridian);
            detailDto.Efficacy.Should().Be(herb.Efficacy);
            detailDto.MainFunction.Should().Be(herb.MainFunction);
            detailDto.Usage.Should().Be(herb.Usage);
            detailDto.Precautions.Should().Be(herb.Precautions);
            detailDto.UnitPrice.Should().Be(herb.UnitPrice);
            detailDto.Unit.Should().Be(herb.Unit);
            detailDto.Remark.Should().Be(herb.Remark);
        }

        [Fact]
        public void Map_HerbCreateDto_To_Herb_Should_Success()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "黄芪",
                PinYinCode = "HQ",
                Category = "补气药",
                Nature = "微温",
                Flavor = "甘",
                Meridian = "肺、脾经",
                Efficacy = "补气升阳，固表止汗",
                MainFunction = "气虚乏力，中气下陷",
                Precautions = "阴虚阳亢者忌用",
                UnitPrice = 0.6m,
                Unit = "g",
                Remark = "生用补气，炙用温中"
            };

            // Act
            var herb = _mapper.Map<Herb>(createDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be(createDto.Name);
            herb.PinYinCode.Should().Be(createDto.PinYinCode);
            herb.Category.Should().Be(createDto.Category);
            herb.Nature.Should().Be(createDto.Nature);
            herb.Flavor.Should().Be(createDto.Flavor);
            herb.Meridian.Should().Be(createDto.Meridian);
            herb.Efficacy.Should().Be(createDto.Efficacy);
            herb.MainFunction.Should().Be(createDto.MainFunction);
            herb.Precautions.Should().Be(createDto.Precautions);
            herb.UnitPrice.Should().Be(createDto.UnitPrice);
            herb.Unit.Should().Be(createDto.Unit);
            herb.Remark.Should().Be(createDto.Remark);

            // 验证忽略字段
            herb.Id.Should().Be(Guid.Empty);
            herb.Usage.Should().BeNull();
        }

        [Fact]
        public void Map_HerbUpdateDto_To_Herb_Should_Success()
        {
            // Arrange
            var updateDto = new HerbUpdateDto
            {
                Id = Guid.NewGuid(),
                Name = "人参",
                PinYinCode = "RS",
                Category = "补气药",
                Nature = "微温",
                Flavor = "甘、微苦",
                Meridian = "肺、脾、心、肾经",
                Efficacy = "大补元气，复脉固脱",
                MainFunction = "气虚欲脱，心力衰竭",
                Usage = "煎服，3-9g",
                Precautions = "实热证忌用",
                UnitPrice = 15.0m,
                Unit = "g",
                Remark = "野山参效果最佳"
            };

            // Act
            var herb = _mapper.Map<Herb>(updateDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be(updateDto.Name);
            herb.PinYinCode.Should().Be(updateDto.PinYinCode);
            herb.Category.Should().Be(updateDto.Category);
            herb.Nature.Should().Be(updateDto.Nature);
            herb.Flavor.Should().Be(updateDto.Flavor);
            herb.Meridian.Should().Be(updateDto.Meridian);
            herb.Efficacy.Should().Be(updateDto.Efficacy);
            herb.MainFunction.Should().Be(updateDto.MainFunction);
            herb.Usage.Should().Be(updateDto.Usage);
            herb.Precautions.Should().Be(updateDto.Precautions);
            herb.UnitPrice.Should().Be(updateDto.UnitPrice);
            herb.Unit.Should().Be(updateDto.Unit);
            herb.Remark.Should().Be(updateDto.Remark);

            // 验证忽略字段
            herb.Id.Should().Be(Guid.Empty);
        }

        [Fact]
        public void Map_Herb_To_HerbDto_Should_Success()
        {
            // Arrange
            var herb = new Herb
            {
                Id = Guid.NewGuid(),
                Name = "甘草",
                PinYinCode = "GC",
                Category = "补气药",
                Nature = "平",
                Flavor = "甘",
                Meridian = "心、肺、脾、胃经",
                Efficacy = "补脾益气，清热解毒",
                MainFunction = "脾胃虚弱，咳嗽痰多",
                UnitPrice = 0.3m,
                Unit = "g"
            };

            // Act
            var dto = _mapper.Map<HerbDto>(herb);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(herb.Id);
            dto.Name.Should().Be(herb.Name);
            dto.PinYinCode.Should().Be(herb.PinYinCode);
            dto.Category.Should().Be(herb.Category);
            dto.Nature.Should().Be(herb.Nature);
            dto.Flavor.Should().Be(herb.Flavor);
            dto.Meridian.Should().Be(herb.Meridian);
            dto.Efficacy.Should().Be(herb.Efficacy);
            dto.MainFunction.Should().Be(herb.MainFunction);
            dto.UnitPrice.Should().Be(herb.UnitPrice);
            dto.Unit.Should().Be(herb.Unit);
        }

        [Fact]
        public void Map_HerbImportDto_To_Herb_Should_Success()
        {
            // Arrange
            var importDto = new HerbImportDto
            {
                Name = "川芎",
                PinYinCode = "CX",
                Category = "活血化瘀药",
                Nature = "温",
                Flavor = "辛",
                Meridian = "肝、胆、心包经",
                Efficacy = "活血行气，祛风止痛",
                MainFunction = "月经不调，头痛眩晕",
                Precautions = "阴虚火旺者慎用",
                UnitPrice = 1.2m,
                Unit = "g",
                Remark = "川芎辛散温燥"
            };

            // Act
            var herb = _mapper.Map<Herb>(importDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be(importDto.Name);
            herb.PinYinCode.Should().Be(importDto.PinYinCode);
            herb.Category.Should().Be(importDto.Category);
            herb.Nature.Should().Be(importDto.Nature);
            herb.Flavor.Should().Be(importDto.Flavor);
            herb.Meridian.Should().Be(importDto.Meridian);
            herb.Efficacy.Should().Be(importDto.Efficacy);
            herb.MainFunction.Should().Be(importDto.MainFunction);
            herb.Precautions.Should().Be(importDto.Precautions);
            herb.UnitPrice.Should().Be(importDto.UnitPrice);
            herb.Unit.Should().Be(importDto.Unit);
            herb.Remark.Should().Be(importDto.Remark);

            // 验证忽略字段
            herb.Id.Should().Be(Guid.Empty);
            herb.Usage.Should().BeNull();
        }

        [Fact]
        public void Map_Herb_With_ColdNature_Should_Success()
        {
            // Arrange
            var herb = new Herb
            {
                Id = Guid.NewGuid(),
                Name = "黄连",
                Category = "清热燥湿药",
                Nature = "寒",
                Flavor = "苦",
                Efficacy = "清热燥湿，泻火解毒",
                UnitPrice = 8.0m
            };

            // Act
            var dto = _mapper.Map<HerbDto>(herb);

            // Assert
            dto.Should().NotBeNull();
            dto.Name.Should().Be("黄连");
            dto.Nature.Should().Be("寒");
            dto.Flavor.Should().Be("苦");
            dto.UnitPrice.Should().Be(8.0m);
        }

        [Fact]
        public void Map_Herb_With_HotNature_Should_Success()
        {
            // Arrange
            var herb = new Herb
            {
                Id = Guid.NewGuid(),
                Name = "附子",
                Category = "温里药",
                Nature = "热",
                Flavor = "辛、甘",
                Efficacy = "回阳救逆，补火助阳",
                Precautions = "孕妇禁用，有毒"
            };

            // Act
            var detailDto = _mapper.Map<HerbDetailDto>(herb);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Name.Should().Be("附子");
            detailDto.Nature.Should().Be("热");
            detailDto.Precautions.Should().Be("孕妇禁用，有毒");
        }

        [Fact]
        public void Map_Herb_With_NullOptionalFields_Should_Success()
        {
            // Arrange
            var herb = new Herb
            {
                Id = Guid.NewGuid(),
                Name = "简单药材",
                PinYinCode = "JDYC",
                Category = null,
                Nature = null,
                Flavor = null,
                Meridian = null,
                Efficacy = null,
                MainFunction = null,
                Usage = null,
                Precautions = null,
                UnitPrice = 1.0m,
                Unit = "g",
                Remark = null
            };

            // Act
            var dto = _mapper.Map<HerbDto>(herb);

            // Assert
            dto.Should().NotBeNull();
            dto.Name.Should().Be("简单药材");
            dto.PinYinCode.Should().Be("JDYC");
            dto.Category.Should().BeNull();
            dto.Nature.Should().BeNull();
            dto.Flavor.Should().BeNull();
            dto.Meridian.Should().BeNull();
            dto.Efficacy.Should().BeNull();
            dto.MainFunction.Should().BeNull();
            dto.UnitPrice.Should().Be(1.0m);
            dto.Unit.Should().Be("g");
        }

        [Fact]
        public void Map_HerbCreateDto_With_ComplexMeridian_Should_Success()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "柴胡",
                PinYinCode = "CH",
                Category = "解表药",
                Nature = "微寒",
                Flavor = "苦、辛",
                Meridian = "肝、胆、心包、三焦经",
                Efficacy = "疏肝解郁，升举阳气",
                MainFunction = "肝郁气滞，月经不调",
                UnitPrice = 2.5m,
                Unit = "g"
            };

            // Act
            var herb = _mapper.Map<Herb>(createDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be("柴胡");
            herb.Meridian.Should().Be("肝、胆、心包、三焦经");
            herb.Flavor.Should().Be("苦、辛");
            herb.UnitPrice.Should().Be(2.5m);
        }

        [Fact]
        public void Map_HerbUpdateDto_With_HighPrice_Should_Success()
        {
            // Arrange
            var updateDto = new HerbUpdateDto
            {
                Id = Guid.NewGuid(),
                Name = "冬虫夏草",
                Category = "补阳药",
                Nature = "温",
                Flavor = "甘",
                UnitPrice = 500.0m,
                Unit = "g",
                Precautions = "价格昂贵，谨慎使用"
            };

            // Act
            var herb = _mapper.Map<Herb>(updateDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be("冬虫夏草");
            herb.UnitPrice.Should().Be(500.0m);
            herb.Precautions.Should().Be("价格昂贵，谨慎使用");
        }

        [Fact]
        public void Map_HerbImportDto_With_MinimalData_Should_Success()
        {
            // Arrange
            var importDto = new HerbImportDto
            {
                Name = "最简药材",
                UnitPrice = 0.1m,
                Unit = "g"
            };

            // Act
            var herb = _mapper.Map<Herb>(importDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be("最简药材");
            herb.UnitPrice.Should().Be(0.1m);
            herb.Unit.Should().Be("g");
        }
    }
}