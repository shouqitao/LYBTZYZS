using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Models.Herbs;
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
    /// HerbService异常处理和错误场景测试 - UltraThink设计
    /// 专注于异常处理、错误恢复、边界条件等场景
    /// </summary>
    public class HerbServiceExceptionTests : IDisposable
    {
        private readonly HerbService _herbService;
        private readonly Mock<IHerbRepository> _mockRepository;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;
        private readonly MockFactory _mockFactory;
        private readonly HerbTestDataBuilder _dataBuilder;

        public HerbServiceExceptionTests()
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

        #region Repository Exception Tests

        [Fact]
        public async Task GetByIdAsync_WhenRepositoryThrows_PropagatesException()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(herbId))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _herbService.GetByIdAsync(herbId));
        }

        [Fact]
        public async Task GetListAsync_WhenRepositoryThrows_PropagatesException()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetAllAsync())
                .ThrowsAsync(new TimeoutException("Query timeout"));

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(
                async () => await _herbService.GetListAsync());
        }

        [Fact]
        public async Task AddAsync_WhenRepositoryReturnsNull_ReturnsNull()
        {
            // Arrange
            var createDto = new HerbCreateDto { Name = "测试", Price = 10 };
            _mockRepository.Setup(x => x.AddAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel?)null);

            // Act
            var result = await _herbService.AddAsync(createDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_WhenRepositoryUpdateFails_ReturnsFalse()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            var updateDto = new HerbUpdateDto { Id = herb.Id, Name = "更新" };

            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel?)null);

            // Act
            var result = await _herbService.UpdateAsync(updateDto);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Database Context Exception Tests

        [Fact]
        public async Task GetPagedAsync_WhenDatabaseUnavailable_ThrowsException()
        {
            // Arrange
            // 关闭数据库连接模拟错误
            await _context.DisposeAsync();
            
            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
            };

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await _herbService.GetPagedAsync(query));
        }

        [Fact]
        public async Task SearchAsync_WithClosedContext_ThrowsException()
        {
            // Arrange
            await _context.DisposeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await _herbService.SearchAsync("test"));
        }

        #endregion

        #region Input Validation Tests

        [Fact]
        public async Task SearchAsync_WithNullKeyword_ReturnsEmptyList()
        {
            // Act
            var result = await _herbService.SearchAsync(null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchAsync_WithWhitespaceOnly_ReturnsEmptyList()
        {
            // Act
            var result = await _herbService.SearchAsync("   \t\n   ");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ImportAsync_WithEmptyList_ReturnsZero()
        {
            // Arrange
            var emptyList = new List<HerbImportDto>();
            _mockRepository.Setup(x => x.AddRangeAsync(It.IsAny<List<HerbModel>>()))
                .ReturnsAsync(true);

            // Act
            var result = await _herbService.ImportAsync(emptyList);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task BatchUpdatePriceAsync_WithEmptyList_ReturnsZero()
        {
            // Act
            var result = await _herbService.BatchUpdatePriceAsync(new List<HerbPriceUpdateDto>());

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region Pinyin Generation Tests

        [Fact]
        public async Task AddAsync_WithEmptyName_GeneratesEmptyPinyin()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "",
                Price = 10
            };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            await _herbService.AddAsync(createDto);

            // Assert
            _mockRepository.Verify(x => x.AddAsync(It.Is<HerbModel>(h => 
                h.PinYinCode == "")), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithProvidedPinyin_UsesProvidedValue()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            var updateDto = new HerbUpdateDto
            {
                Id = herb.Id,
                Name = "测试药材",
                PinYinCode = "CSYC", // 提供的拼音码
                Price = 100,
                Status = CommonStatus.Enabled
            };

            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            await _herbService.UpdateAsync(updateDto);

            // Assert
            _mockRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.PinYinCode == "CSYC")), Times.Once);
        }

        #endregion

        #region Concurrent Access Tests

        [Fact]
        public async Task MultipleReads_DoNotInterfere()
        {
            // Arrange
            var herbs = _dataBuilder.BuildMany(5);
            _mockRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(herbs);

            // Act
            var tasks = new List<Task<List<HerbDto>>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_herbService.GetListAsync());
            }
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, r => Assert.Equal(5, r.Count));
            _mockRepository.Verify(x => x.GetAllAsync(), Times.Exactly(10));
        }

        [Fact]
        public async Task ConcurrentUpdates_HandleCorrectly()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            var updateDto1 = new HerbUpdateDto { Id = herb.Id, Name = "更新1", Price = 100, Status = CommonStatus.Enabled };
            var updateDto2 = new HerbUpdateDto { Id = herb.Id, Name = "更新2", Price = 200, Status = CommonStatus.Enabled };

            // Act
            var task1 = _herbService.UpdateAsync(updateDto1);
            var task2 = _herbService.UpdateAsync(updateDto2);
            var results = await Task.WhenAll(task1, task2);

            // Assert
            Assert.All(results, r => Assert.True(r));
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<HerbModel>()), Times.Exactly(2));
        }

        #endregion

        #region Status Management Tests

        [Fact]
        public async Task SetStatusAsync_WhenRepositoryUpdateReturnsNull_ReturnsFalse()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel?)null);

            // Act
            var result = await _herbService.SetStatusAsync(herb.Id, true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_WhenUpdateFails_ReturnsFalse()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel?)null);

            // Act
            var result = await _herbService.DeleteAsync(herb.Id);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Sorting and Ordering Tests

        [Fact]
        public async Task GetAvailableHerbsAsync_AlwaysOrdersByName()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "Z药材", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "A药材", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "M药材", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.GetAvailableHerbsAsync();

            // Assert
            Assert.Equal("A药材", result[0].Name);
            Assert.Equal("M药材", result[1].Name);
            Assert.Equal("Z药材", result[2].Name);
        }

        [Fact]
        public async Task GetByPriceRangeAsync_AlwaysOrdersByPrice()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "高价", Price = 100, Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "低价", Price = 10, Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "中价", Price = 50, Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.GetByPriceRangeAsync(0, 200);

            // Assert
            Assert.Equal("低价", result[0].Name);
            Assert.Equal("中价", result[1].Name);
            Assert.Equal("高价", result[2].Name);
        }

        #endregion

        #region Null Reference Tests

        [Fact]
        public async Task UpdatePriceAsync_WithNullPriceValues_SkipsNullUpdates()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            herb.Price = 100;
            herb.CostPrice = 50;

            var priceUpdate = new HerbPriceUpdateDto
            {
                Id = herb.Id,
                CostPrice = null,
                Price = null
            };

            _mockRepository.Setup(x => x.GetByIdAsync(herb.Id))
                .ReturnsAsync(herb);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel h) => h);

            // Act
            var result = await _herbService.UpdatePriceAsync(priceUpdate);

            // Assert
            Assert.True(result);
            // 原值应该保持不变
            Assert.Equal(100, herb.Price);
            Assert.Equal(50, herb.CostPrice);
        }

        [Fact]
        public async Task GetPagedAsync_WithNullPinYinCode_HandlesCorrectly()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "无拼音", PinYinCode = null, Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "有拼音", PinYinCode = "YPY", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            var query = new HerbPagedQueryDto
            {
                PinYinCode = "Y",
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            Assert.Single(result.Items);
            Assert.Equal("有拼音", result.Items[0].Name);
        }

        #endregion

        #region Special Scenarios Tests

        [Fact]
        public async Task ImportAsync_WithDuplicateNames_ImportsAll()
        {
            // Arrange
            var importDtos = new List<HerbImportDto>
            {
                new HerbImportDto { Name = "重复药材", Price = 10 },
                new HerbImportDto { Name = "重复药材", Price = 20 },
                new HerbImportDto { Name = "重复药材", Price = 30 }
            };

            _mockRepository.Setup(x => x.AddRangeAsync(It.IsAny<List<HerbModel>>()))
                .ReturnsAsync(true);

            // Act
            var result = await _herbService.ImportAsync(importDtos);

            // Assert
            Assert.Equal(3, result);
            _mockRepository.Verify(x => x.AddRangeAsync(It.Is<List<HerbModel>>(list => 
                list.Count == 3 && list.All(h => h.Name == "重复药材"))), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_WithPageBeyondTotal_ReturnsEmptyItems()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "药材1", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "药材2", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            var query = new HerbPagedQueryDto
            {
                CurrentPage = 100, // 远超实际页数
                PageSize = 10
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task SearchAsync_WithSpecialRegexCharacters_HandlesCorrectly()
        {
            // Arrange
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "药材[特殊]", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "药材(括号)", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var result1 = await _herbService.SearchAsync("[");
            var result2 = await _herbService.SearchAsync("(");

            // Assert
            Assert.Single(result1);
            Assert.Single(result2);
        }

        [Fact]
        public async Task UpdateAsync_WithMaxDecimalPrice_HandlesCorrectly()
        {
            // Arrange
            var herb = _dataBuilder.AsValidHerb().Build();
            var updateDto = new HerbUpdateDto
            {
                Id = herb.Id,
                Name = "最大价格",
                Price = decimal.MaxValue,
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
                h.Price == decimal.MaxValue)), Times.Once);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _mockFactory?.ClearCache();
        }
    }
}