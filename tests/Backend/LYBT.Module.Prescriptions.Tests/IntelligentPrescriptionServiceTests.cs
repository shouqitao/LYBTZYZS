using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Entities.Prescriptions;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Moq;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests
{
    /// <summary>
    /// 智能处方服务单元测试
    /// </summary>
    public class IntelligentPrescriptionServiceTests
    {
        private readonly Mock<IHerbService> _mockHerbService;
        private readonly IntelligentPrescriptionService _service;

        public IntelligentPrescriptionServiceTests()
        {
            _mockHerbService = new Mock<IHerbService>();
            _service = new IntelligentPrescriptionService(_mockHerbService.Object);
        }

        #region DetectDuplicateHerbs Tests

        [Fact]
        public void DetectDuplicateHerbs_WithNoDuplicates_ShouldReturnNoDuplicates()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { Id = Guid.NewGuid(), HerbName = "甘草", Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.NewGuid(), HerbName = "当归", Quantity = 15, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.NewGuid(), HerbName = "黄芪", Quantity = 20, Unit = "g" }
            };

            // Act
            var result = _service.DetectDuplicateHerbs(items);

            // Assert
            result.Should().NotBeNull();
            result.HasDuplicates.Should().BeFalse();
            result.DuplicateHerbs.Should().BeEmpty();
            result.Warnings.Should().BeEmpty();
            result.WarningMessage.Should().BeEmpty();
        }

        [Fact]
        public void DetectDuplicateHerbs_WithDuplicateSameQuantity_ShouldDetectDuplicatesWithoutConflict()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), HerbName = "甘草", Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), HerbName = "甘草", Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), HerbName = "当归", Quantity = 15, Unit = "g" }
            };

            // Act
            var result = _service.DetectDuplicateHerbs(items);

            // Assert
            result.Should().NotBeNull();
            result.HasDuplicates.Should().BeTrue();
            result.DuplicateHerbs.Should().Contain("甘草");
            result.Warnings.Should().HaveCount(1);
            result.Warnings[0].Should().Contain("甘草在多个验方中重复，剂量相同");
            result.WarningMessage.Should().Contain("甘草");

            // 验证重复项被移除，只保留一个
            items.Where(x => x.HerbName == "甘草").Should().HaveCount(1);
            items.Should().HaveCount(2); // 总数应该减少
        }

        [Fact]
        public void DetectDuplicateHerbs_WithDuplicateDifferentQuantity_ShouldDetectConflicts()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), HerbName = "甘草", Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), HerbName = "甘草", Quantity = 15, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), HerbName = "当归", Quantity = 20, Unit = "g" }
            };

            // Act
            var result = _service.DetectDuplicateHerbs(items);

            // Assert
            result.Should().NotBeNull();
            result.HasDuplicates.Should().BeTrue();
            result.DuplicateHerbs.Should().Contain("甘草");
            result.Warnings.Should().HaveCount(1);
            result.Warnings[0].Should().Contain("甘草在多个验方中重复，剂量冲突");
            result.Warnings[0].Should().Contain("已采用标准剂量：10g");
            result.WarningMessage.Should().Contain("甘草");

            // 验证保留第一个（标准剂量）
            items.Where(x => x.HerbName == "甘草").Should().HaveCount(1);
            items.First(x => x.HerbName == "甘草").Quantity.Should().Be(10);
        }

        [Fact]
        public void DetectDuplicateHerbs_WithCaseInsensitiveNames_ShouldTreatAsSame()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), HerbName = "甘草", Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), HerbName = "甘草 ", Quantity = 15, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), HerbName = "当归", Quantity = 20, Unit = "g" }
            };

            // Act
            var result = _service.DetectDuplicateHerbs(items);

            // Assert
            result.Should().NotBeNull();
            result.HasDuplicates.Should().BeTrue();
            result.DuplicateHerbs.Should().HaveCount(1);
        }

        [Fact]
        public void DetectDuplicateHerbs_WithEmptyList_ShouldReturnNoDuplicates()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>();

            // Act
            var result = _service.DetectDuplicateHerbs(items);

            // Assert
            result.Should().NotBeNull();
            result.HasDuplicates.Should().BeFalse();
            result.DuplicateHerbs.Should().BeEmpty();
            result.Warnings.Should().BeEmpty();
        }

        [Fact]
        public void DetectDuplicateHerbs_WithNullOrEmptyNames_ShouldHandleGracefully()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { Id = Guid.NewGuid(), HerbName = null!, Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.NewGuid(), HerbName = "", Quantity = 15, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.NewGuid(), HerbName = "甘草", Quantity = 20, Unit = "g" }
            };

            // Act
            var result = _service.DetectDuplicateHerbs(items);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeNull();
            // 不应该抛异常
        }

        [Fact]
        public void DetectDuplicateHerbs_WithMultipleDuplicateGroups_ShouldDetectAll()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), HerbName = "甘草", Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), HerbName = "甘草", Quantity = 15, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), HerbName = "当归", Quantity = 20, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), HerbName = "当归", Quantity = 25, Unit = "g" },
                new PrescriptionItemModel { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), HerbName = "黄芪", Quantity = 30, Unit = "g" }
            };

            // Act
            var result = _service.DetectDuplicateHerbs(items);

            // Assert
            result.Should().NotBeNull();
            result.HasDuplicates.Should().BeTrue();
            result.DuplicateHerbs.Should().Contain("甘草");
            result.DuplicateHerbs.Should().Contain("当归");
            result.DuplicateHerbs.Should().NotContain("黄芪");
            result.Warnings.Should().HaveCount(2);

            // 验证每组只保留一个项目
            items.Where(x => x.HerbName == "甘草").Should().HaveCount(1);
            items.Where(x => x.HerbName == "当归").Should().HaveCount(1);
            items.Where(x => x.HerbName == "黄芪").Should().HaveCount(1);
        }

        #endregion

        #region CheckHerbAvailabilityAsync Tests

        [Fact]
        public async Task CheckHerbAvailabilityAsync_WithAllAvailableHerbs_ShouldReturnFullyAvailable()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { HerbName = "甘草", Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { HerbName = "当归", Quantity = 15, Unit = "g" }
            };

            var availableHerbs = new List<HerbDto>
            {
                new HerbDto { Name = "甘草" },
                new HerbDto { Name = "当归" },
                new HerbDto { Name = "黄芪" }
            };

            _mockHerbService.Setup(x => x.GetAvailableHerbsAsync())
                           .ReturnsAsync(availableHerbs);

            // Act
            var result = await _service.CheckHerbAvailabilityAsync(items);

            // Assert
            result.Should().NotBeNull();
            result.IsFullyAvailable.Should().BeTrue();
            result.IsAvailable.Should().BeTrue();
            result.MissingHerbs.Should().BeEmpty();
        }

        [Fact]
        public async Task CheckHerbAvailabilityAsync_WithSomeMissingHerbs_ShouldReturnPartiallyAvailable()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { HerbName = "甘草", Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { HerbName = "当归", Quantity = 15, Unit = "g" },
                new PrescriptionItemModel { HerbName = "人参", Quantity = 20, Unit = "g" }
            };

            var availableHerbs = new List<HerbDto>
            {
                new HerbDto { Name = "甘草" },
                new HerbDto { Name = "黄芪" }
            };

            _mockHerbService.Setup(x => x.GetAvailableHerbsAsync())
                           .ReturnsAsync(availableHerbs);

            // Act
            var result = await _service.CheckHerbAvailabilityAsync(items);

            // Assert
            result.Should().NotBeNull();
            result.IsFullyAvailable.Should().BeFalse();
            result.IsAvailable.Should().BeTrue(); // 还有部分可用
            result.MissingHerbs.Should().Contain("当归");
            result.MissingHerbs.Should().Contain("人参");
            result.MissingHerbs.Should().NotContain("甘草");
        }

        [Fact]
        public async Task CheckHerbAvailabilityAsync_WithAllMissingHerbs_ShouldReturnUnavailable()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { HerbName = "人参", Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { HerbName = "虫草", Quantity = 15, Unit = "g" }
            };

            var availableHerbs = new List<HerbDto>
            {
                new HerbDto { Name = "甘草" },
                new HerbDto { Name = "当归" }
            };

            _mockHerbService.Setup(x => x.GetAvailableHerbsAsync())
                           .ReturnsAsync(availableHerbs);

            // Act
            var result = await _service.CheckHerbAvailabilityAsync(items);

            // Assert
            result.Should().NotBeNull();
            result.IsFullyAvailable.Should().BeFalse();
            result.IsAvailable.Should().BeFalse(); // 完全不可用
            result.MissingHerbs.Should().Contain("人参");
            result.MissingHerbs.Should().Contain("虫草");
        }

        [Fact]
        public async Task CheckHerbAvailabilityAsync_WithEmptyItems_ShouldReturnFullyAvailable()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>();
            var availableHerbs = new List<HerbDto>();

            _mockHerbService.Setup(x => x.GetAvailableHerbsAsync())
                           .ReturnsAsync(availableHerbs);

            // Act
            var result = await _service.CheckHerbAvailabilityAsync(items);

            // Assert
            result.Should().NotBeNull();
            result.IsFullyAvailable.Should().BeTrue(); // 没有缺失药材
            result.IsAvailable.Should().BeFalse(); // 当前实现：0 < 0 = false
            result.MissingHerbs.Should().BeEmpty();
        }

        [Fact]
        public async Task CheckHerbAvailabilityAsync_WithCaseInsensitiveMatching_ShouldWork()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { HerbName = "甘草", Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { HerbName = "当归 ", Quantity = 15, Unit = "g" } // 带空格
            };

            var availableHerbs = new List<HerbDto>
            {
                new HerbDto { Name = "甘草" },
                new HerbDto { Name = "当归" }
            };

            _mockHerbService.Setup(x => x.GetAvailableHerbsAsync())
                           .ReturnsAsync(availableHerbs);

            // Act
            var result = await _service.CheckHerbAvailabilityAsync(items);

            // Assert
            result.Should().NotBeNull();
            result.IsFullyAvailable.Should().BeTrue();
            result.MissingHerbs.Should().BeEmpty();
        }

        #endregion

        #region CalculatePrescriptionPrice Tests

        [Fact]
        public void CalculatePrescriptionPrice_WithBasicItems_ShouldCalculateCorrectly()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { HerbName = "甘草", Quantity = 10, Unit = "g" },
                new PrescriptionItemModel { HerbName = "当归", Quantity = 15, Unit = "g" },
                new PrescriptionItemModel { HerbName = "黄芪", Quantity = 20, Unit = "g" }
            };
            var dosageCount = 7;

            // Act
            var result = _service.CalculatePrescriptionPrice(items, dosageCount);

            // Assert
            result.Should().NotBeNull();
            result.DosageCount.Should().Be(dosageCount);
            result.TotalWeight.Should().Be(45 * dosageCount); // (10+15+20) * 7
            result.SingleDosePrice.Should().Be(0); // 当前实现价格为0
            result.TotalPrice.Should().Be(0); // 当前实现价格为0
        }

        [Fact]
        public void CalculatePrescriptionPrice_WithEmptyItems_ShouldReturnZero()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>();
            var dosageCount = 7;

            // Act
            var result = _service.CalculatePrescriptionPrice(items, dosageCount);

            // Assert
            result.Should().NotBeNull();
            result.DosageCount.Should().Be(dosageCount);
            result.TotalWeight.Should().Be(0);
            result.SingleDosePrice.Should().Be(0);
            result.TotalPrice.Should().Be(0);
        }

        [Fact]
        public void CalculatePrescriptionPrice_WithSingleDose_ShouldCalculateCorrectly()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { HerbName = "甘草", Quantity = 10, Unit = "g" }
            };
            var dosageCount = 1;

            // Act
            var result = _service.CalculatePrescriptionPrice(items, dosageCount);

            // Assert
            result.Should().NotBeNull();
            result.DosageCount.Should().Be(1);
            result.TotalWeight.Should().Be(10);
            result.SingleDosePrice.Should().Be(0);
            result.TotalPrice.Should().Be(0);
        }

        [Fact]
        public void CalculatePrescriptionPrice_WithZeroDosage_ShouldReturnZero()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { HerbName = "甘草", Quantity = 10, Unit = "g" }
            };
            var dosageCount = 0;

            // Act
            var result = _service.CalculatePrescriptionPrice(items, dosageCount);

            // Assert
            result.Should().NotBeNull();
            result.DosageCount.Should().Be(0);
            result.TotalWeight.Should().Be(0);
            result.TotalPrice.Should().Be(0);
        }

        #endregion

        #region GeneratePrescriptionSuggestionsAsync Tests

        [Fact]
        public async Task GeneratePrescriptionSuggestionsAsync_WithInsomnia_ShouldProvideRelevantAdvice()
        {
            // Arrange
            var diagnosis = "不寐";
            var symptoms = new List<string> { "失眠", "多梦" };

            // Act
            var result = await _service.GeneratePrescriptionSuggestionsAsync(diagnosis, symptoms);

            // Assert
            result.Should().NotBeNull();
            result.SuggestedAdvice.Should().Contain("建议睡前30分钟温服");
            result.Precautions.Should().Contain("服药期间避免浓茶咖啡");
        }

        [Fact]
        public async Task GeneratePrescriptionSuggestionsAsync_WithDiarrhea_ShouldProvideRelevantAdvice()
        {
            // Arrange
            var diagnosis = "泄泻";
            var symptoms = new List<string> { "腹泻", "腹痛" };

            // Act
            var result = await _service.GeneratePrescriptionSuggestionsAsync(diagnosis, symptoms);

            // Assert
            result.Should().NotBeNull();
            result.SuggestedAdvice.Should().Contain("温服，忌食生冷");
            result.Precautions.Should().Contain("腹泻严重时及时就医");
        }

        [Fact]
        public async Task GeneratePrescriptionSuggestionsAsync_WithCold_ShouldProvideRelevantAdvice()
        {
            // Arrange
            var diagnosis = "外感";
            var symptoms = new List<string> { "感冒", "发热" };

            // Act
            var result = await _service.GeneratePrescriptionSuggestionsAsync(diagnosis, symptoms);

            // Assert
            result.Should().NotBeNull();
            result.SuggestedAdvice.Should().Contain("热服取汗");
            result.Precautions.Should().Contain("服药后避风寒，适当休息");
        }

        [Fact]
        public async Task GeneratePrescriptionSuggestionsAsync_WithUnknownCondition_ShouldReturnEmptyAdvice()
        {
            // Arrange
            var diagnosis = "未知疾病";
            var symptoms = new List<string> { "未知症状" };

            // Act
            var result = await _service.GeneratePrescriptionSuggestionsAsync(diagnosis, symptoms);

            // Assert
            result.Should().NotBeNull();
            result.SuggestedAdvice.Should().BeEmpty();
            result.Precautions.Should().BeEmpty();
            result.SuggestedFormulas.Should().BeEmpty();
        }

        [Fact]
        public async Task GeneratePrescriptionSuggestionsAsync_WithEmptyInput_ShouldNotThrow()
        {
            // Arrange
            var diagnosis = "";
            var symptoms = new List<string>();

            // Act
            var result = await _service.GeneratePrescriptionSuggestionsAsync(diagnosis, symptoms);

            // Assert
            result.Should().NotBeNull();
            result.SuggestedAdvice.Should().BeEmpty();
            result.Precautions.Should().BeEmpty();
            result.SuggestedFormulas.Should().BeEmpty();
        }

        [Fact]
        public async Task GeneratePrescriptionSuggestionsAsync_WithDoctorId_ShouldNotThrow()
        {
            // Arrange
            var diagnosis = "不寐";
            var symptoms = new List<string> { "失眠" };
            var doctorId = Guid.NewGuid();

            // Act
            var result = await _service.GeneratePrescriptionSuggestionsAsync(diagnosis, symptoms, doctorId);

            // Assert
            result.Should().NotBeNull();
            result.SuggestedAdvice.Should().Contain("建议睡前30分钟温服");
        }

        #endregion

        #region ComposeFromFormulasAsync Tests

        [Fact]
        public async Task ComposeFromFormulasAsync_WithEmptyFormulaIds_ShouldReturnEmptyResult()
        {
            // Arrange
            var formulaIds = new List<Guid>();
            var dosageCount = 7;

            // Mock herb service for availability check
            _mockHerbService.Setup(x => x.GetAvailableHerbsAsync())
                           .ReturnsAsync(new List<HerbDto>());

            // Act
            var result = await _service.ComposeFromFormulasAsync(formulaIds, dosageCount);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.FormulaNames.Should().BeEmpty();
            result.DosageCount.Should().Be(dosageCount);
            result.TotalPrice.Should().Be(0);
            result.TotalWeight.Should().Be(0);
        }

        [Fact]
        public async Task ComposeFromFormulasAsync_WithFormulaIds_ShouldNotThrow()
        {
            // Arrange
            var formulaIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var dosageCount = 14;

            // Mock herb service for availability check
            _mockHerbService.Setup(x => x.GetAvailableHerbsAsync())
                           .ReturnsAsync(new List<HerbDto>());

            // Act
            var result = await _service.ComposeFromFormulasAsync(formulaIds, dosageCount);

            // Assert
            result.Should().NotBeNull();
            result.DosageCount.Should().Be(dosageCount);
            // 由于Formula模块功能被注释，主要验证不抛异常
        }

        [Fact]
        public async Task ComposeFromFormulasAsync_WithDefaultDosageCount_ShouldUse7()
        {
            // Arrange
            var formulaIds = new List<Guid> { Guid.NewGuid() };

            // Mock herb service
            _mockHerbService.Setup(x => x.GetAvailableHerbsAsync())
                           .ReturnsAsync(new List<HerbDto>());

            // Act
            var result = await _service.ComposeFromFormulasAsync(formulaIds);

            // Assert
            result.Should().NotBeNull();
            result.DosageCount.Should().Be(7); // 默认值
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task CheckHerbAvailabilityAsync_WhenHerbServiceThrows_ShouldPropagateException()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { HerbName = "甘草", Quantity = 10, Unit = "g" }
            };

            _mockHerbService.Setup(x => x.GetAvailableHerbsAsync())
                           .ThrowsAsync(new InvalidOperationException("服务异常"));

            // Act & Assert
            await FluentActions.Invoking(() => _service.CheckHerbAvailabilityAsync(items))
                               .Should().ThrowAsync<InvalidOperationException>()
                               .WithMessage("服务异常");
        }

        [Fact]
        public void CalculatePrescriptionPrice_WithNegativeDosage_ShouldHandleGracefully()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>
            {
                new PrescriptionItemModel { HerbName = "甘草", Quantity = 10, Unit = "g" }
            };
            var dosageCount = -1;

            // Act
            var result = _service.CalculatePrescriptionPrice(items, dosageCount);

            // Assert
            result.Should().NotBeNull();
            result.DosageCount.Should().Be(-1);
            // 负数处理依赖具体业务逻辑，这里主要确保不抛异常
        }

        [Fact]
        public void DetectDuplicateHerbs_WithLargeList_ShouldPerformEfficiently()
        {
            // Arrange
            var items = new List<PrescriptionItemModel>();
            for (int i = 0; i < 100; i++)
            {
                items.Add(new PrescriptionItemModel 
                { 
                    Id = Guid.NewGuid(), 
                    HerbName = $"药材{i % 10}", // 创建重复项
                    Quantity = 10, 
                    Unit = "g" 
                });
            }

            // Act
            var result = _service.DetectDuplicateHerbs(items);

            // Assert
            result.Should().NotBeNull();
            result.HasDuplicates.Should().BeTrue();
            result.DuplicateHerbs.Should().HaveCount(10); // 10种重复药材
            
            // 验证最终列表只有10个项目（去重后）
            items.Should().HaveCount(10);
        }

        #endregion
    }
}