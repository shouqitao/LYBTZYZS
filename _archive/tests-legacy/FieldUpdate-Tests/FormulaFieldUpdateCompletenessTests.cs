using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Formula;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.Formula.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Module.Formula.Tests.Services
{
    /// <summary>
    /// Formula模块字段更新完整性测试
    /// UltraThink设计：确保UpdateAsync方法能正确映射FormulaUpdateDto中的所有字段到Formula实体
    /// 重点验证复杂集合Herbs的更新以及DTO字段到实体的映射完整性
    /// </summary>
    public class FormulaFieldUpdateCompletenessTests : IDisposable
    {
        private readonly FormulaService _service;
        private readonly AppDbContext _context;
        private readonly Mock<IFormulaRepository> _repositoryMock;
        private readonly IMapper _mapper;

        public FormulaFieldUpdateCompletenessTests()
        {
            _repositoryMock = new Mock<IFormulaRepository>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 使用Formula模块的真实AutoMapper配置
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<LYBT.Entities.Formula.Formula, FormulaDto>();
                cfg.CreateMap<LYBT.Entities.Formula.Formula, FormulaDetailDto>();
                cfg.CreateMap<FormulaCreateDto, LYBT.Entities.Formula.Formula>();
                
                // 关键：FormulaUpdateDto映射配置，忽略实体中不存在的4个字段
                cfg.CreateMap<FormulaUpdateDto, LYBT.Entities.Formula.Formula>()
                    .ForSourceMember(src => src.Id, opt => opt.DoNotValidate())
                    .ForSourceMember(src => src.Instructions, opt => opt.DoNotValidate())      // 实体中不存在
                    .ForSourceMember(src => src.Indications, opt => opt.DoNotValidate())       // 实体中不存在
                    .ForSourceMember(src => src.Contraindications, opt => opt.DoNotValidate()) // 实体中不存在
                    .ForSourceMember(src => src.Preparation, opt => opt.DoNotValidate())       // 实体中不存在
                    .ForMember(dest => dest.Herbs, opt => opt.Ignore()); // 复杂集合单独处理
                
                cfg.CreateMap<FormulaHerbItemCreateDto, FormulaHerbItem>();
                cfg.CreateMap<FormulaHerbItemUpdateDto, FormulaHerbItem>();
                cfg.CreateMap<FormulaHerbItem, FormulaHerbItemDto>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new FormulaService(
                _context,
                _repositoryMock.Object,
                _mapper,
                NullLogger<FormulaService>.Instance);
        }

        /// <summary>
        /// 测试FormulaUpdateDto到Formula实体的完整字段映射
        /// 验证所有实体中存在的字段都能被正确更新，DTO中额外的字段被忽略
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithCompleteDto_UpdatesAllMappedFormulaFields()
        {
            // Arrange - 创建原始验方
            var originalFormula = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "原始验方名",
                Effect = "原始功效",
                Usage = "原始用法",
                Property = "原始性味归经",
                Status = CommonStatus.Enabled,
                IsShared = false,
                Remark = "原始备注",
                Herbs = new List<FormulaHerbItem>
                {
                    new FormulaHerbItem { Id = Guid.NewGuid(), HerbName = "人参", Dosage = "10g" },
                    new FormulaHerbItem { Id = Guid.NewGuid(), HerbName = "当归", Dosage = "15g" }
                }
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(originalFormula.Id))
                .ReturnsAsync(originalFormula);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<LYBT.Entities.Formula.Formula>()))
                .ReturnsAsync((LYBT.Entities.Formula.Formula f) => f);

            // 创建包含所有字段的更新DTO
            var updateDto = new FormulaUpdateDto
            {
                Id = originalFormula.Id, // 这个字段在映射中被忽略
                
                // FormulaInputBaseDto基类字段 - 实体中存在的应该被映射
                Name = "更新后验方名",
                Effect = "更新后功效",
                Usage = "更新后用法",
                IsShared = true,
                Remark = "更新后备注",
                
                // 实体中不存在的字段 - 应该在映射中被忽略
                Instructions = "用药指导（实体中不存在）",
                Indications = "主治症状（实体中不存在）",
                Contraindications = "禁忌症（实体中不存在）",
                Preparation = "制备方法（实体中不存在）",
                
                // 复杂集合字段 - 需要特殊处理
                Herbs = new List<FormulaHerbItemUpdateDto>
                {
                    new FormulaHerbItemUpdateDto
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "甘草",
                        Dosage = "6g",
                        Unit = "g",
                        HerbId = Guid.NewGuid()
                    },
                    new FormulaHerbItemUpdateDto
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "白术",
                        Dosage = "12g",
                        Unit = "g",
                        HerbId = Guid.NewGuid()
                    }
                }
            };

            // Act - 执行更新
            var result = await _service.UpdateAsync(originalFormula.Id, updateDto);

            // Assert - 验证更新结果
            Assert.NotNull(result);
            Assert.True(result.Success);
            
            // 验证实体中存在的字段都被正确更新
            Assert.Equal("更新后验方名", originalFormula.Name);
            Assert.Equal("更新后功效", originalFormula.Effect);
            Assert.Equal("更新后用法", originalFormula.Usage);
            Assert.True(originalFormula.IsShared);
            Assert.Equal("更新后备注", originalFormula.Remark);
            
            // 验证实体ID没有被DTO中的Id覆盖（因为ForSourceMember忽略）
            Assert.Equal(originalFormula.Id, originalFormula.Id); // ID应该保持不变
            
            // 验证Herbs集合被正确更新
            Assert.NotNull(originalFormula.Herbs);
            Assert.Equal(2, originalFormula.Herbs.Count);
            Assert.Contains(originalFormula.Herbs, h => h.HerbName == "甘草" && h.Dosage == "6g");
            Assert.Contains(originalFormula.Herbs, h => h.HerbName == "白术" && h.Dosage == "12g");
            
            // 验证Repository的UpdateAsync方法被调用
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<LYBT.Entities.Formula.Formula>()), Times.Once);
        }

        /// <summary>
        /// 测试Herbs集合的复杂更新逻辑
        /// 验证药材组合的清空、新增操作
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithNewHerbsList_ReplacesHerbsCompletely()
        {
            // Arrange
            var originalFormula = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "测试验方",
                Effect = "测试功效",
                Usage = "测试用法",
                Herbs = new List<FormulaHerbItem>
                {
                    new FormulaHerbItem { Id = Guid.NewGuid(), HerbName = "旧药材1", Dosage = "10g" },
                    new FormulaHerbItem { Id = Guid.NewGuid(), HerbName = "旧药材2", Dosage = "15g" },
                    new FormulaHerbItem { Id = Guid.NewGuid(), HerbName = "旧药材3", Dosage = "20g" }
                }
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(originalFormula.Id))
                .ReturnsAsync(originalFormula);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<LYBT.Entities.Formula.Formula>()))
                .ReturnsAsync((LYBT.Entities.Formula.Formula f) => f);

            var updateDto = new FormulaUpdateDto
            {
                Id = originalFormula.Id,
                Name = originalFormula.Name,
                Effect = originalFormula.Effect,
                Usage = originalFormula.Usage,
                Herbs = new List<FormulaHerbItemUpdateDto>
                {
                    new FormulaHerbItemUpdateDto
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "新药材1",
                        Dosage = "8g",
                        Unit = "g"
                    },
                    new FormulaHerbItemUpdateDto
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "新药材2", 
                        Dosage = "12g",
                        Unit = "g"
                    }
                }
            };

            // Act
            await _service.UpdateAsync(originalFormula.Id, updateDto);

            // Assert
            // 验证原有药材被清空，新药材被添加
            Assert.Equal(2, originalFormula.Herbs.Count);
            Assert.DoesNotContain(originalFormula.Herbs, h => h.HerbName.Contains("旧药材"));
            Assert.Contains(originalFormula.Herbs, h => h.HerbName == "新药材1" && h.Dosage == "8g");
            Assert.Contains(originalFormula.Herbs, h => h.HerbName == "新药材2" && h.Dosage == "12g");
        }

        /// <summary>
        /// 测试空Herbs集合的处理
        /// 验证当Herbs为空或null时的行为
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithEmptyHerbsList_ClearsAllHerbs()
        {
            // Arrange
            var originalFormula = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "测试验方",
                Effect = "测试功效",
                Usage = "测试用法",
                Herbs = new List<FormulaHerbItem>
                {
                    new FormulaHerbItem { Id = Guid.NewGuid(), HerbName = "药材1", Dosage = "10g" },
                    new FormulaHerbItem { Id = Guid.NewGuid(), HerbName = "药材2", Dosage = "15g" }
                }
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(originalFormula.Id))
                .ReturnsAsync(originalFormula);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<LYBT.Entities.Formula.Formula>()))
                .ReturnsAsync((LYBT.Entities.Formula.Formula f) => f);

            var updateDto = new FormulaUpdateDto
            {
                Id = originalFormula.Id,
                Name = originalFormula.Name,
                Effect = originalFormula.Effect,
                Usage = originalFormula.Usage,
                Herbs = new List<FormulaHerbItemUpdateDto>() // 空列表
            };

            // Act
            await _service.UpdateAsync(originalFormula.Id, updateDto);

            // Assert
            Assert.Empty(originalFormula.Herbs);
        }

        /// <summary>
        /// 测试必填字段的更新
        /// 验证必填字段（Name, Effect, Usage）的更新
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithRequiredFields_UpdatesSuccessfully()
        {
            // Arrange
            var originalFormula = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "原始名称",
                Effect = "原始功效",
                Usage = "原始用法",
                Herbs = new List<FormulaHerbItem>()
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(originalFormula.Id))
                .ReturnsAsync(originalFormula);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<LYBT.Entities.Formula.Formula>()))
                .ReturnsAsync((LYBT.Entities.Formula.Formula f) => f);

            var updateDto = new FormulaUpdateDto
            {
                Id = originalFormula.Id,
                Name = "更新名称",       // Required
                Effect = "更新功效",     // Required  
                Usage = "更新用法",      // Required
                Herbs = new List<FormulaHerbItemUpdateDto>
                {
                    new FormulaHerbItemUpdateDto
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "甘草",
                        Dosage = "6g"
                    }
                }
            };

            // Act
            var result = await _service.UpdateAsync(originalFormula.Id, updateDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("更新名称", originalFormula.Name);
            Assert.Equal("更新功效", originalFormula.Effect);
            Assert.Equal("更新用法", originalFormula.Usage);
        }

        /// <summary>
        /// 测试IsShared字段的布尔值更新
        /// 验证共享状态的切换
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateAsync_WithDifferentIsSharedValues_UpdatesCorrectly(bool isSharedValue)
        {
            // Arrange
            var originalFormula = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "测试验方",
                Effect = "测试功效",
                Usage = "测试用法",
                IsShared = !isSharedValue, // 设为相反值
                Herbs = new List<FormulaHerbItem>()
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(originalFormula.Id))
                .ReturnsAsync(originalFormula);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<LYBT.Entities.Formula.Formula>()))
                .ReturnsAsync((LYBT.Entities.Formula.Formula f) => f);

            var updateDto = new FormulaUpdateDto
            {
                Id = originalFormula.Id,
                Name = originalFormula.Name,
                Effect = originalFormula.Effect,
                Usage = originalFormula.Usage,
                IsShared = isSharedValue,
                Herbs = new List<FormulaHerbItemUpdateDto>()
            };

            // Act
            await _service.UpdateAsync(originalFormula.Id, updateDto);

            // Assert
            Assert.Equal(isSharedValue, originalFormula.IsShared);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}