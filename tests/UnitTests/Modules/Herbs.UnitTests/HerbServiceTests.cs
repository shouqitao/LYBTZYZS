using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Services;
using LYBT.Module.Herbs.Mapping;
using LYBT.Module.Herbs.Tests.Base;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LYBT.Module.Herbs.Tests
{
    /// <summary>
    /// HerbService 单元测试
    /// </summary>
    public class HerbServiceTests : IDisposable
    {
        private readonly HerbService _herbService;
        private readonly Mock<IHerbRepository> _mockHerbRepository;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;
        private readonly List<HerbModel> _testHerbs;

        public HerbServiceTests()
        {
            // 设置测试数据
            _testHerbs = new List<HerbModel>();
            InitializeTestData();

            // 创建 InMemory 数据库上下文
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 添加测试数据到上下文
            _context.Herbs.AddRange(_testHerbs);
            _context.SaveChanges();

            // 创建 Mock Repository
            _mockHerbRepository = new Mock<IHerbRepository>();
            SetupRepositoryMethods();

            // 创建 Mapper
            _mapper = CreateHerbMapper();

            // 创建 HerbService 实例
            _herbService = new HerbService(_mockHerbRepository.Object, _mapper, _context);
        }

        #region 初始化测试数据

        private void InitializeTestData()
        {
            // 创建测试中药材数据
            _testHerbs.AddRange(HerbTestDataGenerator.CreateTestHerbs(8));
            
            // 确保有不同状态的中药材
            _testHerbs[0].Status = CommonStatus.Enabled;
            _testHerbs[1].Status = CommonStatus.Enabled;
            _testHerbs[2].Status = CommonStatus.Disabled;
            _testHerbs[3].Status = CommonStatus.Enabled;
            _testHerbs[4].Status = CommonStatus.Disabled;

            // 设置特定的价格范围用于测试
            _testHerbs[0].Price = 50.00m;
            _testHerbs[1].Price = 100.00m;
            _testHerbs[2].Price = 200.00m;
            _testHerbs[3].Price = 25.00m;

            // 设置特定名称用于搜索测试
            _testHerbs[0].Name = "人参";
            _testHerbs[0].PinYinCode = "RC";
            _testHerbs[1].Name = "黄芪";
            _testHerbs[1].PinYinCode = "HQ";
        }

        private void SetupRepositoryMethods()
        {
            // Setup GetByIdAsync
            _mockHerbRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => _testHerbs.FirstOrDefault(h => h.Id == id));

            // Setup GetListAsync
            _mockHerbRepository
                .Setup(x => x.GetListAsync())
                .ReturnsAsync(() => _testHerbs.ToList());

            // Setup AddAsync
            _mockHerbRepository
                .Setup(x => x.AddAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel herb) =>
                {
                    _testHerbs.Add(herb);
                    return true;
                });

            // Setup UpdateAsync
            _mockHerbRepository
                .Setup(x => x.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel herb) =>
                {
                    var existing = _testHerbs.FirstOrDefault(h => h.Id == herb.Id);
                    if (existing != null)
                    {
                        _testHerbs.Remove(existing);
                        _testHerbs.Add(herb);
                        return true;
                    }
                    return false;
                });

            // Setup DeleteAsync
            _mockHerbRepository
                .Setup(x => x.DeleteAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var herb = _testHerbs.FirstOrDefault(h => h.Id == id);
                    if (herb != null)
                    {
                        _testHerbs.Remove(herb);
                        return true;
                    }
                    return false;
                });

            // Setup AddRangeAsync
            _mockHerbRepository
                .Setup(x => x.AddRangeAsync(It.IsAny<List<HerbModel>>()))
                .ReturnsAsync((List<HerbModel> herbs) =>
                {
                    _testHerbs.AddRange(herbs);
                    return true;
                });

            // Setup GetPagedAsync
            _mockHerbRepository
                .Setup(x => x.GetPagedAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((string? keyword, int page, int pageSize) =>
                {
                    var query = _testHerbs.AsQueryable();
                    
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        query = query.Where(h => h.Name.Contains(keyword) || 
                            (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
                    }
                    
                    var total = query.Count();
                    var items = query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                        
                    return (items, total);
                });
        }

        private IMapper CreateHerbMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new HerbMappingProfile());
                
                // 添加测试需要的额外映射
                cfg.CreateMap<HerbImportDto, HerbModel>()
                    .ForMember(dest => dest.Id, opt => opt.Ignore())
                    .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                    .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore())
                    .ForMember(dest => dest.Usage, opt => opt.Ignore());
            }, NullLoggerFactory.Instance);

            return config.CreateMapper();
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_Should_Return_Herb_When_Exists()
        {
            // Arrange
            var herb = _testHerbs.First();

            // Act
            var result = await _herbService.GetByIdAsync(herb.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(herb.Id);
            result.Name.Should().Be(herb.Name);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_When_Not_Exists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _herbService.GetByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetListAsync 测试

        [Fact]
        public async Task GetListAsync_Should_Return_All_Herbs()
        {
            // Act
            var result = await _herbService.GetListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(_testHerbs.Count);
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Paginated_Herbs()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 5
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(5);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(5);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_Name()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                Name = "人参"
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items.First().Name.Should().Be("人参");
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_PinYinCode()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                PinYinCode = "RC"
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items.First().PinYinCode.Should().Be("RC");
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_Origin()
        {
            // Arrange
            var targetOrigin = _testHerbs.First().Origin;
            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                Origin = targetOrigin
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().OnlyContain(h => h.Origin == targetOrigin);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_Price_Range()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                MinPrice = 40m,
                MaxPrice = 60m
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().OnlyContain(h => h.Price >= 40m && h.Price <= 60m);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_Status()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                Status = CommonStatus.Enabled
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().OnlyContain(h => h.Status == CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Return_Only_Enabled_By_Default()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
                // 不设置Status，应该默认只返回启用的
            };

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().OnlyContain(h => h.Status == CommonStatus.Enabled);
        }

        #endregion

        #region AddAsync 测试

        [Fact]
        public async Task AddAsync_Should_Add_New_Herb_Successfully()
        {
            // Arrange
            var dto = new HerbCreateDto
            {
                Name = "新药材",
                Origin = "云南",
                Spec = "一级",
                Unit = "克",
                Price = 88.88m,
                // CostPrice = 66.66m, // 属性不存在
                Effect = "补气养血",
                Usage = "煎服"
            };

            // Act
            var result = await _herbService.AddAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(dto.Name);
            result.Origin.Should().Be(dto.Origin);
            result.Price.Should().Be(dto.Price);
            result.Status.Should().Be(CommonStatus.Enabled);

            // 验证Repository调用
            _mockHerbRepository.Verify(x => x.AddAsync(It.Is<HerbModel>(h => 
                h.Name == dto.Name && 
                h.Status == CommonStatus.Enabled
            )), Times.Once);
        }

        [Fact]
        public async Task AddAsync_Should_Generate_PinyinCode_When_Not_Provided()
        {
            // Arrange
            var dto = new HerbCreateDto
            {
                Name = "测试药材"
                // 不设置PinYinCode
            };

            // Act
            var result = await _herbService.AddAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result!.PinYinCode.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task AddAsync_Should_Return_Null_When_Repository_Fails()
        {
            // Arrange
            _mockHerbRepository
                .Setup(x => x.AddAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync(false);

            var dto = new HerbCreateDto { Name = "测试药材" };

            // Act
            var result = await _herbService.AddAsync(dto);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region UpdateAsync 测试

        [Fact]
        public async Task UpdateAsync_Should_Update_Herb_Successfully()
        {
            // Arrange
            var existingHerb = _testHerbs.First();
            var dto = new HerbUpdateDto
            {
                Id = existingHerb.Id,
                Name = "更新后的名称",
                Origin = "更新后的产地",
                Price = 999.99m,
                Status = CommonStatus.Enabled
            };

            // Act
            var result = await _herbService.UpdateAsync(dto);

            // Assert
            result.Should().BeTrue();

            // 验证Repository调用
            _mockHerbRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.Id == dto.Id && 
                h.Name == dto.Name &&
                h.Origin == dto.Origin &&
                h.Price == dto.Price
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Return_False_When_Herb_Not_Exists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var dto = new HerbUpdateDto
            {
                Id = nonExistentId,
                Name = "不存在的药材"
            };

            // Act
            var result = await _herbService.UpdateAsync(dto);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_Should_Generate_PinyinCode_When_Not_Provided()
        {
            // Arrange
            var existingHerb = _testHerbs.First();
            var dto = new HerbUpdateDto
            {
                Id = existingHerb.Id,
                Name = "新名称"
                // 不设置PinYinCode
            };

            // Act
            var result = await _herbService.UpdateAsync(dto);

            // Assert
            result.Should().BeTrue();
            
            // 验证PinYinCode被生成
            _mockHerbRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.Id == dto.Id && 
                !string.IsNullOrEmpty(h.PinYinCode)
            )), Times.Once);
        }

        #endregion

        #region DeleteAsync 测试

        [Fact]
        public async Task DeleteAsync_Should_Set_Status_To_Disabled()
        {
            // Arrange
            var herb = _testHerbs.First(h => h.Status == CommonStatus.Enabled);

            // Act
            var result = await _herbService.DeleteAsync(herb.Id);

            // Assert
            result.Should().BeTrue();

            // 验证Repository调用
            _mockHerbRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.Id == herb.Id && 
                h.Status == CommonStatus.Disabled
            )), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Should_Return_False_When_Herb_Not_Exists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _herbService.DeleteAsync(nonExistentId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region SearchAsync 测试

        [Fact]
        public async Task SearchAsync_Should_Return_Herbs_Matching_Name()
        {
            // Arrange
            var keyword = "人参";

            // Act
            var result = await _herbService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain(h => h.Name.Contains(keyword));
            result.Should().OnlyContain(h => h.Status == CommonStatus.Enabled);
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Herbs_Matching_PinyinCode()
        {
            // Arrange
            var keyword = "RC";

            // Act
            var result = await _herbService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain(h => h.PinYinCode != null && h.PinYinCode.Contains(keyword));
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Empty_List_When_Keyword_Is_Empty()
        {
            // Act
            var result = await _herbService.SearchAsync("");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_Should_Limit_Results_To_20()
        {
            // Arrange
            // 添加更多测试数据
            var moreHerbs = HerbTestDataGenerator.CreateTestHerbs(30, CommonStatus.Enabled);
            _context.Herbs.AddRange(moreHerbs);
            await _context.SaveChangesAsync();

            // Act
            var result = await _herbService.SearchAsync("药");

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().BeLessOrEqualTo(20);
        }

        #endregion

        #region GetAvailableHerbsAsync 测试

        [Fact]
        public async Task GetAvailableHerbsAsync_Should_Return_Only_Enabled_Herbs()
        {
            // Act
            var result = await _herbService.GetAvailableHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(h => h.Status == CommonStatus.Enabled);
            result.Count.Should().Be(_testHerbs.Count(h => h.Status == CommonStatus.Enabled));
        }

        #endregion

        #region SetStatusAsync 测试

        [Fact]
        public async Task SetStatusAsync_Should_Enable_Herb_Successfully()
        {
            // Arrange
            var herb = _testHerbs.First(h => h.Status == CommonStatus.Disabled);

            // Act
            var result = await _herbService.SetStatusAsync(herb.Id, true);

            // Assert
            result.Should().BeTrue();

            // 验证Repository调用
            _mockHerbRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.Id == herb.Id && 
                h.Status == CommonStatus.Enabled
            )), Times.Once);
        }

        [Fact]
        public async Task SetStatusAsync_Should_Disable_Herb_Successfully()
        {
            // Arrange
            var herb = _testHerbs.First(h => h.Status == CommonStatus.Enabled);

            // Act
            var result = await _herbService.SetStatusAsync(herb.Id, false);

            // Assert
            result.Should().BeTrue();

            // 验证Repository调用
            _mockHerbRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.Id == herb.Id && 
                h.Status == CommonStatus.Disabled
            )), Times.Once);
        }

        [Fact]
        public async Task SetStatusAsync_Should_Return_False_When_Herb_Not_Exists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _herbService.SetStatusAsync(nonExistentId, true);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region ImportAsync 测试 - Record-Only基线之外，已标记过时

        [Fact]
        [Obsolete("Trimmed under Record-Only baseline - exceeds basic CRUD scope")]
        public async Task ImportAsync_Should_Import_Herbs_Successfully()
        {
            // Arrange
            var importList = new List<HerbImportDto>
            {
                new HerbImportDto
                {
                    Name = "导入药材1",
                    Origin = "云南",
                    Unit = "克",
                    Price = 50.00m
                },
                new HerbImportDto
                {
                    Name = "导入药材2",
                    Origin = "四川", 
                    Unit = "两",
                    Price = 80.00m
                }
            };

            // Act
            var result = await _herbService.ImportAsync(importList);

            // Assert
            result.Should().Be(2);

            // 验证Repository调用
            _mockHerbRepository.Verify(x => x.AddRangeAsync(It.Is<List<HerbModel>>(herbs => 
                herbs.Count == 2 && 
                herbs.All(h => h.Status == CommonStatus.Enabled)
            )), Times.Once);
        }

        [Fact]
        [Obsolete("Trimmed under Record-Only baseline - exceeds basic CRUD scope")]
        public async Task ImportAsync_Should_Generate_PinyinCode_For_Imported_Herbs()
        {
            // Arrange
            var importList = new List<HerbImportDto>
            {
                new HerbImportDto { Name = "测试药材" }
            };

            // Act
            var result = await _herbService.ImportAsync(importList);

            // Assert
            result.Should().Be(1);

            // 验证PinYinCode被生成
            _mockHerbRepository.Verify(x => x.AddRangeAsync(It.Is<List<HerbModel>>(herbs => 
                herbs.All(h => !string.IsNullOrEmpty(h.PinYinCode))
            )), Times.Once);
        }

        [Fact]
        [Obsolete("Trimmed under Record-Only baseline - exceeds basic CRUD scope")]
        public async Task ImportAsync_Should_Return_Zero_When_Repository_Fails()
        {
            // Arrange
            _mockHerbRepository
                .Setup(x => x.AddRangeAsync(It.IsAny<List<HerbModel>>()))
                .ReturnsAsync(false);

            var importList = new List<HerbImportDto>
            {
                new HerbImportDto { Name = "测试药材" }
            };

            // Act
            var result = await _herbService.ImportAsync(importList);

            // Assert
            result.Should().Be(0);
        }

        #endregion

        #region ExportAsync 测试 - Record-Only基线之外，已标记过时

        [Fact]
        [Obsolete("Trimmed under Record-Only baseline - exceeds basic CRUD scope")]
        public async Task ExportAsync_Should_Export_All_Herbs()
        {
            // Act
            var result = await _herbService.ExportAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(_testHerbs.Count);
            
            // 验证Repository调用
            _mockHerbRepository.Verify(x => x.GetListAsync(), Times.Once);
        }

        #endregion

        #region 价格管理功能测试 - Record-Only基线之外，已标记过时

        [Fact]
        [Obsolete("Trimmed under Record-Only baseline - exceeds basic CRUD scope")]
        public async Task UpdatePriceAsync_Should_Update_Price_Successfully()
        {
            // Arrange
            var herb = _testHerbs.First();
            var dto = new HerbPriceUpdateDto
            {
                Id = herb.Id,
                Price = 123.45m,
                CostPrice = 90.00m
            };

            // Act
            var result = await _herbService.UpdatePriceAsync(dto);

            // Assert
            result.Should().BeTrue();

            // 验证Repository调用
            _mockHerbRepository.Verify(x => x.UpdateAsync(It.Is<HerbModel>(h => 
                h.Id == dto.Id && 
                h.Price == dto.Price && 
                h.CostPrice == dto.CostPrice
            )), Times.Once);
        }

        [Fact]
        public async Task UpdatePriceAsync_Should_Return_False_When_Herb_Not_Exists()
        {
            // Arrange
            var dto = new HerbPriceUpdateDto
            {
                Id = Guid.NewGuid(),
                Price = 100.00m
            };

            // Act
            var result = await _herbService.UpdatePriceAsync(dto);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task BatchUpdatePriceAsync_Should_Update_Multiple_Prices()
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
            result.Should().Be(2);

            // 验证Repository调用次数
            _mockHerbRepository.Verify(x => x.UpdateAsync(It.IsAny<HerbModel>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetByPriceRangeAsync_Should_Return_Herbs_Within_Range()
        {
            // Arrange
            decimal minPrice = 40m;
            decimal maxPrice = 60m;

            // Act
            var result = await _herbService.GetByPriceRangeAsync(minPrice, maxPrice);

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(h => h.Price >= minPrice && h.Price <= maxPrice);
            result.Should().OnlyContain(h => h.Status == CommonStatus.Enabled);
        }

        #endregion

        #region 库存管理功能测试 - Record-Only基线之外，已标记过时（已禁用功能的兼容性测试）

        [Fact]
        public async Task GetStockWarningListAsync_Should_Return_Empty_List()
        {
            // Act
            var result = await _herbService.GetStockWarningListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetStockStatisticsAsync_Should_Return_Default_Statistics()
        {
            // Act
            var result = await _herbService.GetStockStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(_testHerbs.Count(h => h.Status == CommonStatus.Enabled));
            result.OutOfStockCount.Should().Be(0);
            result.WarningCount.Should().Be(0);
            result.SufficientCount.Should().Be(_testHerbs.Count(h => h.Status == CommonStatus.Enabled));
        }

        [Fact]
        public async Task UpdateStockAsync_Should_Return_True_For_Existing_Herb()
        {
            // Arrange
            var herb = _testHerbs.First();

            // Act
            var result = await _herbService.UpdateStockAsync(herb.Id, 100m, true);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetExpiryWarningListAsync_Should_Return_Empty_List()
        {
            // Act
            var result = await _herbService.GetExpiryWarningListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region 特价功能测试（已禁用功能的兼容性测试）

        [Fact]
        public async Task SetSpecialPriceAsync_Should_Return_True_For_Existing_Herb()
        {
            // Arrange
            var herb = _testHerbs.First();

            // Act
            var result = await _herbService.SetSpecialPriceAsync(herb.Id, 88.88m, DateTime.Now, DateTime.Now.AddDays(30));

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CancelSpecialPriceAsync_Should_Return_True_For_Existing_Herb()
        {
            // Arrange
            var herb = _testHerbs.First();

            // Act
            var result = await _herbService.CancelSpecialPriceAsync(herb.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetSpecialPriceHerbsAsync_Should_Return_Empty_List()
        {
            // Act
            var result = await _herbService.GetSpecialPriceHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPriceHistoryAsync_Should_Return_Empty_List()
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

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}