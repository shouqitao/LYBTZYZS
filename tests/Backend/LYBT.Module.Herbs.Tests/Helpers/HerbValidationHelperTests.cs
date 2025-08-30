using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Helpers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Herbs.Tests.Helpers
{
    /// <summary>
    /// HerbValidationHelper单元测试
    /// 测试重构后的验证助手类，确保使用BaseValidationHelper基类方法的正确性
    /// </summary>
    public class HerbValidationHelperTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<HerbValidationHelper> _logger;
        private readonly HerbValidationHelper _validationHelper;

        public HerbValidationHelperTests()
        {
            // 配置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 配置AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                // 简化映射配置用于测试
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            _logger = NullLogger<HerbValidationHelper>.Instance;

            _validationHelper = new HerbValidationHelper(_context, _mapper, _logger);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region ValidateCreateAsync Tests

        [Fact]
        public async Task ValidateCreateAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var dto = new HerbCreateDto
            {
                Name = "有效药材名",
                Price = 12.5m,
                Unit = "g"
            };

            // Act
            var result = await _validationHelper.ValidateCreateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidateCreateAsync_WithInvalidName_ReturnsFailure(string invalidName)
        {
            // Arrange
            var dto = new HerbCreateDto
            {
                Name = invalidName,
                Price = 12.5m,
                Unit = "g"
            };

            // Act
            var result = await _validationHelper.ValidateCreateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("药材名称");
            result.ErrorMessage.Should().Contain("不能为空");
        }

        [Fact]
        public async Task ValidateCreateAsync_WithNegativePrice_ReturnsFailure()
        {
            // Arrange
            var dto = new HerbCreateDto
            {
                Name = "有效药材名",
                Price = -5.0m,
                Unit = "g"
            };

            // Act
            var result = await _validationHelper.ValidateCreateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("药材价格");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidateCreateAsync_WithInvalidUnit_ReturnsFailure(string invalidUnit)
        {
            // Arrange
            var dto = new HerbCreateDto
            {
                Name = "有效药材名",
                Price = 12.5m,
                Unit = invalidUnit
            };

            // Act
            var result = await _validationHelper.ValidateCreateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("药材单位");
            result.ErrorMessage.Should().Contain("不能为空");
        }

        [Fact]
        public async Task ValidateCreateAsync_WithDuplicateName_ReturnsFailure()
        {
            // Arrange
            var existingHerb = new Herb
            {
                Id = Guid.NewGuid(),
                Name = "重复药材",
                Price = 10m,
                Unit = "g",
                Status = CommonStatus.Enabled
            };
            _context.Herbs.Add(existingHerb);
            await _context.SaveChangesAsync();

            var dto = new HerbCreateDto
            {
                Name = "重复药材",
                Price = 15m,
                Unit = "g"
            };

            // Act
            var result = await _validationHelper.ValidateCreateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("已存在");
        }

        [Fact]
        public async Task ValidateCreateAsync_WithZeroPrice_ReturnsSuccess()
        {
            // Arrange - 零价格是允许的
            var dto = new HerbCreateDto
            {
                Name = "免费药材",
                Price = 0m,
                Unit = "g"
            };

            // Act
            var result = await _validationHelper.ValidateCreateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region ValidateUpdateAsync Tests

        [Fact]
        public async Task ValidateUpdateAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var existingHerb = new Herb
            {
                Id = herbId,
                Name = "原始药材名",
                Price = 10m,
                Unit = "g",
                Status = CommonStatus.Enabled
            };
            _context.Herbs.Add(existingHerb);
            await _context.SaveChangesAsync();

            var dto = new HerbUpdateDto
            {
                Name = "更新后药材名",
                Price = 15m,
                Unit = "ml"
            };

            // Act
            var result = await _validationHelper.ValidateUpdateAsync(herbId, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateUpdateAsync_WithNonExistentId_ReturnsFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var dto = new HerbUpdateDto
            {
                Name = "药材名",
                Price = 15m,
                Unit = "g"
            };

            // Act
            var result = await _validationHelper.ValidateUpdateAsync(nonExistentId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("不存在");
        }

        [Fact]
        public async Task ValidateUpdateAsync_WithDuplicateName_ReturnsFailure()
        {
            // Arrange
            var herbId1 = Guid.NewGuid();
            var herbId2 = Guid.NewGuid();
            
            var herb1 = new Herb
            {
                Id = herbId1,
                Name = "药材1",
                Price = 10m,
                Unit = "g",
                Status = CommonStatus.Enabled
            };
            var herb2 = new Herb
            {
                Id = herbId2,
                Name = "药材2",
                Price = 15m,
                Unit = "g",
                Status = CommonStatus.Enabled
            };
            
            _context.Herbs.AddRange(herb1, herb2);
            await _context.SaveChangesAsync();

            var dto = new HerbUpdateDto
            {
                Name = "药材2", // 试图使用已存在的名称
                Price = 20m,
                Unit = "ml"
            };

            // Act
            var result = await _validationHelper.ValidateUpdateAsync(herbId1, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("已被其他药材使用");
        }

        #endregion

        #region ValidateDeleteAsync Tests

        [Fact]
        public async Task ValidateDeleteAsync_WithExistingHerb_ReturnsSuccess()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var herb = new Herb
            {
                Id = herbId,
                Name = "待删除药材",
                Price = 10m,
                Unit = "g",
                Status = CommonStatus.Enabled
            };
            _context.Herbs.Add(herb);
            await _context.SaveChangesAsync();

            // Act
            var result = await _validationHelper.ValidateDeleteAsync(herbId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateDeleteAsync_WithNonExistentHerb_ReturnsFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _validationHelper.ValidateDeleteAsync(nonExistentId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("不存在");
        }

        #endregion

        #region ValidateImportAsync Tests

        [Fact]
        public async Task ValidateImportAsync_WithValidHerbs_ReturnsEmptyErrors()
        {
            // Arrange
            var herbs = new List<HerbImportDto>
            {
                new() { Name = "药材1", Price = 10m, Unit = "g" },
                new() { Name = "药材2", Price = 15m, Unit = "ml" }
            };

            // Act
            var result = await _validationHelper.ValidateImportAsync(herbs);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateImportAsync_WithEmptyList_ReturnsErrors()
        {
            // Arrange
            var herbs = new List<HerbImportDto>();

            // Act
            var result = await _validationHelper.ValidateImportAsync(herbs);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("导入的药材列表为空");
        }

        [Fact]
        public async Task ValidateImportAsync_WithNullList_ReturnsErrors()
        {
            // Arrange
            List<HerbImportDto> herbs = null;

            // Act
            var result = await _validationHelper.ValidateImportAsync(herbs);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain("导入的药材列表为空");
        }

        [Fact]
        public async Task ValidateImportAsync_WithInvalidData_ReturnsErrors()
        {
            // Arrange
            var herbs = new List<HerbImportDto>
            {
                new() { Name = "", Price = 10m, Unit = "g" },        // 空名称
                new() { Name = "药材2", Price = -5m, Unit = "ml" },   // 负价格
                new() { Name = "药材3", Price = 20m, Unit = "" }      // 空单位
            };

            // Act
            var result = await _validationHelper.ValidateImportAsync(herbs);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCountGreaterOrEqualTo(3);
            result.Data.Should().Contain(error => error.Contains("第1行") && error.Contains("药材名称不能为空"));
            result.Data.Should().Contain(error => error.Contains("第2行") && error.Contains("药材价格不能为负数"));
            result.Data.Should().Contain(error => error.Contains("第3行") && error.Contains("药材单位不能为空"));
        }

        [Fact]
        public async Task ValidateImportAsync_WithDuplicateNames_ReturnsErrors()
        {
            // Arrange
            var herbs = new List<HerbImportDto>
            {
                new() { Name = "重复药材", Price = 10m, Unit = "g" },
                new() { Name = "重复药材", Price = 15m, Unit = "ml" }
            };

            // Act
            var result = await _validationHelper.ValidateImportAsync(herbs);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Contain(error => error.Contains("第2行") && error.Contains("在导入列表中重复"));
        }

        #endregion

        #region ValidatePriceUpdateAsync Tests

        [Fact]
        public async Task ValidatePriceUpdateAsync_WithValidPrices_ReturnsSuccess()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var herb = new Herb
            {
                Id = herbId,
                Name = "药材",
                Price = 10m,
                Unit = "g"
            };
            _context.Herbs.Add(herb);
            await _context.SaveChangesAsync();

            var dto = new HerbPriceUpdateDto
            {
                CostPrice = 8m,
                Price = 12m
            };

            // Act
            var result = await _validationHelper.ValidatePriceUpdateAsync(herbId, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidatePriceUpdateAsync_WithCostPriceHigherThanPrice_ReturnsFailure()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var herb = new Herb
            {
                Id = herbId,
                Name = "药材",
                Price = 10m,
                Unit = "g"
            };
            _context.Herbs.Add(herb);
            await _context.SaveChangesAsync();

            var dto = new HerbPriceUpdateDto
            {
                CostPrice = 15m,
                Price = 10m
            };

            // Act
            var result = await _validationHelper.ValidatePriceUpdateAsync(herbId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("成本价不能高于销售价");
        }

        [Fact]
        public async Task ValidatePriceUpdateAsync_WithNegativeCostPrice_ReturnsFailure()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var herb = new Herb
            {
                Id = herbId,
                Name = "药材",
                Price = 10m,
                Unit = "g"
            };
            _context.Herbs.Add(herb);
            await _context.SaveChangesAsync();

            var dto = new HerbPriceUpdateDto
            {
                CostPrice = -5m,
                Price = 12m
            };

            // Act
            var result = await _validationHelper.ValidatePriceUpdateAsync(herbId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("成本价");
        }

        #endregion

        #region ValidateBatchStatusUpdateAsync Tests

        [Fact]
        public async Task ValidateBatchStatusUpdateAsync_WithValidIds_ReturnsSuccess()
        {
            // Arrange
            var herbId1 = Guid.NewGuid();
            var herbId2 = Guid.NewGuid();
            
            var herbs = new List<Herb>
            {
                new() { Id = herbId1, Name = "药材1", Price = 10m, Unit = "g" },
                new() { Id = herbId2, Name = "药材2", Price = 15m, Unit = "ml" }
            };
            _context.Herbs.AddRange(herbs);
            await _context.SaveChangesAsync();

            var dto = new BatchStatusUpdateDto
            {
                Ids = new List<Guid> { herbId1, herbId2 },
                Status = CommonStatus.Enabled
            };

            // Act
            var result = await _validationHelper.ValidateBatchStatusUpdateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateBatchStatusUpdateAsync_WithEmptyIds_ReturnsFailure()
        {
            // Arrange
            var dto = new BatchStatusUpdateDto
            {
                Ids = new List<Guid>(),
                Status = CommonStatus.Enabled
            };

            // Act
            var result = await _validationHelper.ValidateBatchStatusUpdateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("药材ID列表为空");
        }

        [Fact]
        public async Task ValidateBatchStatusUpdateAsync_WithInvalidIds_ReturnsFailure()
        {
            // Arrange
            var validId = Guid.NewGuid();
            var invalidId = Guid.NewGuid();
            
            var herb = new Herb
            {
                Id = validId,
                Name = "药材",
                Price = 10m,
                Unit = "g"
            };
            _context.Herbs.Add(herb);
            await _context.SaveChangesAsync();

            var dto = new BatchStatusUpdateDto
            {
                Ids = new List<Guid> { validId, invalidId },
                Status = CommonStatus.Enabled
            };

            // Act
            var result = await _validationHelper.ValidateBatchStatusUpdateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("不存在");
            result.ErrorMessage.Should().Contain(invalidId.ToString());
        }

        #endregion

        #region ValidatePagedQuery Tests

        [Fact]
        public void ValidatePagedQuery_WithValidParameters_ReturnsSuccess()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 20,
                MinPrice = 10m,
                MaxPrice = 100m
            };

            // Act
            var result = _validationHelper.ValidatePagedQuery(query);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidatePagedQuery_WithInvalidPageIndex_ReturnsFailure(int pageIndex)
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                PageIndex = pageIndex,
                PageSize = 20
            };

            // Act
            var result = _validationHelper.ValidatePagedQuery(query);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("页码必须大于0");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidatePagedQuery_WithInvalidPageSize_ReturnsFailure(int pageSize)
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                PageIndex = 1,
                PageSize = pageSize
            };

            // Act
            var result = _validationHelper.ValidatePagedQuery(query);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("页大小必须大于0");
        }

        [Fact]
        public void ValidatePagedQuery_WithPageSizeTooLarge_ReturnsFailure()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 150
            };

            // Act
            var result = _validationHelper.ValidatePagedQuery(query);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("页大小不能超过100");
        }

        [Fact]
        public void ValidatePagedQuery_WithMinPriceGreaterThanMaxPrice_ReturnsFailure()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 20,
                MinPrice = 100m,
                MaxPrice = 50m
            };

            // Act
            var result = _validationHelper.ValidatePagedQuery(query);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("最低价格不能高于最高价格");
        }

        #endregion

        #region Utility Methods Tests

        [Theory]
        [InlineData("有效关键词", true)]
        [InlineData("a", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void IsValidSearchKeyword_ReturnsExpectedResult(string keyword, bool expected)
        {
            // Act
            var result = _validationHelper.IsValidSearchKeyword(keyword);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("药材名称", "Y")]
        [InlineData("黄芩", "H")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void GenerateSimplePinyinCode_ReturnsExpectedResult(string name, string expected)
        {
            // Act
            var result = _validationHelper.GenerateSimplePinyinCode(name);

            // Assert
            result.Should().Be(expected);
        }

        #endregion
    }
}