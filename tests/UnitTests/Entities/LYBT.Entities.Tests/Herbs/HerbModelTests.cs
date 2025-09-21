using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Entities.Tests.Herbs
{
    /// <summary>
    /// Herb实体单元测试 - 测试中药材实体的所有属性和默认值
    /// </summary>
    public class HerbModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Arrange & Act
            var herb = new Herb();

            // Assert
            herb.Id.Should().Be(Guid.Empty);
            herb.Name.Should().Be(string.Empty);
            herb.PinYinCode.Should().BeNull();
            herb.Origin.Should().BeNull();
            herb.Spec.Should().BeNull();
            herb.Unit.Should().Be("克");
            herb.Price.Should().Be(0);
            herb.CostPrice.Should().BeNull();
            herb.Effect.Should().BeNull();
            herb.Usage.Should().BeNull();
            herb.Remark.Should().BeNull();
            herb.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void Id_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();
            var testId = Guid.NewGuid();

            // Act
            herb.Id = testId;

            // Assert
            herb.Id.Should().Be(testId);
        }

        [Fact]
        public void Name_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();
            const string testName = "人参";

            // Act
            herb.Name = testName;

            // Assert
            herb.Name.Should().Be(testName);
        }

        [Fact]
        public void PinYinCode_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();
            const string testPinYinCode = "rs";

            // Act
            herb.PinYinCode = testPinYinCode;

            // Assert
            herb.PinYinCode.Should().Be(testPinYinCode);
        }

        [Fact]
        public void Origin_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();
            const string testOrigin = "吉林长白山";

            // Act
            herb.Origin = testOrigin;

            // Assert
            herb.Origin.Should().Be(testOrigin);
        }

        [Fact]
        public void Spec_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();
            const string testSpec = "一等品";

            // Act
            herb.Spec = testSpec;

            // Assert
            herb.Spec.Should().Be(testSpec);
        }

        [Fact]
        public void Unit_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();
            const string testUnit = "两";

            // Act
            herb.Unit = testUnit;

            // Assert
            herb.Unit.Should().Be(testUnit);
        }

        [Fact]
        public void Unit_DefaultValueShouldBeGram()
        {
            // Arrange & Act
            var herb = new Herb();

            // Assert
            herb.Unit.Should().Be("克");
        }

        [Fact]
        public void Price_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();
            const decimal testPrice = 25.50m;

            // Act
            herb.Price = testPrice;

            // Assert
            herb.Price.Should().Be(testPrice);
        }

        [Fact]
        public void CostPrice_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();
            const decimal testCostPrice = 20.00m;

            // Act
            herb.CostPrice = testCostPrice;

            // Assert
            herb.CostPrice.Should().Be(testCostPrice);
        }

        [Fact]
        public void Effect_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();
            const string testEffect = "大补元气，复脉固脱，补脾益肺，生津止渴，安神益智";

            // Act
            herb.Effect = testEffect;

            // Assert
            herb.Effect.Should().Be(testEffect);
        }

        [Fact]
        public void Usage_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();
            const string testUsage = "煎服，3-9g，宜另煎兑入";

            // Act
            herb.Usage = testUsage;

            // Assert
            herb.Usage.Should().Be(testUsage);
        }

        [Fact]
        public void Remark_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();
            const string testRemark = "珍贵药材，请妥善保存";

            // Act
            herb.Remark = testRemark;

            // Assert
            herb.Remark.Should().Be(testRemark);
        }

        [Fact]
        public void Status_PropertyCanBeSetAndGet()
        {
            // Arrange
            var herb = new Herb();

            // Act & Assert
            herb.Status = CommonStatus.Disabled;
            herb.Status.Should().Be(CommonStatus.Disabled);

            herb.Status = CommonStatus.Enabled;
            herb.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void Status_DefaultValueShouldBeEnabled()
        {
            // Arrange & Act
            var herb = new Herb();

            // Assert
            herb.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void NullableProperties_CanBeSetToNull()
        {
            // Arrange
            var herb = new Herb();

            // Act
            herb.PinYinCode = null;
            herb.Origin = null;
            herb.Spec = null;
            herb.CostPrice = null;
            herb.Effect = null;
            herb.Usage = null;
            herb.Remark = null;

            // Assert
            herb.PinYinCode.Should().BeNull();
            herb.Origin.Should().BeNull();
            herb.Spec.Should().BeNull();
            herb.CostPrice.Should().BeNull();
            herb.Effect.Should().BeNull();
            herb.Usage.Should().BeNull();
            herb.Remark.Should().BeNull();
        }

        [Fact]
        public void CreateCompleteHerb_ShouldSetAllProperties()
        {
            // Arrange
            var herb = new Herb();
            var herbId = Guid.NewGuid();

            // Act
            herb.Id = herbId;
            herb.Name = "当归";
            herb.PinYinCode = "dg";
            herb.Origin = "甘肃岷县";
            herb.Spec = "特级";
            herb.Unit = "克";
            herb.Price = 15.80m;
            herb.CostPrice = 12.50m;
            herb.Effect = "补血活血，调经止痛，润肠通便";
            herb.Usage = "煎服，6-12g";
            herb.Remark = "常用补血药";
            herb.Status = CommonStatus.Enabled;

            // Assert
            herb.Id.Should().Be(herbId);
            herb.Name.Should().Be("当归");
            herb.PinYinCode.Should().Be("dg");
            herb.Origin.Should().Be("甘肃岷县");
            herb.Spec.Should().Be("特级");
            herb.Unit.Should().Be("克");
            herb.Price.Should().Be(15.80m);
            herb.CostPrice.Should().Be(12.50m);
            herb.Effect.Should().Be("补血活血，调经止痛，润肠通便");
            herb.Usage.Should().Be("煎服，6-12g");
            herb.Remark.Should().Be("常用补血药");
            herb.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void PriceCalculations_ShouldHandleDecimalPrecision()
        {
            // Arrange
            var herb = new Herb();

            // Act
            herb.Price = 123.456m;
            herb.CostPrice = 98.789m;

            // Assert
            herb.Price.Should().Be(123.456m);
            herb.CostPrice.Should().Be(98.789m);
        }

        [Fact]
        public void MultipleInstances_ShouldBeIndependent()
        {
            // Arrange & Act
            var herb1 = new Herb
            {
                Id = Guid.NewGuid(),
                Name = "人参",
                Price = 50.00m
            };

            var herb2 = new Herb
            {
                Id = Guid.NewGuid(),
                Name = "当归",
                Price = 15.80m
            };

            // Assert
            herb1.Id.Should().NotBe(herb2.Id);
            herb1.Name.Should().NotBe(herb2.Name);
            herb1.Price.Should().NotBe(herb2.Price);
        }

        [Fact]
        public void EmptyStrings_ShouldBeHandledCorrectly()
        {
            // Arrange
            var herb = new Herb();

            // Act
            herb.Name = "";
            herb.Unit = "";

            // Assert
            herb.Name.Should().Be("");
            herb.Unit.Should().Be("");
        }

        [Fact]
        public void LongStrings_ShouldBeAccepted()
        {
            // Arrange
            var herb = new Herb();
            var longEffect = new string('中', 500); // 500个中文字符
            var longUsage = new string('用', 500);

            // Act
            herb.Effect = longEffect;
            herb.Usage = longUsage;

            // Assert
            herb.Effect.Should().Be(longEffect);
            herb.Usage.Should().Be(longUsage);
            herb.Effect.Should().HaveLength(500);
            herb.Usage.Should().HaveLength(500);
        }
    }
}