using System;
using System.Collections.Generic;
using AutoMapper;
using Xunit;
using LYBT.Module.Herbs.Mapping;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Tests.Mapping
{
    /// <summary>
    /// Herb模块DTO映射验证测试
    /// UltraThink质量保证：确保Herb相关的所有DTO映射正确无误
    /// </summary>
    public class HerbDtoMappingValidationTests : BaseDtoMappingValidationTests
    {
        protected override IEnumerable<Profile> GetMappingProfiles()
        {
            yield return new HerbMappingProfile();
        }

        protected override IEnumerable<(Type Source, Type Destination)> GetMappingPairs()
        {
            // HerbModel ↔ HerbDto 双向映射
            yield return (typeof(HerbModel), typeof(HerbDto));
            yield return (typeof(HerbDto), typeof(HerbModel));

            // HerbCreateDto → HerbModel 单向映射
            yield return (typeof(HerbCreateDto), typeof(HerbModel));

            // HerbUpdateDto → HerbModel 单向映射
            yield return (typeof(HerbUpdateDto), typeof(HerbModel));

            // HerbModel → HerbDetailDto 单向映射
            yield return (typeof(HerbModel), typeof(HerbDetailDto));
        }

        /// <summary>
        /// 测试HerbModel到HerbDto的映射
        /// </summary>
        [Fact]
        public void MapHerbModelToDto_ShouldMapAllFields()
        {
            // Arrange
            var herbModel = new HerbModel
            {
                Id = Guid.NewGuid(),
                Name = "当归",
                Code = "DG001",
                PinYinCode = "DG",
                Category = "补血药",
                Unit = "g",
                Price = 15.50m,
                Stock = 500,
                MinStock = 50,
                Nature = "温",
                Taste = "甘、辛",
                Meridian = "肝、心、脾",
                Functions = "补血活血，调经止痛",
                Indications = "血虚萎黄，月经不调",
                Dosage = "6-15g",
                Contraindications = "月经过多者慎用",
                Manufacturer = "某制药厂",
                BatchNumber = "20250101",
                ExpiryDate = DateTime.Now.AddYears(2),
                Status = CommonStatus.Enabled,
                Remark = "道地药材",
                CreateTime = DateTime.Now.AddDays(-30),
                UpdateTime = DateTime.Now.AddHours(-1)
            };

            // Act
            var herbDto = _mapper.Map<HerbDto>(herbModel);

            // Assert - 验证所有重要字段都正确映射
            Assert.Equal(herbModel.Id, herbDto.Id);
            Assert.Equal(herbModel.Name, herbDto.Name);
            Assert.Equal(herbModel.Code, herbDto.Code);
            Assert.Equal(herbModel.PinYinCode, herbDto.PinYinCode);
            Assert.Equal(herbModel.Category, herbDto.Category);
            Assert.Equal(herbModel.Unit, herbDto.Unit);
            Assert.Equal(herbModel.Price, herbDto.Price);
            Assert.Equal(herbModel.Stock, herbDto.Stock);
            Assert.Equal(herbModel.Nature, herbDto.Nature);
            Assert.Equal(herbModel.Taste, herbDto.Taste);
            Assert.Equal(herbModel.Meridian, herbDto.Meridian);
            Assert.Equal(herbModel.Functions, herbDto.Functions);
            Assert.Equal(herbModel.Status.ToString(), herbDto.Status);
            Assert.Equal(herbModel.Remark, herbDto.Remark);
            Assert.Equal(herbModel.CreateTime, herbDto.CreateTime);
            Assert.Equal(herbModel.UpdateTime, herbDto.UpdateTime);
        }

        /// <summary>
        /// 测试HerbCreateDto到HerbModel的映射
        /// </summary>
        [Fact]
        public void MapHerbCreateDtoToModel_ShouldMapAllFields()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "黄芪",
                Code = "HQ002",
                Category = "补气药",
                Unit = "g",
                Price = 12.00m,
                Stock = 300,
                MinStock = 30,
                Nature = "微温",
                Taste = "甘",
                Meridian = "脾、肺",
                Functions = "补气升阳，固表止汗",
                Indications = "气虚乏力，脾虚泄泻",
                Dosage = "9-30g",
                Contraindications = "表实邪盛者忌用",
                Status = "Enabled",
                Remark = "优质黄芪"
            };

            // Act
            var herbModel = _mapper.Map<HerbModel>(createDto);

            // Assert - 验证创建映射的正确性
            Assert.Equal(createDto.Name, herbModel.Name);
            Assert.Equal(createDto.Code, herbModel.Code);
            Assert.Equal(createDto.Category, herbModel.Category);
            Assert.Equal(createDto.Unit, herbModel.Unit);
            Assert.Equal(createDto.Price, herbModel.Price);
            Assert.Equal(createDto.Stock, herbModel.Stock);
            Assert.Equal(createDto.MinStock, herbModel.MinStock);
            Assert.Equal(createDto.Nature, herbModel.Nature);
            Assert.Equal(createDto.Taste, herbModel.Taste);
            Assert.Equal(createDto.Meridian, herbModel.Meridian);
            Assert.Equal(createDto.Functions, herbModel.Functions);
            Assert.Equal(createDto.Indications, herbModel.Indications);
            Assert.Equal(createDto.Dosage, herbModel.Dosage);
            Assert.Equal(createDto.Contraindications, herbModel.Contraindications);
            Assert.Equal(CommonStatus.Enabled, herbModel.Status);
            Assert.Equal(createDto.Remark, herbModel.Remark);
        }

        /// <summary>
        /// 测试HerbUpdateDto到HerbModel的映射
        /// 这是新添加的UpdateAsync方法需要验证的重点
        /// </summary>
        [Fact]
        public void MapHerbUpdateDtoToModel_ShouldMapAllFields()
        {
            // Arrange
            var existingHerb = new HerbModel
            {
                Id = Guid.NewGuid(),
                Name = "原始名称",
                Code = "OLD001",
                Category = "原始分类",
                Price = 10.00m,
                Status = CommonStatus.Enabled
            };

            var updateDto = new HerbUpdateDto
            {
                Id = existingHerb.Id,
                Name = "更新名称",
                Code = "NEW001",
                Category = "更新分类",
                Unit = "克",
                Price = 20.00m,
                Stock = 200,
                MinStock = 20,
                Nature = "平",
                Taste = "苦",
                Meridian = "肝",
                Functions = "更新功效",
                Indications = "更新主治",
                Dosage = "3-9g",
                Contraindications = "更新禁忌",
                Manufacturer = "更新厂家",
                BatchNumber = "20250201",
                ExpiryDate = DateTime.Now.AddYears(3),
                Status = "Disabled",
                Remark = "更新备注"
            };

            // Act - 使用AutoMapper进行更新映射
            _mapper.Map(updateDto, existingHerb);

            // Assert - 验证所有字段都被正确更新
            Assert.Equal(updateDto.Id, existingHerb.Id);
            Assert.Equal(updateDto.Name, existingHerb.Name);
            Assert.Equal(updateDto.Code, existingHerb.Code);
            Assert.Equal(updateDto.Category, existingHerb.Category);
            Assert.Equal(updateDto.Unit, existingHerb.Unit);
            Assert.Equal(updateDto.Price, existingHerb.Price);
            Assert.Equal(updateDto.Stock, existingHerb.Stock);
            Assert.Equal(updateDto.MinStock, existingHerb.MinStock);
            Assert.Equal(updateDto.Nature, existingHerb.Nature);
            Assert.Equal(updateDto.Taste, existingHerb.Taste);
            Assert.Equal(updateDto.Meridian, existingHerb.Meridian);
            Assert.Equal(updateDto.Functions, existingHerb.Functions);
            Assert.Equal(updateDto.Indications, existingHerb.Indications);
            Assert.Equal(updateDto.Dosage, existingHerb.Dosage);
            Assert.Equal(updateDto.Contraindications, existingHerb.Contraindications);
            Assert.Equal(updateDto.Manufacturer, existingHerb.Manufacturer);
            Assert.Equal(updateDto.BatchNumber, existingHerb.BatchNumber);
            Assert.Equal(updateDto.ExpiryDate, existingHerb.ExpiryDate);
            Assert.Equal(CommonStatus.Disabled, existingHerb.Status);
            Assert.Equal(updateDto.Remark, existingHerb.Remark);
        }

        /// <summary>
        /// 测试HerbModel到HerbDetailDto的映射
        /// </summary>
        [Fact]
        public void MapHerbModelToDetailDto_ShouldMapAllFields()
        {
            // Arrange
            var herbModel = new HerbModel
            {
                Id = Guid.NewGuid(),
                Name = "人参",
                Code = "RS003",
                PinYinCode = "RS",
                Category = "补气药",
                Unit = "g",
                Price = 80.00m,
                Stock = 50,
                MinStock = 5,
                Nature = "微温",
                Taste = "甘、微苦",
                Meridian = "脾、肺、心",
                Functions = "大补元气，复脉固脱",
                Indications = "体虚欲脱，肢冷脉微",
                Dosage = "3-9g",
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now.AddMonths(-6),
                UpdateTime = DateTime.Now.AddDays(-3)
            };

            // Act
            var detailDto = _mapper.Map<HerbDetailDto>(herbModel);

            // Assert - 验证详情DTO包含完整信息
            Assert.Equal(herbModel.Id, detailDto.Id);
            Assert.Equal(herbModel.Name, detailDto.Name);
            Assert.Equal(herbModel.Code, detailDto.Code);
            Assert.Equal(herbModel.PinYinCode, detailDto.PinYinCode);
            Assert.Equal(herbModel.Category, detailDto.Category);
            Assert.Equal(herbModel.Price, detailDto.Price);
            Assert.Equal(herbModel.Nature, detailDto.Nature);
            Assert.Equal(herbModel.Taste, detailDto.Taste);
            Assert.Equal(herbModel.Functions, detailDto.Functions);
            Assert.Equal(herbModel.Status.ToString(), detailDto.Status);
        }

        /// <summary>
        /// 测试价格字段的精确映射
        /// 药材价格是关键业务数据，需要确保精度不丢失
        /// </summary>
        [Theory]
        [InlineData(0.01)]
        [InlineData(999.99)]
        [InlineData(123.456)]
        [InlineData(0)]
        public void HerbPriceMapping_ShouldMaintainPrecision(decimal testPrice)
        {
            // Arrange
            var herbModel = new HerbModel
            {
                Id = Guid.NewGuid(),
                Name = "价格测试药材",
                Price = testPrice
            };

            // Act
            var herbDto = _mapper.Map<HerbDto>(herbModel);
            var roundTripModel = _mapper.Map<HerbModel>(herbDto);

            // Assert - 验证价格精度保持
            Assert.Equal(testPrice, herbDto.Price);
            Assert.Equal(testPrice, roundTripModel.Price);
        }

        /// <summary>
        /// 测试库存数量的正确映射
        /// </summary>
        [Fact]
        public void HerbStockMapping_ShouldHandleVariousValues()
        {
            // Arrange
            var testCases = new[]
            {
                new { Stock = 0, MinStock = 0 },
                new { Stock = 1000, MinStock = 100 },
                new { Stock = int.MaxValue, MinStock = int.MaxValue }
            };

            foreach (var testCase in testCases)
            {
                var herbModel = new HerbModel
                {
                    Id = Guid.NewGuid(),
                    Name = $"库存测试{testCase.Stock}",
                    Stock = testCase.Stock,
                    MinStock = testCase.MinStock
                };

                // Act
                var herbDto = _mapper.Map<HerbDto>(herbModel);

                // Assert
                Assert.Equal(testCase.Stock, herbDto.Stock);
                Assert.Equal(testCase.MinStock, herbDto.MinStock);
            }
        }

        /// <summary>
        /// 测试药材属性的中文处理
        /// 确保中医药专业术语正确映射
        /// </summary>
        [Fact]
        public void HerbChineseMedicine_ShouldMapCorrectly()
        {
            // Arrange
            var herbModel = new HerbModel
            {
                Id = Guid.NewGuid(),
                Name = "川芎",
                Nature = "温",
                Taste = "辛",
                Meridian = "肝、胆、心包",
                Functions = "活血行气，祛风止痛",
                Indications = "月经不调，经闭痛经，癥瘕腹痛，胸胁刺痛，跌扑肿痛，头痛，风湿痹痛",
                Contraindications = "阴虚火旺，上盛下虚及气弱之人忌服"
            };

            // Act
            var herbDto = _mapper.Map<HerbDto>(herbModel);
            var roundTripModel = _mapper.Map<HerbModel>(herbDto);

            // Assert - 验证中文内容完整性
            Assert.Equal("川芎", herbDto.Name);
            Assert.Equal("温", herbDto.Nature);
            Assert.Equal("辛", herbDto.Taste);
            Assert.Equal("肝、胆、心包", herbDto.Meridian);
            Assert.Equal("活血行气，祛风止痛", herbDto.Functions);
            Assert.Contains("月经不调", herbDto.Indications);
            Assert.Contains("阴虚火旺", herbDto.Contraindications);
            
            // 往返映射验证
            Assert.Equal("川芎", roundTripModel.Name);
            Assert.Equal("温", roundTripModel.Nature);
        }
    }
}