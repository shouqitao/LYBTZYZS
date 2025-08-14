using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;
using LYBT.Tests.UltraThink.TestInfrastructure.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Module.Herbs.Tests.Services
{
    /// <summary>
    /// HerbService高级测试用例 - UltraThink设计
    /// 包含边缘情况、并发测试、性能测试等高级场景
    /// </summary>
    public class HerbServiceAdvancedTests : IDisposable
    {
        private readonly HerbService _herbService;
        private readonly Mock<IHerbRepository> _mockRepository;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;
        private readonly MockFactory _mockFactory;
        private readonly HerbTestDataBuilder _dataBuilder;

        public HerbServiceAdvancedTests()
        {
            _mockFactory = new MockFactory();
            _dataBuilder = new HerbTestDataBuilder();
            _mockRepository = new Mock<IHerbRepository>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<HerbModel, HerbDto>();
                cfg.CreateMap<HerbModel, HerbDetailDto>();
                cfg.CreateMap<HerbCreateDto, HerbModel>();
                cfg.CreateMap<HerbUpdateDto, HerbModel>();
                cfg.CreateMap<HerbImportDto, HerbModel>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _herbService = new HerbService(_mockRepository.Object, _mapper, _context);
        }

        #region Edge Cases Tests

        [Fact]
        public async Task AddAsync_WithSpecialCharactersInName_HandlesCorrectly()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "特殊字符药材~!@#$%^&*()",
                Price = 50
            };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.AddAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Name, result.Name);
        }

        [Fact]
        public async Task AddAsync_WithVeryLongName_HandlesCorrectly()
        {
            // Arrange
            var longName = new string('药', 500); // 500个字符
            var createDto = new HerbCreateDto
            {
                Name = longName,
                Price = 50
            };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.AddAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(longName, result.Name);
        }

        [Fact]
        public async Task UpdateAsync_WithNullOptionalFields_HandlesCorrectly()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            var updateDto = new HerbUpdateDto
            {
                Id = herb.Id,
                Name = "更新名称",
                Origin = null,
                Spec = null,
                Unit = "g",
                Price = 100,
                Effect = null,
                Usage = null,
                Remark = null,
                Status = CommonStatus.Enabled
            };

            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.Origin == null && h.Spec == null && h.Effect == null)), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_WithPageSizeExceedingTotal_ReturnsAllItems()
        {
            // Arrange
            var herbs = new List<HerbModel>();
            for (int i = 0; i < 5; i++)
            {
                herbs.Add(new HerbModel 
                { 
                    Id = Guid.NewGuid(), 
                    Name = $"药材{i}", 
                    Status = CommonStatus.Enabled 
                });
            }
            await _context.Herbs.AddRangeAsync(herbs);
            await _context.SaveChangesAsync();

            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 100 // 超过实际数量
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
        }

        [Fact]
        public async Task GetPagedAsync_WithNegativePageNumber_HandlesAsFirstPage()
        {
            // Arrange
            await CreateHerbsInContext(10);
            var query = new HerbPagedQueryDto
            {
                CurrentPage = -1, // 负数页码
                PageSize = 5
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            Assert.NotNull(result);
            // 由于Skip计算会得到负数，实际会从第0条开始
            Assert.Equal(5, result.Items.Count);
        }

        #endregion

        #region Complex Query Tests

        [Fact]
        public async Task GetPagedAsync_WithMultipleFilters_AppliesAllCorrectly()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel 
                { 
                    Id = Guid.NewGuid(), 
                    Name = "麻黄", 
                    PinYinCode = "MH",
                    Origin = "河南",
                    Spec = "优质",
                    Price = 50,
                    Status = CommonStatus.Enabled 
                },
                new HerbModel 
                { 
                    Id = Guid.NewGuid(), 
                    Name = "黄芩", 
                    PinYinCode = "HQ",
                    Origin = "山东",
                    Spec = "普通",
                    Price = 30,
                    Status = CommonStatus.Enabled 
                },
                new HerbModel 
                { 
                    Id = Guid.NewGuid(), 
                    Name = "黄连", 
                    PinYinCode = "HL",
                    Origin = "四川",
                    Spec = "优质",
                    Price = 80,
                    Status = CommonStatus.Enabled 
                }
            });
            await _context.SaveChangesAsync();

            var query = new HerbPagedQueryDto
            {
                Name = "黄",
                Spec = "优质",
                MinPrice = 40,
                MaxPrice = 100,
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            Assert.Single(result.Items);
            Assert.Equal("黄连", result.Items[0].Name);
        }

        [Fact]
        public async Task SearchAsync_CaseInsensitive_FindsMatches()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "MaHuang", PinYinCode = "MH", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "mahuang", PinYinCode = "mh", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.SearchAsync("MAHUANG");

            // Assert
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region Concurrent Operation Tests

        [Fact]
        public async Task ImportAsync_WithLargeBatch_HandlesCorrectly()
        {
            // Arrange
            var importDtos = new List<HerbImportDto>();
            for (int i = 0; i < 1000; i++)
            {
                importDtos.Add(new HerbImportDto 
                { 
                    Name = $"批量药材{i:D4}", 
                    Price = 10 + i 
                });
            }

            _mockRepository.Setup(x => x.AddRangeAsync(It.IsAny<List<HerbModel>>()))
                .ReturnsAsync(true);

            // Act
            var result = await _herbService.ImportAsync(importDtos);

            // Assert
            Assert.Equal(1000, result);
            _mockRepository.Verify(x => x.AddRangeAsync(It.Is<List<HerbModel>>(list => 
                list.Count == 1000)), Times.Once);
        }

        [Fact]
        public async Task BatchUpdatePriceAsync_WithPartialFailures_ReturnsSuccessCount()
        {
            // Arrange
            var herb1 = _dataBuilder.AsValidHerb().WithId(Guid.NewGuid()).Build();
            var herb2 = _dataBuilder.AsValidHerb().WithId(Guid.NewGuid()).Build();
            var nonExistentId = Guid.NewGuid();

            var updates = new List<HerbPriceUpdateDto>
            {
                new HerbPriceUpdateDto { Id = herb1.Id, Price = 100 },
                new HerbPriceUpdateDto { Id = nonExistentId, Price = 200 }, // 不存在
                new HerbPriceUpdateDto { Id = herb2.Id, Price = 300 }
            };

            _mockRepository.Setup(x => x.GetByIdAsync(herb1.Id))
                .ReturnsAsync(herb1);
            _mockRepository.Setup(x => x.GetByIdAsync(nonExistentId))
                .ReturnsAsync((HerbModel?)null);
            _mockRepository.Setup(x => x.GetByIdAsync(herb2.Id))
                .ReturnsAsync(herb2);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.BatchUpdatePriceAsync(updates);

            // Assert
            Assert.Equal(2, result); // 只有2个成功
        }

        #endregion

        #region Deprecated Methods Tests

        [Fact]
        public async Task GetStockWarningListAsync_AlwaysReturnsEmptyList()
        {
            // Act
            var result = await _herbService.GetStockWarningListAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetStockStatisticsAsync_ReturnsDefaultStatistics()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "药材1", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "药材2", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "药材3", Status = CommonStatus.Disabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.GetStockStatisticsAsync();

            // Assert
            Assert.Equal(2, result.TotalCount); // 只统计启用的
            Assert.Equal(0, result.OutOfStockCount);
            Assert.Equal(0, result.WarningCount);
            Assert.Equal(2, result.SufficientCount);
        }

        [Fact]
        public async Task UpdateStockAsync_AlwaysReturnsTrue_WhenHerbExists()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.UpdateStockAsync(herb.Id, 100, true);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<HerbModel>()), Times.Once);
        }

        [Fact]
        public async Task BatchUpdateStockAsync_ProcessesAllValidHerbs()
        {
            // Arrange
            var herbs = _dataBuilder.BuildMany(3);
            var updates = herbs.Select(h => new HerbStockUpdateDto
            {
                Id = h.Id,
                Quantity = 100,
                IsIncrease = true
            }).ToList();

            foreach (var herb in herbs)
            {
                _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                    .ReturnsAsync(herb);
            }
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.BatchUpdateStockAsync(updates);

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public async Task SetStockWarningLevelAsync_AlwaysReturnsTrue_WhenHerbExists()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.SetStockWarningLevelAsync(herb.Id, 10, 100);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetExpiryWarningListAsync_AlwaysReturnsEmptyList()
        {
            // Act
            var result = await _herbService.GetExpiryWarningListAsync(30);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SetSpecialPriceAsync_AlwaysReturnsTrue_WhenHerbExists()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.SetSpecialPriceAsync(
                herb.Id, 
                88.88m, 
                DateTime.Now, 
                DateTime.Now.AddDays(7));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CancelSpecialPriceAsync_AlwaysReturnsTrue_WhenHerbExists()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.CancelSpecialPriceAsync(herb.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetSpecialPriceHerbsAsync_AlwaysReturnsEmptyList()
        {
            // Act
            var result = await _herbService.GetSpecialPriceHerbsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPriceHistoryAsync_AlwaysReturnsEmptyList()
        {
            // Act
            var result = await _herbService.GetPriceHistoryAsync(Guid.NewGuid());

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task GetPagedAsync_WithLargeDataset_PerformsEfficiently()
        {
            // Arrange
            var herbs = new List<HerbModel>();
            for (int i = 0; i < 10000; i++)
            {
                herbs.Add(new HerbModel
                {
                    Id = Guid.NewGuid(),
                    Name = $"药材{i:D5}",
                    Price = i % 100,
                    Status = CommonStatus.Enabled
                });
            }
            await _context.Herbs.AddRangeAsync(herbs);
            await _context.SaveChangesAsync();

            var query = new HerbPagedQueryDto
            {
                CurrentPage = 500,
                PageSize = 10
            };

            // Act
            var startTime = DateTime.UtcNow;
            var result = await _herbService.GetPagedAsync(query);
            var duration = DateTime.UtcNow - startTime;

            // Assert
            Assert.Equal(10000, result.TotalCount);
            Assert.Equal(10, result.Items.Count);
            Assert.True(duration.TotalSeconds < 1, "查询应在1秒内完成");
        }

        [Fact]
        public async Task SearchAsync_WithManyResults_LimitsTo20()
        {
            // Arrange
            var herbs = new List<HerbModel>();
            for (int i = 0; i < 100; i++)
            {
                herbs.Add(new HerbModel
                {
                    Id = Guid.NewGuid(),
                    Name = $"测试药材{i}",
                    Status = CommonStatus.Enabled
                });
            }
            await _context.Herbs.AddRangeAsync(herbs);
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.SearchAsync("测试");

            // Assert
            Assert.Equal(20, result.Count); // 限制为20条
        }

        #endregion

        #region Data Validation Tests

        [Fact]
        public async Task AddAsync_WithNegativePrice_StillCreatesHerb()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "负价格药材",
                Price = -100 // 负数价格
            };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.AddAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-100, result.Price); // 服务层不做验证，由控制器层处理
        }

        [Fact]
        public async Task UpdatePriceAsync_WithOnlyCostPrice_UpdatesCostOnly()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            herb.Price = 100;
            herb.CostPrice = 50;

            var priceUpdate = new HerbPriceUpdateDto
            {
                Id = herb.Id,
                CostPrice = 60,
                Price = null // 不更新销售价
            };

            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.UpdatePriceAsync(priceUpdate);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.CostPrice == 60 && h.Price == 100)), Times.Once);
        }

        [Fact]
        public async Task GetByPriceRangeAsync_WithReversedRange_ReturnsEmpty()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "药材1", Price = 50, Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.GetByPriceRangeAsync(100, 10); // 最小值大于最大值

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Unicode and Internationalization Tests

        [Fact]
        public async Task AddAsync_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "中药材名称 💊 한약재 薬草",
                Origin = "中国🇨🇳",
                Price = 50
            };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.AddAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Name, result.Name);
            Assert.Equal(createDto.Origin, result.Origin);
        }

        [Fact]
        public async Task SearchAsync_WithChineseCharacters_FindsCorrectly()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "中药材一", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "中药材二", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "西药材", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.SearchAsync("中药");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, h => Assert.Contains("中药", h.Name));
        }

        #endregion

        #region Helper Methods

        private List<HerbModel> CreateHerbsInContext(int count)
        {
            var herbs = _dataBuilder.BuildMany(count);
            _context.Herbs.AddRange(herbs);
            _context.SaveChanges();
            return herbs;
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _mockFactory?.ClearCache();
        }
    }
}