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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Module.Herbs.Tests.Services
{
    /// <summary>
    /// HerbService单元测试 - UltraThink设计
    /// 职责单一：专注于HerbService的单元测试
    /// 代码干净：清晰的测试结构，描述性的方法名
    /// 性能出色：使用内存数据库，快速执行
    /// </summary>
    public class HerbServiceTests : IDisposable
    {
        private readonly HerbService _herbService;
        private readonly Mock<IHerbRepository> _mockRepository;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;
        private readonly MockFactory _mockFactory;
        private readonly HerbTestDataBuilder _dataBuilder;

        public HerbServiceTests()
        {
            _mockFactory = new MockFactory();
            _dataBuilder = new HerbTestDataBuilder();
            _mockRepository = new Mock<IHerbRepository>();

            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 配置AutoMapper
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

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WhenHerbExists_ReturnsHerbDetailDto()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);

            // Act
            var result = await _herbService.GetByIdAsync(herb.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(herb.Name, result.Name);
            Assert.Equal(herb.Price, result.Price);
            _mockRepository.Verify(x => x.GetByIdAsync(herb.Id), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenHerbNotExists_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(nonExistentId))
                .ReturnsAsync((HerbModel?)null);

            // Act
            var result = await _herbService.GetByIdAsync(nonExistentId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(x => x.GetByIdAsync(nonExistentId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithEmptyGuid_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetByIdAsync(Guid.Empty))
                .ReturnsAsync((HerbModel?)null);

            // Act
            var result = await _herbService.GetByIdAsync(Guid.Empty);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetListAsync Tests

        [Fact]
        public async Task GetListAsync_ReturnsAllHerbs()
        {
            // Arrange
            var herbs = _dataBuilder.BuildMany(5);
            _mockRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(herbs);

            // Act
            var result = await _herbService.GetListAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);
            _mockRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetListAsync_WhenNoHerbs_ReturnsEmptyList()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<HerbModel>());

            // Act
            var result = await _herbService.GetListAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithValidQuery_ReturnsPaginatedResult()
        {
            // Arrange
            var herbs = CreateHerbsInContext(10);
            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 5
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(1, result.CurrentPage);
        }

        [Fact]
        public async Task GetPagedAsync_WithNameFilter_ReturnsFilteredResults()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "麻黄", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "桂枝", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "黄芩", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            var query = new HerbPagedQueryDto
            {
                Name = "黄",
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, h => Assert.Contains("黄", h.Name));
        }

        [Fact]
        public async Task GetPagedAsync_WithPriceRange_ReturnsFilteredResults()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "低价药", Price = 10, Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "中价药", Price = 50, Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "高价药", Price = 100, Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            var query = new HerbPagedQueryDto
            {
                MinPrice = 20,
                MaxPrice = 80,
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            Assert.Single(result.Items);
            Assert.Equal("中价药", result.Items[0].Name);
        }

        [Fact]
        public async Task GetPagedAsync_WithStatusFilter_ReturnsOnlyEnabledByDefault()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "启用药材", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "禁用药材", Status = CommonStatus.Disabled }
            });
            await _context.SaveChangesAsync();

            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            Assert.Single(result.Items);
            Assert.Equal("启用药材", result.Items[0].Name);
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_WithValidData_CreatesNewHerb()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "新增药材",
                Origin = "云南",
                Spec = "优质",
                Unit = "g",
                Price = 88.88m,
                Effect = "清热解毒",
                Usage = "水煎服"
            };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.AddAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Name, result.Name);
            Assert.Equal(createDto.Price, result.Price);
            _mockRepository.Verify(x => x.AddAsync(It.IsAny<HerbModel>()), Times.Once);
        }

        [Fact]
        public async Task AddAsync_AutoGeneratesPinyinCode_WhenNotProvided()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "测试药材",
                Price = 50
            };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.AddAsync(createDto);

            // Assert
            Assert.NotNull(result);
            _mockRepository.Verify(x => x.AddAsync(It.Is<HerbModel>(h => 
                h.PinYinCode != null && h.PinYinCode.Length > 0)), Times.Once);
        }

        [Fact]
        public async Task AddAsync_SetsDefaultStatus_AsEnabled()
        {
            // Arrange
            var createDto = new HerbCreateDto { Name = "测试", Price = 10 };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            await _herbService.AddAsync(createDto);

            // Assert
            _mockRepository.Verify(x => x.AddAsync(It.Is<HerbModel>(h => 
                h.Status == CommonStatus.Enabled)), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WhenHerbExists_UpdatesSuccessfully()
        {
            // Arrange
            var existingHerb = _dataBuilder.AsValidHerb().Build();
            var updateDto = new HerbUpdateDto
            {
                Id = existingHerb.Id,
                Name = "更新后的名称",
                Price = 999.99m,
                Status = CommonStatus.Enabled
            };

            _mockRepository.Setup(x => x.GetByIdAsync(existingHerb.Id))
                .ReturnsAsync(existingHerb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.UpdateAsync(updateDto);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(x => x.GetByIdAsync(existingHerb.Id), Times.Once);
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<HerbModel>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenHerbNotExists_ReturnsFalse()
        {
            // Arrange
            var updateDto = new HerbUpdateDto
            {
                Id = Guid.NewGuid(),
                Name = "不存在的药材"
            };

            _mockRepository.Setup(x => x.GetByIdAsync(updateDto.Id))
                .ReturnsAsync((HerbModel?)null);

            // Act
            var result = await _herbService.UpdateAsync(updateDto);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<HerbModel>()), Times.Never);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WhenHerbExists_SoftDeletesSuccessfully()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.DeleteAsync(herb.Id);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.Status == CommonStatus.Disabled)), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenHerbNotExists_ReturnsFalse()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(nonExistentId))
                .ReturnsAsync((HerbModel?)null);

            // Act
            var result = await _herbService.DeleteAsync(nonExistentId);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<HerbModel>()), Times.Never);
        }

        #endregion

        #region SearchAsync Tests

        [Fact]
        public async Task SearchAsync_WithValidKeyword_ReturnsMatchingHerbs()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "麻黄", PinYinCode = "MH", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "桂枝", PinYinCode = "GZ", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "黄芩", PinYinCode = "HQ", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.SearchAsync("黄");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, h => Assert.Contains("黄", h.Name));
        }

        [Fact]
        public async Task SearchAsync_WithPinyinCode_ReturnsMatchingHerbs()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "麻黄", PinYinCode = "MH", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "木香", PinYinCode = "MX", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.SearchAsync("MH");

            // Assert
            Assert.Single(result);
            Assert.Equal("麻黄", result[0].Name);
        }

        [Fact]
        public async Task SearchAsync_WithEmptyKeyword_ReturnsEmptyList()
        {
            // Act
            var result = await _herbService.SearchAsync("");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchAsync_OnlyReturnsEnabledHerbs()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "启用药材", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "禁用药材", Status = CommonStatus.Disabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.SearchAsync("药材");

            // Assert
            Assert.Single(result);
            Assert.Equal("启用药材", result[0].Name);
        }

        [Fact]
        public async Task SearchAsync_LimitsResultsTo20()
        {
            // Arrange
            var herbs = new List<HerbModel>();
            for (int i = 0; i < 30; i++)
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

            // Act
            var result = await _herbService.SearchAsync("药材");

            // Assert
            Assert.Equal(20, result.Count);
        }

        #endregion

        #region GetAvailableHerbsAsync Tests

        [Fact]
        public async Task GetAvailableHerbsAsync_ReturnsOnlyEnabledHerbs()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "启用1", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "禁用1", Status = CommonStatus.Disabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "启用2", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.GetAvailableHerbsAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, h => Assert.Contains("启用", h.Name));
        }

        [Fact]
        public async Task GetAvailableHerbsAsync_ReturnsOrderedByName()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "C药材", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "A药材", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "B药材", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.GetAvailableHerbsAsync();

            // Assert
            Assert.Equal("A药材", result[0].Name);
            Assert.Equal("B药材", result[1].Name);
            Assert.Equal("C药材", result[2].Name);
        }

        #endregion

        #region SetStatusAsync Tests

        [Fact]
        public async Task SetStatusAsync_EnableHerb_SetsStatusToEnabled()
        {
            // Arrange
            var herb = _dataBuilder.AsDiscontinuedHerb().Build();
            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.SetStatusAsync(herb.Id, true);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.Status == CommonStatus.Enabled)), Times.Once);
        }

        [Fact]
        public async Task SetStatusAsync_DisableHerb_SetsStatusToDisabled()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.SetStatusAsync(herb.Id, false);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.Status == CommonStatus.Disabled)), Times.Once);
        }

        [Fact]
        public async Task SetStatusAsync_WhenHerbNotExists_ReturnsFalse()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(nonExistentId))
                .ReturnsAsync((HerbModel?)null);

            // Act
            var result = await _herbService.SetStatusAsync(nonExistentId, true);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region ImportAsync Tests

        [Fact]
        public async Task ImportAsync_WithValidData_ImportsAllHerbs()
        {
            // Arrange
            var importDtos = new List<HerbImportDto>
            {
                new HerbImportDto { Name = "导入药材1", Price = 10 },
                new HerbImportDto { Name = "导入药材2", Price = 20 },
                new HerbImportDto { Name = "导入药材3", Price = 30 }
            };

            _mockRepository.Setup(x => x.AddRangeAsync(It.IsAny<List<HerbModel>>()))
                .ReturnsAsync(true);

            // Act
            var result = await _herbService.ImportAsync(importDtos);

            // Assert
            Assert.Equal(3, result);
            _mockRepository.Verify(x => x.AddRangeAsync(It.Is<List<HerbModel>>(list => 
                list.Count == 3)), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_GeneratesPinyinForAllImports()
        {
            // Arrange
            var importDtos = new List<HerbImportDto>
            {
                new HerbImportDto { Name = "测试", Price = 10 }
            };

            _mockRepository.Setup(x => x.AddRangeAsync(It.IsAny<List<HerbModel>>()))
                .ReturnsAsync(true);

            // Act
            await _herbService.ImportAsync(importDtos);

            // Assert
            _mockRepository.Verify(x => x.AddRangeAsync(It.Is<List<HerbModel>>(list => 
                list.All(h => !string.IsNullOrEmpty(h.PinYinCode)))), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_SetsAllImportsAsEnabled()
        {
            // Arrange
            var importDtos = new List<HerbImportDto>
            {
                new HerbImportDto { Name = "药材1", Price = 10 },
                new HerbImportDto { Name = "药材2", Price = 20 }
            };

            _mockRepository.Setup(x => x.AddRangeAsync(It.IsAny<List<HerbModel>>()))
                .ReturnsAsync(true);

            // Act
            await _herbService.ImportAsync(importDtos);

            // Assert
            _mockRepository.Verify(x => x.AddRangeAsync(It.Is<List<HerbModel>>(list => 
                list.All(h => h.Status == CommonStatus.Enabled))), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenAddRangeFails_ReturnsZero()
        {
            // Arrange
            var importDtos = new List<HerbImportDto>
            {
                new HerbImportDto { Name = "药材", Price = 10 }
            };

            _mockRepository.Setup(x => x.AddRangeAsync(It.IsAny<List<HerbModel>>()))
                .ReturnsAsync(false);

            // Act
            var result = await _herbService.ImportAsync(importDtos);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region ExportAsync Tests

        [Fact]
        public async Task ExportAsync_ReturnsAllHerbDetails()
        {
            // Arrange
            var herbs = _dataBuilder.BuildMany(5);
            _mockRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(herbs);

            // Act
            var result = await _herbService.ExportAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);
            _mockRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task ExportAsync_WhenNoHerbs_ReturnsEmptyList()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<HerbModel>());

            // Act
            var result = await _herbService.ExportAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region Price Management Tests

        [Fact]
        public async Task UpdatePriceAsync_UpdatesBothCostAndSalePrice()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            var priceUpdate = new HerbPriceUpdateDto
            {
                Id = herb.Id,
                CostPrice = 50,
                Price = 100
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
                h.CostPrice == 50 && h.Price == 100)), Times.Once);
        }

        [Fact]
        public async Task BatchUpdatePriceAsync_UpdatesMultipleHerbs()
        {
            // Arrange
            var herbs = _dataBuilder.BuildMany(3);
            var updates = herbs.Select(h => new HerbPriceUpdateDto
            {
                Id = h.Id,
                Price = 99.99m
            }).ToList();

            foreach (var herb in herbs)
            {
                _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                    .ReturnsAsync(herb);
            }
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.BatchUpdatePriceAsync(updates);

            // Assert
            Assert.Equal(3, result);
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<HerbModel>()), Times.Exactly(3));
        }

        [Fact]
        public async Task GetByPriceRangeAsync_ReturnsHerbsInPriceRange()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "便宜药", Price = 10, Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "中价药", Price = 50, Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "贵价药", Price = 100, Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.GetByPriceRangeAsync(30, 70);

            // Assert
            Assert.Single(result);
            Assert.Equal("中价药", result[0].Name);
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