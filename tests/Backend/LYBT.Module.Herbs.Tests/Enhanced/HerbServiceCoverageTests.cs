using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Moq;
using Xunit;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Herbs;
// using LYBT.Infrastructure.Mapping;
using LYBT.Module.Herbs.Services;
using LYBT.Module.Herbs.Helpers;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Mapping;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Herbs.Tests.Enhanced
{
    /// <summary>
    /// HerbService 增强测试类 - 专注于代码覆盖率提升
    /// 目标：覆盖重构后的 HerbService 核心业务逻辑
    /// </summary>
    public class HerbServiceCoverageTests : IDisposable
    {
        private readonly HerbService _herbService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly Mock<IHerbRepository> _mockRepository;
        private readonly List<Herb> _testHerbs;

        public HerbServiceCoverageTests()
        {
            // 创建内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 创建AutoMapper
            _mapper = CreateMapper();

            // 创建Mock Repository
            _mockRepository = new Mock<IHerbRepository>();

            // 创建测试数据
            _testHerbs = CreateTestData();
            _context.Herbs.AddRange(_testHerbs);
            _context.SaveChanges();

            // 配置Mock
            SetupMockRepository();

            // 创建Helper类实例
            var queryHelper = new HerbQueryHelper(_context, _mapper, NullLogger<HerbQueryHelper>.Instance);
            var validationHelper = new HerbValidationHelper(_context, NullLogger<HerbValidationHelper>.Instance);
            var businessHelper = new HerbBusinessHelper(_context, _mockRepository.Object, _mapper, NullLogger<HerbBusinessHelper>.Instance);

            // 创建HerbService实例
            _herbService = new HerbService(
                _context,
                _mockRepository.Object,
                _mapper,
                NullLogger<HerbService>.Instance,
                queryHelper,
                validationHelper,
                businessHelper
            );
        }

        #region 测试数据初始化

        private List<Herb> CreateTestData()
        {
            var herbs = new List<Herb>();

            // 创建不同状态的药材
            herbs.Add(new Herb
            {
                Id = Guid.NewGuid(),
                Name = "人参",
                PinYinCode = "RC",
                Origin = "吉林",
                Spec = "特级",
                Unit = "克",
                Price = 50.00m,
                Effect = "大补元气",
                Usage = "煎服",
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now.AddDays(-10)
            });

            herbs.Add(new Herb
            {
                Id = Guid.NewGuid(),
                Name = "黄芪",
                PinYinCode = "HQ",
                Origin = "内蒙古",
                Spec = "一级",
                Unit = "克",
                Price = 25.00m,
                Effect = "补气升阳",
                Usage = "煎服",
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now.AddDays(-8)
            });

            herbs.Add(new Herb
            {
                Id = Guid.NewGuid(),
                Name = "当归",
                PinYinCode = "DG",
                Origin = "甘肃",
                Spec = "二级",
                Unit = "克",
                Price = 35.00m,
                Effect = "补血活血",
                Usage = "煎服",
                Status = CommonStatus.Disabled,
                CreateTime = DateTime.Now.AddDays(-5)
            });

            return herbs;
        }

        private IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<HerbMappingProfile>();
                // cfg.AddProfile<CommonMappingProfile>();
            }, NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        private void SetupMockRepository()
        {
            _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => _testHerbs.FirstOrDefault(h => h.Id == id));

            _mockRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(() => _testHerbs);

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<Herb>()))
                .Returns((Herb herb) =>
                {
                    _testHerbs.Add(herb);
                    return Task.FromResult(herb);
                });

            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Herb>()))
                .Returns((Herb herb) =>
                {
                    var existing = _testHerbs.FirstOrDefault(h => h.Id == herb.Id);
                    if (existing != null)
                    {
                        _testHerbs.Remove(existing);
                        _testHerbs.Add(herb);
                    }
                    return Task.FromResult(herb);
                });
        }

        #endregion

        #region 基础CRUD操作测试

        [Fact]
        public async Task GetByIdAsync_ValidId_ShouldReturnHerb()
        {
            // Arrange
            var herb = _testHerbs.First();

            // Act
            var result = await _herbService.GetByIdAsync(herb.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(herb.Id);
            result.Data.Name.Should().Be(herb.Name);
        }

        [Fact]
        public async Task GetByIdAsync_InvalidId_ShouldReturnFailure()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _herbService.GetByIdAsync(invalidId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllHerbs()
        {
            // Act
            var result = await _herbService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(_testHerbs.Count);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ShouldCreateHerb()
        {
            // Arrange
            var dto = new HerbCreateDto
            {
                Name = "川芎",
                Origin = "四川",
                Spec = "一级",
                Unit = "克",
                Price = 20.00m,
                Effect = "活血行气",
                Usage = "煎服"
            };

            // Act
            var result = await _herbService.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be(dto.Name);
            result.Data.Price.Should().Be(dto.Price);
        }

        [Fact]
        public async Task UpdateAsync_ValidIdAndDto_ShouldUpdateHerb()
        {
            // Arrange
            var herb = _testHerbs.First();
            var dto = new HerbUpdateDto
            {
                Name = "更新后的名称",
                Origin = "更新后的产地",
                Price = 100.00m,
                Status = CommonStatus.Enabled
            };

            // Act
            var result = await _herbService.UpdateAsync(herb.Id, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be(dto.Name);
            result.Data.Origin.Should().Be(dto.Origin);
            result.Data.Price.Should().Be(dto.Price);
        }

        [Fact]
        public async Task DeleteAsync_ValidId_ShouldSoftDeleteHerb()
        {
            // Arrange
            var herb = _testHerbs.First(h => h.Status == CommonStatus.Enabled);

            // Act
            var result = await _herbService.DeleteAsync(herb.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        #endregion

        #region 查询和搜索操作测试

        [Fact]
        public async Task GetPagedAsync_ValidQuery_ShouldReturnPagedResult()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 2
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCountLessOrEqualTo(2);
            result.Data.CurrentPage.Should().Be(1);
        }

        [Fact]
        public async Task SearchAsync_ValidKeyword_ShouldReturnMatchingHerbs()
        {
            // Arrange
            var keyword = "人参";

            // Act
            var result = await _herbService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().Contain(h => h.Name.Contains(keyword));
        }

        [Fact]
        public async Task SearchAsync_EmptyKeyword_ShouldReturnEmptyList()
        {
            // Arrange
            var keyword = "";

            // Act
            var result = await _herbService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAvailableHerbsAsync_ShouldReturnOnlyEnabledHerbs()
        {
            // Act
            var result = await _herbService.GetAvailableHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().OnlyContain(h => h.Status == CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetByIdsAsync_ValidIds_ShouldReturnMatchingHerbs()
        {
            // Arrange
            var ids = _testHerbs.Take(2).Select(h => h.Id).ToList();

            // Act
            var result = await _herbService.GetByIdsAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByPriceRangeAsync_ValidRange_ShouldReturnHerbsInRange()
        {
            // Arrange
            decimal minPrice = 20.00m;
            decimal maxPrice = 40.00m;

            // Act
            var result = await _herbService.GetByPriceRangeAsync(minPrice, maxPrice);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().OnlyContain(h => h.Price >= minPrice && h.Price <= maxPrice);
        }

        [Fact]
        public async Task GetHerbsAsync_ShouldReturnAllHerbs()
        {
            // Act
            var result = await _herbService.GetHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(_testHerbs.Count);
        }

        [Fact]
        public async Task GetListAsync_WithoutQuery_ShouldReturnAllHerbs()
        {
            // Act
            var result = await _herbService.GetListAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(_testHerbs.Count);
        }

        [Fact]
        public async Task GetListAsync_WithQuery_ShouldReturnFilteredHerbs()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                Status = CommonStatus.Enabled
            };

            // Act
            var result = await _herbService.GetListAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task SearchByNameAsync_ValidName_ShouldReturnMatchingHerbs()
        {
            // Arrange
            var name = "黄芪";

            // Act
            var result = await _herbService.SearchByNameAsync(name);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().Contain(h => h.Name.Contains(name));
        }

        #endregion

        #region 业务操作测试

        [Fact]
        public async Task SetStatusAsync_ValidIdAndStatus_ShouldUpdateStatus()
        {
            // Arrange
            var herb = _testHerbs.First();
            var newStatus = !herb.Status.Equals(CommonStatus.Enabled);

            // Act
            var result = await _herbService.SetStatusAsync(herb.Id, newStatus);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ImportHerbsAsync_ValidHerbs_ShouldImportSuccessfully()
        {
            // Arrange
            var importHerbs = new List<HerbImportDto>
            {
                new HerbImportDto
                {
                    Name = "导入药材1",
                    Origin = "云南",
                    Unit = "克",
                    Price = 30.00m
                },
                new HerbImportDto
                {
                    Name = "导入药材2",
                    Origin = "四川",
                    Unit = "克",
                    Price = 40.00m
                }
            };

            // Act
            var result = await _herbService.ImportHerbsAsync(importHerbs);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(2);
        }

        [Fact]
        public async Task ExportHerbsAsync_ShouldReturnAllHerbs()
        {
            // Act
            var result = await _herbService.ExportHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(_testHerbs.Count);
        }

        [Fact]
        public async Task BatchUpdateStatusAsync_ValidDto_ShouldUpdateMultipleHerbs()
        {
            // Arrange
            var dto = new BatchStatusUpdateDto
            {
                Ids = _testHerbs.Take(2).Select(h => h.Id).ToList(),
                Status = CommonStatus.Disabled
            };

            // Act
            var result = await _herbService.BatchUpdateStatusAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Fact]
        public async Task GetStatisticsAsync_ShouldReturnStatistics()
        {
            // Act
            var result = await _herbService.GetStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdatePriceAsync_ValidData_ShouldUpdatePrice()
        {
            // Arrange
            var herb = _testHerbs.First();
            var dto = new HerbPriceUpdateDto
            {
                Price = 150.00m,
                CostPrice = 100.00m
            };

            // Act
            var result = await _herbService.UpdatePriceAsync(herb.Id, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task BatchUpdatePriceAsync_ValidUpdates_ShouldUpdateMultiplePrices()
        {
            // Arrange
            var updates = _testHerbs.Take(2).Select(h => new HerbPriceUpdateDto
            {
                Id = h.Id,
                Price = 200.00m
            }).ToList();

            // Act
            var result = await _herbService.BatchUpdatePriceAsync(updates);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(2);
        }

        #endregion

        #region 兼容性接口测试

        [Fact]
        public async Task UpdateStockAsync_ShouldReturnTrue()
        {
            // Arrange
            var herb = _testHerbs.First();
            var dto = new HerbStockUpdateDto
            {
                Quantity = 100,
                IsIncrease = true
            };

            // Act
            var result = await _herbService.UpdateStockAsync(herb.Id, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Fact]
        public async Task GetOutOfStockHerbsAsync_ShouldReturnEmptyList()
        {
            // Act
            var result = await _herbService.GetOutOfStockHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetExpiringHerbsAsync_ShouldReturnEmptyList()
        {
            // Act
            var result = await _herbService.GetExpiringHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetStockStatisticsAsync_ShouldReturnStatistics()
        {
            // Act
            var result = await _herbService.GetStockStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetStockWarningListAsync_ShouldReturnEmptyList()
        {
            // Act
            var result = await _herbService.GetStockWarningListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateStockAsync_WithQuantity_ShouldReturnTrue()
        {
            // Arrange
            var herb = _testHerbs.First();

            // Act
            var result = await _herbService.UpdateStockAsync(herb.Id, 50m, true);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task BatchUpdateStockAsync_ShouldReturnCount()
        {
            // Arrange
            var updates = new List<HerbStockUpdateDto>
            {
                new HerbStockUpdateDto { Id = _testHerbs.First().Id, Quantity = 100 }
            };

            // Act
            var result = await _herbService.BatchUpdateStockAsync(updates);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task SetStockWarningLevelAsync_ShouldReturnTrue()
        {
            // Arrange
            var herb = _testHerbs.First();

            // Act
            var result = await _herbService.SetStockWarningLevelAsync(herb.Id, 10m, 1000m);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetExpiryWarningListAsync_ShouldReturnEmptyList()
        {
            // Act
            var result = await _herbService.GetExpiryWarningListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SetSpecialPriceAsync_ShouldReturnTrue()
        {
            // Arrange
            var herb = _testHerbs.First();

            // Act
            var result = await _herbService.SetSpecialPriceAsync(herb.Id, 20.00m, DateTime.Now, DateTime.Now.AddDays(7));

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CancelSpecialPriceAsync_ShouldReturnTrue()
        {
            // Arrange
            var herb = _testHerbs.First();

            // Act
            var result = await _herbService.CancelSpecialPriceAsync(herb.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetSpecialPriceHerbsAsync_ShouldReturnEmptyList()
        {
            // Act
            var result = await _herbService.GetSpecialPriceHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPriceHistoryAsync_ShouldReturnEmptyList()
        {
            // Arrange
            var herb = _testHerbs.First();

            // Act
            var result = await _herbService.GetPriceHistoryAsync(herb.Id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region 边界条件和异常处理测试

        [Fact]
        public async Task GetByIdAsync_EmptyGuid_ShouldReturnFailure()
        {
            // Act
            var result = await _herbService.GetByIdAsync(Guid.Empty);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_NullDto_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _herbService.CreateAsync(null!));
        }

        [Fact]
        public async Task SearchAsync_NullKeyword_ShouldReturnEmptyList()
        {
            // Act
            var result = await _herbService.SearchAsync(null!);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_EmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            var emptyIds = new List<Guid>();

            // Act
            var result = await _herbService.GetByIdsAsync(emptyIds);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportHerbsAsync_EmptyList_ShouldReturnZero()
        {
            // Arrange
            var emptyList = new List<HerbImportDto>();

            // Act
            var result = await _herbService.ImportHerbsAsync(emptyList);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}