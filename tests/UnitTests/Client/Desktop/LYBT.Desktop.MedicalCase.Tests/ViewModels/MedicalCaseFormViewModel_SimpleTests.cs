using FluentAssertions;
using LYBT.Desktop.Prescriptions.Models.Items;
using LYBT.Shared.Models.Contracts.Herbs;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.ViewModels
{
    /// <summary>
    /// PrescriptionHerbItem简化单元测试
    /// Epic #2175 BF-002 Phase 4 Task 4.2: ViewModel单元测试
    /// OpenSpec: unify-frontend-backend-types Phase 8.4 - 类型重命名
    ///
    /// 注意：完整测试因依赖复杂度较高暂时简化
    /// 本测试文件聚焦核心功能测试:
    /// 1. PrescriptionHerbItem价格计算功能
    /// 2. 处方项集合操作
    /// </summary>
    public class PrescriptionHerbItem_SimpleTests
    {

        #region Prescript ionItemViewModel 价格计算测试

        [Fact]
        public void PrescriptionHerbItem_WhenHerbAndDosageSet_ShouldCalculatePrice()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();
            var herb = new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };

            // Act
            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 10;

            // Assert
            viewModel.ItemAmount.Should().Be(150.0m, "价格应为 15 * 10 = 150");
        }

        [Fact]
        public void PrescriptionHerbItem_WhenDosageChanges_ShouldRecalculatePrice()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();
            var herb = new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };
            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 10;

            var originalPrice = viewModel.ItemAmount;

            // Act
            viewModel.Dosage = 20;

            // Assert
            viewModel.ItemAmount.Should().BeGreaterThan(originalPrice, "增加剂量后价格应增加");
            viewModel.ItemAmount.Should().Be(300.0m, "新价格应为 15 * 20 = 300");
        }

        [Fact]
        public void PrescriptionHerbItem_WhenHerbChanges_ShouldRecalculatePrice()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();
            var herb1 = new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };
            var herb2 = new HerbDetailDto { Id = Guid.NewGuid(), Name = "党参", Price = 20.0m };

            viewModel.SelectedHerb = herb1;
            viewModel.Dosage = 10;

            var originalPrice = viewModel.ItemAmount;

            // Act
            viewModel.SelectedHerb = herb2;

            // Assert
            viewModel.ItemAmount.Should().BeGreaterThan(originalPrice, "更换为更贵的药材后价格应增加");
            viewModel.ItemAmount.Should().Be(200.0m, "新价格应为 20 * 10 = 200");
        }

        [Fact]
        public void PrescriptionHerbItem_WhenNoHerbSelected_ItemAmountShouldBeZero()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();
            viewModel.Dosage = 10;

            // Act & Assert
            viewModel.ItemAmount.Should().Be(0m, "未选择药材时总价应为0");
        }

        [Fact]
        public void PrescriptionHerbItem_WhenDosageZero_ItemAmountShouldBeZero()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();
            var herb = new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };
            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 0;

            // Act & Assert
            viewModel.ItemAmount.Should().Be(0m, "剂量为0时总价应为0");
        }

        [Fact]
        public void PrescriptionHerbItem_WithDecimalDosage_ShouldCalculateCorrectly()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();
            var herb = new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.5m };
            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 11;

            // Act & Assert
            // 价格计算: 15.5 * 11 = 170.5
            viewModel.ItemAmount.Should().Be(170.5m, "价格应精确计算: 15.5 * 11 = 170.5");
        }

        #endregion

        #region Property Change Notification测试

        [Fact]
        public void PrescriptionHerbItem_WhenSelectedHerbChanges_ShouldRaisePropertyChanged()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();
            var herb = new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };

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
        public void PrescriptionHerbItem_WhenDosageChanges_ShouldRaisePropertyChanged()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();

            var propertyChangedRaised = false;
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.Dosage))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act - 设置为不同于初始值10的值
            viewModel.Dosage = 20;

            // Assert
            propertyChangedRaised.Should().BeTrue("设置Dosage应触发PropertyChanged事件");
        }

        [Fact]
        public void PrescriptionHerbItem_WhenPriceChanges_ShouldRaiseItemTotalPropertyChanged()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();
            var herb = new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };

            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 10;

            var totalPriceChangedRaised = false;
            viewModel.PropertyChanged += (sender, args) =>
            {
                // OpenSpec: unify-frontend-backend-types Phase 8.4
                // PrescriptionHerbItem使用ItemTotal作为主属性，ItemAmount是别名
                if (args.PropertyName == nameof(viewModel.ItemTotal))
                {
                    totalPriceChangedRaised = true;
                }
            };

            // Act - 修改剂量触发价格变化
            viewModel.Dosage = 20;

            // Assert
            totalPriceChangedRaised.Should().BeTrue("剂量变化应触发ItemTotal的PropertyChanged事件");
        }

        #endregion

        #region 边界条件测试

        [Theory]
        [InlineData(-1)]   // 负数剂量
        [InlineData(-10)] // 负数剂量
        public void PrescriptionHerbItem_WithNegativeDosage_ShouldNotThrow(int dosage)
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();
            var herb = new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };
            viewModel.SelectedHerb = herb;

            // Act
            Action act = () => viewModel.Dosage = dosage;

            // Assert
            act.Should().NotThrow("设置负数剂量不应抛出异常");
        }

        [Fact]
        public void PrescriptionHerbItem_WithVeryLargeDosage_ShouldCalculateCorrectly()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();
            var herb = new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", Price = 15.0m };
            viewModel.SelectedHerb = herb;

            // Act
            viewModel.Dosage = 1000;

            // Assert
            viewModel.ItemAmount.Should().Be(15000.0m, "超大剂量应正确计算");
        }

        [Fact]
        public void PrescriptionHerbItem_WithZeroPriceHerb_ShouldCalculateZero()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();
            var herb = new HerbDetailDto { Id = Guid.NewGuid(), Name = "免费药材", Price = 0m };
            viewModel.SelectedHerb = herb;
            viewModel.Dosage = 10;

            // Act & Assert
            viewModel.ItemAmount.Should().Be(0m, "价格为0的药材总价应为0");
        }

        #endregion

        #region HerbName和拼音过滤测试

        [Fact]
        public void PrescriptionHerbItem_WhenHerbNameSet_ShouldUpdateProperty()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();

            // Act
            viewModel.HerbName = "当归";

            // Assert
            viewModel.HerbName.Should().Be("当归");
        }

        [Fact]
        public void PrescriptionHerbItem_WhenHerbNameChanges_ShouldRaisePropertyChanged()
        {
            // Arrange
            var viewModel = new PrescriptionHerbItem();

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
