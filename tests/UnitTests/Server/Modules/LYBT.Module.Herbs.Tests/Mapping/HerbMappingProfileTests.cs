using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Mapping;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
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
            });

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new HerbMappingProfile());
            });

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
                Origin = "甘肃",
                Spec = "统货",
                Effect = "补血活血，调经止痛",
                Usage = "煎服，6-12g",
                Price = 0.8m,
                CostPrice = 0.5m,
                Unit = "g",
                Remark = "当归身补血，当归尾活血",
                Status = CommonStatus.Enabled
            };

            // Act
            var detailDto = _mapper.Map<HerbDetailDto>(herb);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(herb.Id);
            detailDto.Name.Should().Be(herb.Name);
            detailDto.PinYinCode.Should().Be(herb.PinYinCode);
            detailDto.Origin.Should().Be(herb.Origin);
            detailDto.Spec.Should().Be(herb.Spec);
            detailDto.Effect.Should().Be(herb.Effect);
            detailDto.Usage.Should().Be(herb.Usage);
            detailDto.Price.Should().Be(herb.Price);
            // CostPrice 不在 HerbDetailDto 中
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
                Origin = "内蒙古",
                Spec = "片",
                Effect = "补气升阳，固表止汗",
                Usage = "煎服，9-30g",
                Price = 0.6m,
                Unit = "g",
                Remark = "生用补气，炙用温中"
            };

            // Act
            var herb = _mapper.Map<Herb>(createDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be(createDto.Name);
            herb.PinYinCode.Should().Be(createDto.PinYinCode);
            herb.Origin.Should().Be(createDto.Origin);
            herb.Spec.Should().Be(createDto.Spec);
            herb.Effect.Should().Be(createDto.Effect);
            herb.Usage.Should().Be(createDto.Usage);
            herb.Price.Should().Be(createDto.Price);
            herb.Unit.Should().Be(createDto.Unit);
            herb.Remark.Should().Be(createDto.Remark);

            // 验证忽略字段 - BaseEntity.Id 有默认初始化为 Guid.NewGuid()
            herb.Id.Should().NotBe(Guid.Empty);
            herb.Status.Should().Be(CommonStatus.Enabled);
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
                Origin = "吉林",
                Spec = "统货",
                Effect = "大补元气，复脉固脱",
                Usage = "煎服，3-9g",
                Price = 15.0m,
                Unit = "g",
                Remark = "野山参效果最佳",
                Status = CommonStatus.Enabled
            };

            // Act
            var herb = _mapper.Map<Herb>(updateDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be(updateDto.Name);
            herb.PinYinCode.Should().Be(updateDto.PinYinCode);
            herb.Origin.Should().Be(updateDto.Origin);
            herb.Spec.Should().Be(updateDto.Spec);
            herb.Effect.Should().Be(updateDto.Effect);
            herb.Usage.Should().Be(updateDto.Usage);
            herb.Price.Should().Be(updateDto.Price);
            herb.Unit.Should().Be(updateDto.Unit);
            herb.Remark.Should().Be(updateDto.Remark);

            // 验证忽略字段 - BaseEntity.Id 有默认初始化为 Guid.NewGuid()
            herb.Id.Should().NotBe(Guid.Empty);
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
                Origin = "内蒙古",
                Spec = "统货",
                Effect = "补脾益气，清热解毒",
                Usage = "煎服，3-10g",
                Price = 0.3m,
                CostPrice = 0.2m,
                Unit = "g",
                Remark = "脾胃虚弱，咳嗽痰多",
                Status = CommonStatus.Enabled
            };

            // Act
            var dto = _mapper.Map<HerbDto>(herb);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(herb.Id);
            dto.Name.Should().Be(herb.Name);
            dto.PinYinCode.Should().Be(herb.PinYinCode);
            dto.Origin.Should().Be(herb.Origin);
            dto.Spec.Should().Be(herb.Spec);
            dto.Effect.Should().Be(herb.Effect);
            dto.Usage.Should().Be(herb.Usage);
            dto.Price.Should().Be(herb.Price);
            dto.CostPrice.Should().Be(herb.CostPrice);
            dto.Unit.Should().Be(herb.Unit);
            dto.Remark.Should().Be(herb.Remark);
        }

        [Fact]
        public void Map_HerbImportDto_To_Herb_Should_Success()
        {
            // Arrange
            var importDto = new HerbImportDto
            {
                Name = "川芎",
                Origin = "四川",
                Spec = "片",
                Effect = "活血行气，祛风止痛",
                Unit = "g",
                Price = 1.2m,
                BatchNo = "2025-01",
                ExpireDate = DateTime.Now.AddYears(2),
                Remark = "川芎辛散温燥，阴虚火旺者慎用"
            };

            // Act
            var herb = _mapper.Map<Herb>(importDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be(importDto.Name);
            herb.Origin.Should().Be(importDto.Origin);
            herb.Spec.Should().Be(importDto.Spec);
            herb.Effect.Should().Be(importDto.Effect);
            herb.Unit.Should().Be(importDto.Unit);
            herb.Price.Should().Be(importDto.Price);
            herb.Remark.Should().Be(importDto.Remark);

            // 验证忽略字段 - BaseEntity.Id 有默认初始化为 Guid.NewGuid()
            herb.Id.Should().NotBe(Guid.Empty);
            herb.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void Map_Herb_With_SpecialPrice_Should_Success()
        {
            // Arrange
            var herb = new Herb
            {
                Id = Guid.NewGuid(),
                Name = "黄连",
                Origin = "四川",
                Spec = "统货",
                Effect = "清热燥湿，泻火解毒",
                Price = 8.0m,
                CostPrice = 5.0m,
                Unit = "g",
                Status = CommonStatus.Enabled
            };

            // Act
            var dto = _mapper.Map<HerbDto>(herb);

            // Assert
            dto.Should().NotBeNull();
            dto.Name.Should().Be("黄连");
            dto.Origin.Should().Be("四川");
            dto.Effect.Should().Be("清热燥湿，泻火解毒");
            dto.Price.Should().Be(8.0m);
            dto.CostPrice.Should().Be(5.0m);
        }

        [Fact]
        public void Map_Herb_With_HighPrice_Should_Success()
        {
            // Arrange
            var herb = new Herb
            {
                Id = Guid.NewGuid(),
                Name = "附子",
                Origin = "四川",
                Spec = "制附片",
                Effect = "回阳救逆，补火助阳",
                Usage = "煎服，3-15g",
                Price = 10.0m,
                Remark = "孕妇禁用，有毒",
                Status = CommonStatus.Enabled
            };

            // Act
            var detailDto = _mapper.Map<HerbDetailDto>(herb);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Name.Should().Be("附子");
            detailDto.Effect.Should().Be("回阳救逆，补火助阳");
            detailDto.Remark.Should().Be("孕妇禁用，有毒");
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
                Origin = null,
                Spec = null,
                Effect = null,
                Usage = null,
                Price = 1.0m,
                CostPrice = null,
                Unit = "g",
                Remark = null,
                Status = CommonStatus.Enabled
            };

            // Act
            var dto = _mapper.Map<HerbDto>(herb);

            // Assert
            dto.Should().NotBeNull();
            dto.Name.Should().Be("简单药材");
            dto.PinYinCode.Should().Be("JDYC");
            dto.Origin.Should().BeNull();
            dto.Spec.Should().BeNull();
            dto.Effect.Should().BeNull();
            dto.Usage.Should().BeNull();
            dto.Price.Should().Be(1.0m);
            dto.CostPrice.Should().BeNull();
            dto.Unit.Should().Be("g");
            dto.Remark.Should().BeNull();
        }

        [Fact]
        public void Map_HerbCreateDto_With_CompleteData_Should_Success()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "柴胡",
                PinYinCode = "CH",
                Origin = "湖北",
                Spec = "片",
                Effect = "疏肝解郁，升举阳气",
                Usage = "煎服，3-10g",
                Price = 2.5m,
                CostPrice = 1.5m,
                Unit = "g"
            };

            // Act
            var herb = _mapper.Map<Herb>(createDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be("柴胡");
            herb.PinYinCode.Should().Be("CH");
            herb.Origin.Should().Be("湖北");
            herb.Spec.Should().Be("片");
            herb.Effect.Should().Be("疏肝解郁，升举阳气");
            herb.Usage.Should().Be("煎服，3-10g");
            herb.Price.Should().Be(2.5m);
        }

        [Fact]
        public void Map_HerbUpdateDto_With_HighPrice_Should_Success()
        {
            // Arrange
            var updateDto = new HerbUpdateDto
            {
                Id = Guid.NewGuid(),
                Name = "冬虫夏草",
                Origin = "西藏",
                Spec = "整条",
                Effect = "补肺益肾，止血化痰",
                Price = 500.0m,
                CostPrice = 400.0m,
                Unit = "g",
                Status = CommonStatus.Enabled
            };

            // Act
            var herb = _mapper.Map<Herb>(updateDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be("冬虫夏草");
            herb.Price.Should().Be(500.0m);
            herb.CostPrice.Should().Be(400.0m);
        }

        [Fact]
        public void Map_HerbImportDto_With_MinimalData_Should_Success()
        {
            // Arrange
            var importDto = new HerbImportDto
            {
                Name = "最简药材",
                Price = 0.1m,
                Unit = "g"
            };

            // Act
            var herb = _mapper.Map<Herb>(importDto);

            // Assert
            herb.Should().NotBeNull();
            herb.Name.Should().Be("最简药材");
            herb.Price.Should().Be(0.1m);
            herb.Unit.Should().Be("g");
        }
    }
}
