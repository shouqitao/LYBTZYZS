using FluentAssertions;
using LYBT.Desktop.Infrastructure.Controls.HerbItem;
using LYBT.Shared.Models.Contracts.Herbs;
using Xunit;

namespace LYBT.Tests.Desktop.ViewModels.MedicalCase
{
    /// <summary>
    /// HerbItemControlViewModel简化单元测试 (原 PrescriptionHerbItem)
    /// Epic #2175 BF-002 Phase 4 Task 4.2: ViewModel单元测试
    /// OpenSpec: unify-frontend-backend-types Phase 8.4 - 类型重命名
    /// OpenSpec: unify-control-data-binding - PrescriptionHerbItem已删除，由HerbItemControlViewModel替代
    ///
    /// 注意：完整测试因依赖复杂度较高暂时简化
    /// 本测试文件聚焦核心功能测试:
    /// 1. HerbItemControlViewModel价格计算功能
    /// 2. 处方项集合操作
    /// </summary>
    public class PrescriptionHerbItem_SimpleTests
    {
        /// <summary>
        /// 计算小计金额 (UnitPrice * Dosage)
        /// HerbItemControlViewModel不再包含ItemAmount/ItemTotal属性，改为在DTO层计算
        /// </summary>
        private static decimal CalculateItemAmount(HerbItemControlViewModel vm) => vm.UnitPrice * vm.Dosage;

        #region HerbItemControlViewModel 价格计算测试

        [Fact]
        public void HerbItemControlViewModel_WhenHerbAndDosageSet_ShouldCalculatePrice()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();
            var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };

            // Act
            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 10;

            // Assert
            CalculateItemAmount(viewModel).Should().Be(150.0m, "价格应为 15 * 10 = 150");
        }

        [Fact]
        public void HerbItemControlViewModel_WhenDosageChanges_ShouldRecalculatePrice()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();
            var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };
            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 10;

            var originalPrice = CalculateItemAmount(viewModel);

            // Act
            viewModel.Dosage = 20;

            // Assert
            CalculateItemAmount(viewModel).Should().BeGreaterThan(originalPrice, "增加剂量后价格应增加");
            CalculateItemAmount(viewModel).Should().Be(300.0m, "新价格应为 15 * 20 = 300");
        }

        [Fact]
        public void HerbItemControlViewModel_WhenHerbChanges_ShouldRecalculatePrice()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();
            var herb1 = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };
            var herb2 = new HerbListDto { Id = Guid.NewGuid(), Name = "党参", Price = 20.0m };

            viewModel.SelectedHerb = herb1;
            viewModel.Dosage = 10;

            var originalPrice = CalculateItemAmount(viewModel);

            // Act
            viewModel.SelectedHerb = herb2;

            // Assert
            CalculateItemAmount(viewModel).Should().BeGreaterThan(originalPrice, "更换为更贵的药材后价格应增加");
            CalculateItemAmount(viewModel).Should().Be(200.0m, "新价格应为 20 * 10 = 200");
        }

        [Fact]
        public void HerbItemControlViewModel_WhenNoHerbSelected_ItemAmountShouldBeZero()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();
            viewModel.Dosage = 10;

            // Act & Assert
            CalculateItemAmount(viewModel).Should().Be(0m, "未选择药材时总价应为0");
        }

        [Fact]
        public void HerbItemControlViewModel_WhenDosageZero_ItemAmountShouldBeZero()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();
            var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };
            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 0;

            // Act & Assert
            CalculateItemAmount(viewModel).Should().Be(0m, "剂量为0时总价应为0");
        }

        [Fact]
        public void HerbItemControlViewModel_WithIntegerDosage_ShouldCalculateCorrectly()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();
            var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.5m };
            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 11;

            // Act & Assert
            // 价格计算: 15.5 * 11 = 170.5
            CalculateItemAmount(viewModel).Should().Be(170.5m, "价格应精确计算: 15.5 * 11 = 170.5");
        }

        #endregion

        #region Property Change Notification测试

        [Fact]
        public void HerbItemControlViewModel_WhenSelectedHerbChanges_ShouldRaisePropertyChanged()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();
            var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };

            var propertyChangedRaised = false;
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.SelectedHerb))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.SelectedHerb = herb;

            // Assert
            propertyChangedRaised.Should().BeTrue("设置SelectedHerb应触发PropertyChanged事件");
        }

        [Fact]
        public void HerbItemControlViewModel_WhenDosageChanges_ShouldRaisePropertyChanged()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();

            var propertyChangedRaised = false;
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.Dosage))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act - 设置为不同于初始值0的值
            viewModel.Dosage = 20;

            // Assert
            propertyChangedRaised.Should().BeTrue("设置Dosage应触发PropertyChanged事件");
        }

        [Fact]
        public void HerbItemControlViewModel_WhenPriceChanges_ShouldRaiseUnitPricePropertyChanged()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();
            var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };

            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 10;

            var unitPriceChangedRaised = false;
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.UnitPrice))
                {
                    unitPriceChangedRaised = true;
                }
            };

            // Act - 更换药材触发UnitPrice变化
            var herb2 = new HerbListDto { Id = Guid.NewGuid(), Name = "黄芪", Price = 20.0m };
            viewModel.SelectedHerb = herb2;

            // Assert
            unitPriceChangedRaised.Should().BeTrue("更换药材应触发UnitPrice的PropertyChanged事件");
        }

        #endregion

        #region 边界条件测试

        [Theory]
        [InlineData(-1)]   // 负数剂量
        [InlineData(-10)] // 负数剂量
        public void HerbItemControlViewModel_WithNegativeDosage_ShouldNotThrow(int dosage)
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();
            var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };
            viewModel.SelectedHerb = herb;

            // Act
            Action act = () => viewModel.Dosage = dosage;

            // Assert
            act.Should().NotThrow("设置负数剂量不应抛出异常");
        }

        [Fact]
        public void HerbItemControlViewModel_WithVeryLargeDosage_ShouldCalculateCorrectly()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();
            var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };
            viewModel.SelectedHerb = herb;

            // Act
            viewModel.Dosage = 1000;

            // Assert
            CalculateItemAmount(viewModel).Should().Be(15000.0m, "超大剂量应正确计算");
        }

        [Fact]
        public void HerbItemControlViewModel_WithZeroPriceHerb_ShouldCalculateZero()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();
            var herb = new HerbListDto { Id = Guid.NewGuid(), Name = "免费药材", Price = 0m };
            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 10;

            // Act & Assert
            CalculateItemAmount(viewModel).Should().Be(0m, "价格为0的药材总价应为0");
        }

        #endregion

        #region HerbName和拼音过滤测试

        [Fact]
        public void HerbItemControlViewModel_WhenHerbNameSet_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();

            // Act
            viewModel.HerbName = "当归";

            // Assert
            viewModel.HerbName.Should().Be("当归");
        }

        [Fact]
        public void HerbItemControlViewModel_WhenHerbNameChanges_ShouldRaisePropertyChanged()
        {
            // Arrange
            var viewModel = new HerbItemControlViewModel();

            var propertyChangedRaised = false;
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.HerbName))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.HerbName = "当归";

            // Assert
            propertyChangedRaised.Should().BeTrue("设置HerbName应触发PropertyChanged事件");
        }

        #endregion
    }
}
