using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Module.Herbs.Tests.Services
{
    /// <summary>
    /// Herb模块字段更新完整性测试
    /// UltraThink设计：确保UpdateAsync方法能正确映射HerbUpdateDto中的所有字段到HerbModel实体
    /// 防止手工字段映射导致的字段遗漏问题
    /// </summary>
    public class HerbFieldUpdateCompletenessTests : IDisposable
    {
        private readonly HerbService _service;
        private readonly AppDbContext _context;
        private readonly Mock<IHerbRepository> _repositoryMock;
        private readonly IMapper _mapper;
        private readonly HerbTestDataBuilder _builder;

        public HerbFieldUpdateCompletenessTests()
        {
            _builder = new HerbTestDataBuilder();
            _repositoryMock = new Mock<IHerbRepository>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 使用Herb模块的真实AutoMapper配置
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<HerbModel, HerbDto>();
                cfg.CreateMap<HerbModel, HerbDetailDto>();
                cfg.CreateMap<HerbCreateDto, HerbModel>();
                cfg.CreateMap<HerbUpdateDto, HerbModel>();
                cfg.CreateMap<HerbEditDto, HerbUpdateDto>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new HerbService(
                _context,
                _repositoryMock.Object,
                _mapper,
                NullLogger<HerbService>.Instance);
        }

        /// <summary>
        /// 测试HerbUpdateDto到HerbModel的完整字段映射
        /// 验证AutoMapper能正确映射所有字段，防止字段更新不完整问题
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithCompleteDto_UpdatesAllHerbFields()
        {
            // Arrange - 创建原始药材
            var originalHerb = _builder.AsValidHerb().Build();
            originalHerb.Name = "原始药材名";
            originalHerb.PinYinCode = "YSCM";
            originalHerb.WuBiCode = "YSCM";
            originalHerb.Origin = "原始产地";
            originalHerb.Spec = "原始规格";
            originalHerb.Unit = "原始单位";
            originalHerb.Price = 10.00m;
            originalHerb.Effect = "原始功效";
            originalHerb.Usage = "原始用法";
            originalHerb.Status = CommonStatus.Enabled;
            originalHerb.Remark = "原始备注";

            _repositoryMock.Setup(r => r.GetByIdAsync(originalHerb.Id))
                .ReturnsAsync(originalHerb);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel m) => m);

            // 创建包含所有字段的更新DTO
            var updateDto = new HerbUpdateDto
            {
                // HerbUpdateDto直接字段
                Name = "更新后药材名",
                PinYinCode = "GHYSCM", // 注意：这个字段可能会被业务逻辑重新生成
                WuBiCode = "GHYSCM",
                Origin = "更新后产地",
                Spec = "更新后规格",
                Unit = "更新后单位",
                Price = 25.50m,
                Effect = "更新后功效",
                Usage = "更新后用法",
                
                // 继承自UpdateDtoBase的字段
                Remark = "更新后备注",
                
                // 继承自StatusDto的字段
                Status = CommonStatus.Disabled,
                
                // 继承自TimestampDto的字段（这些通常由系统管理，不应该从DTO更新）
                CreateTime = DateTime.Now.AddDays(-10), // 这个应该被忽略
                UpdateTime = DateTime.Now               // 这个应该被忽略
            };

            // Act - 执行更新
            var result = await _service.UpdateAsync(originalHerb.Id, updateDto);

            // Assert - 验证更新结果
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            
            // 验证所有业务字段都被正确更新
            Assert.Equal("更新后药材名", originalHerb.Name);
            // PinYinCode可能被业务逻辑重新生成，所以不直接比较DTO值
            Assert.NotNull(originalHerb.PinYinCode);
            Assert.Equal("GHYSCM", originalHerb.WuBiCode);
            Assert.Equal("更新后产地", originalHerb.Origin);
            Assert.Equal("更新后规格", originalHerb.Spec);
            Assert.Equal("更新后单位", originalHerb.Unit);
            Assert.Equal(25.50m, originalHerb.Price);
            Assert.Equal("更新后功效", originalHerb.Effect);
            Assert.Equal("更新后用法", originalHerb.Usage);
            Assert.Equal("更新后备注", originalHerb.Remark);
            Assert.Equal(CommonStatus.Disabled, originalHerb.Status);
            
            // 验证时间戳字段不应该被DTO更新（由系统管理）
            Assert.NotEqual(updateDto.CreateTime, originalHerb.CreateTime); // 创建时间不应该被更新
            
            // 验证Repository的UpdateAsync方法被调用
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<HerbModel>()), Times.Once);
        }

        /// <summary>
        /// 测试PinYinCode自动生成功能
        /// 验证当Name字段更新时，PinYinCode能被正确重新生成
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithNewName_RegeneratesPinYinCode()
        {
            // Arrange
            var originalHerb = _builder.AsValidHerb().Build();
            originalHerb.Name = "人参";
            originalHerb.PinYinCode = "RS";

            _repositoryMock.Setup(r => r.GetByIdAsync(originalHerb.Id))
                .ReturnsAsync(originalHerb);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel m) => m);

            var updateDto = new HerbUpdateDto
            {
                Name = "当归",
                Unit = originalHerb.Unit,
                Price = originalHerb.Price,
                Status = originalHerb.Status
            };

            // Act
            var result = await _service.UpdateAsync(originalHerb.Id, updateDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("当归", originalHerb.Name);
            // PinYinCode应该被重新生成（具体值取决于拼音生成逻辑）
            Assert.NotEqual("RS", originalHerb.PinYinCode);
            Assert.NotNull(originalHerb.PinYinCode);
            Assert.NotEmpty(originalHerb.PinYinCode);
        }

        /// <summary>
        /// 测试必填字段验证
        /// 确保必填字段（Name, Unit, Price）的更新工作正常
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithRequiredFields_UpdatesSuccessfully()
        {
            // Arrange
            var originalHerb = _builder.AsValidHerb().Build();

            _repositoryMock.Setup(r => r.GetByIdAsync(originalHerb.Id))
                .ReturnsAsync(originalHerb);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel m) => m);

            var updateDto = new HerbUpdateDto
            {
                Name = "新药材名称",    // Required
                Unit = "新单位",       // Required  
                Price = 99.99m,       // Required
                Status = CommonStatus.Enabled
            };

            // Act
            var result = await _service.UpdateAsync(originalHerb.Id, updateDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("新药材名称", originalHerb.Name);
            Assert.Equal("新单位", originalHerb.Unit);
            Assert.Equal(99.99m, originalHerb.Price);
        }

        /// <summary>
        /// 测试可选字段的null值处理
        /// 验证可选字段设置为null时的行为
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithNullOptionalFields_HandlesNullsCorrectly()
        {
            // Arrange
            var originalHerb = _builder.AsValidHerb().Build();
            originalHerb.Origin = "原产地";
            originalHerb.Effect = "原功效";
            originalHerb.Usage = "原用法";
            originalHerb.Remark = "原备注";

            _repositoryMock.Setup(r => r.GetByIdAsync(originalHerb.Id))
                .ReturnsAsync(originalHerb);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel m) => m);

            var updateDto = new HerbUpdateDto
            {
                Name = originalHerb.Name,
                Unit = originalHerb.Unit,
                Price = originalHerb.Price,
                Status = originalHerb.Status,
                // 所有可选字段设为null
                Origin = null,
                Effect = null,
                Usage = null,
                Remark = null,
                PinYinCode = null,
                WuBiCode = null,
                Spec = null
            };

            // Act
            var result = await _service.UpdateAsync(originalHerb.Id, updateDto);

            // Assert
            Assert.True(result.Success);
            // AutoMapper应该将null值映射到实体，覆盖原有值
            Assert.Null(originalHerb.Origin);
            Assert.Null(originalHerb.Effect);
            Assert.Null(originalHerb.Usage);
            Assert.Null(originalHerb.Remark);
            Assert.Null(originalHerb.WuBiCode);
            Assert.Null(originalHerb.Spec);
            // PinYinCode可能被重新生成，因为Name没有变化且为非空
        }

        /// <summary>
        /// 测试状态枚举更新
        /// 验证CommonStatus枚举的不同值都能正确更新
        /// </summary>
        [Theory]
        [InlineData(CommonStatus.Enabled)]
        [InlineData(CommonStatus.Disabled)]
        public async Task UpdateAsync_WithDifferentStatusValues_UpdatesStatusCorrectly(
            CommonStatus newStatus)
        {
            // Arrange
            var originalHerb = _builder.AsValidHerb().Build();
            originalHerb.Status = CommonStatus.Enabled;

            _repositoryMock.Setup(r => r.GetByIdAsync(originalHerb.Id))
                .ReturnsAsync(originalHerb);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<HerbModel>()))
                .ReturnsAsync((HerbModel m) => m);

            var updateDto = new HerbUpdateDto
            {
                Name = originalHerb.Name,
                Unit = originalHerb.Unit,
                Price = originalHerb.Price,
                Status = newStatus
            };

            // Act
            var result = await _service.UpdateAsync(originalHerb.Id, updateDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(newStatus, originalHerb.Status);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}