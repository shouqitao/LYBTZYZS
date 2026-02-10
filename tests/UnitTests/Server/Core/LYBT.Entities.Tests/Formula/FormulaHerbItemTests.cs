using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Entities.Tests.Formula
{
    /// <summary>
    /// FormulaHerbItem实体单元测试 - 测试验方药材明细实体的所有属性和默认值
    /// FormulaHerbItem不继承BaseEntity，有独立的Id
    /// 属性：Id, FormulaId, HerbId (Guid?), OriginalHerbName, IsValidated,
    ///       HerbName, Dosage (int), Unit, Usage, Remark, ProcessingMethod, DecocteMethod
    /// 导航属性：Formula
    /// </summary>
    public class FormulaHerbItemTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Arrange & Act
            var formulaHerbItem = new FormulaHerbItem();

            // Assert
            formulaHerbItem.HerbId.Should().BeNull("HerbId为可空Guid，默认为null");
            formulaHerbItem.HerbName.Should().Be(string.Empty);
            formulaHerbItem.Dosage.Should().Be(1);
            formulaHerbItem.Unit.Should().Be("g");
            formulaHerbItem.Usage.Should().BeNull();
            formulaHerbItem.Remark.Should().BeNull();
            formulaHerbItem.ProcessingMethod.Should().BeNull();
            formulaHerbItem.OriginalHerbName.Should().BeNull();
            formulaHerbItem.IsValidated.Should().BeFalse();
            formulaHerbItem.DecocteMethod.Should().Be(DecocteMethod.Default);
        }

        [Fact]
        public void HerbId_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();
            var testHerbId = Guid.NewGuid();

            // Act
            formulaHerbItem.HerbId = testHerbId;

            // Assert
            formulaHerbItem.HerbId.Should().Be(testHerbId);
        }

        [Fact]
        public void HerbId_CanBeSetToNull()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();
            formulaHerbItem.HerbId = Guid.NewGuid();

            // Act
            formulaHerbItem.HerbId = null;

            // Assert
            formulaHerbItem.HerbId.Should().BeNull("HerbId支持延迟绑定，可以为null");
        }

        [Fact]
        public void HerbName_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();
            const string testHerbName = "当归";

            // Act
            formulaHerbItem.HerbName = testHerbName;

            // Assert
            formulaHerbItem.HerbName.Should().Be(testHerbName);
        }

        [Fact]
        public void Dosage_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();
            const int testDosage = 12;

            // Act
            formulaHerbItem.Dosage = testDosage;

            // Assert
            formulaHerbItem.Dosage.Should().Be(testDosage);
        }

        [Fact]
        public void Dosage_DefaultValueShouldBeOne()
        {
            // Arrange & Act
            var formulaHerbItem = new FormulaHerbItem();

            // Assert
            formulaHerbItem.Dosage.Should().Be(1);
        }

        [Fact]
        public void Unit_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();
            const string testUnit = "克";

            // Act
            formulaHerbItem.Unit = testUnit;

            // Assert
            formulaHerbItem.Unit.Should().Be(testUnit);
        }

        [Fact]
        public void Unit_DefaultValueShouldBeG()
        {
            // Arrange & Act
            var formulaHerbItem = new FormulaHerbItem();

            // Assert
            formulaHerbItem.Unit.Should().Be("g");
        }

        [Fact]
        public void Usage_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();
            const string testUsage = "后下";

            // Act
            formulaHerbItem.Usage = testUsage;

            // Assert
            formulaHerbItem.Usage.Should().Be(testUsage);
        }

        [Fact]
        public void Remark_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();
            const string testRemark = "君药";

            // Act
            formulaHerbItem.Remark = testRemark;

            // Assert
            formulaHerbItem.Remark.Should().Be(testRemark);
        }

        [Fact]
        public void NullableProperties_CanBeSetToNull()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();

            // Act
            formulaHerbItem.Usage = null;
            formulaHerbItem.Remark = null;
            formulaHerbItem.ProcessingMethod = null;
            formulaHerbItem.OriginalHerbName = null;
            formulaHerbItem.HerbId = null;

            // Assert
            formulaHerbItem.Usage.Should().BeNull();
            formulaHerbItem.Remark.Should().BeNull();
            formulaHerbItem.ProcessingMethod.Should().BeNull();
            formulaHerbItem.OriginalHerbName.Should().BeNull();
            formulaHerbItem.HerbId.Should().BeNull();
        }

        [Fact]
        public void CreateCompleteFormulaHerbItem_ShouldSetAllProperties()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();
            var herbId = Guid.NewGuid();

            // Act
            formulaHerbItem.HerbId = herbId;
            formulaHerbItem.HerbName = "当归";
            formulaHerbItem.Dosage = 12;
            formulaHerbItem.Unit = "克";
            formulaHerbItem.Usage = "酒洗";
            formulaHerbItem.Remark = "补血圣药";
            formulaHerbItem.ProcessingMethod = "酒炙";
            formulaHerbItem.IsValidated = true;

            // Assert
            formulaHerbItem.HerbId.Should().Be(herbId);
            formulaHerbItem.HerbName.Should().Be("当归");
            formulaHerbItem.Dosage.Should().Be(12);
            formulaHerbItem.Unit.Should().Be("克");
            formulaHerbItem.Usage.Should().Be("酒洗");
            formulaHerbItem.Remark.Should().Be("补血圣药");
            formulaHerbItem.ProcessingMethod.Should().Be("酒炙");
            formulaHerbItem.IsValidated.Should().BeTrue();
        }

        [Fact]
        public void Dosage_ShouldHandleZeroAndNegativeValues()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();

            // Act & Assert
            formulaHerbItem.Dosage = 0;
            formulaHerbItem.Dosage.Should().Be(0);

            formulaHerbItem.Dosage = -5;
            formulaHerbItem.Dosage.Should().Be(-5);
        }

        [Fact]
        public void MultipleInstances_ShouldBeIndependent()
        {
            // Arrange & Act
            var item1 = new FormulaHerbItem
            {
                HerbId = Guid.NewGuid(),
                HerbName = "当归",
                Dosage = 12
            };

            var item2 = new FormulaHerbItem
            {
                HerbId = Guid.NewGuid(),
                HerbName = "白芍",
                Dosage = 15
            };

            // Assert
            item1.HerbId.Should().NotBe(item2.HerbId!.Value);
            item1.HerbName.Should().NotBe(item2.HerbName);
            item1.Dosage.Should().NotBe(item2.Dosage);
        }

        [Fact]
        public void HerbName_ShouldHandleEmptyString()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();

            // Act
            formulaHerbItem.HerbName = "";

            // Assert
            formulaHerbItem.HerbName.Should().Be("");
        }

        [Fact]
        public void Unit_ShouldHandleEmptyString()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();

            // Act
            formulaHerbItem.Unit = "";

            // Assert
            formulaHerbItem.Unit.Should().Be("");
        }

        [Fact]
        public void LongStrings_ShouldBeAccepted()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();
            var longUsage = new string('用', 200); // 200个字符
            var longRemark = new string('注', 200);

            // Act
            formulaHerbItem.Usage = longUsage;
            formulaHerbItem.Remark = longRemark;

            // Assert
            formulaHerbItem.Usage.Should().Be(longUsage);
            formulaHerbItem.Remark.Should().Be(longRemark);
            formulaHerbItem.Usage.Should().HaveLength(200);
            formulaHerbItem.Remark.Should().HaveLength(200);
        }

        [Fact]
        public void SpecialCharacters_ShouldBeHandled()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();

            // Act
            formulaHerbItem.HerbName = "当归(酒洗)";
            formulaHerbItem.Usage = "先煎30分钟";
            formulaHerbItem.Remark = "功效：补血、活血、调经";

            // Assert
            formulaHerbItem.HerbName.Should().Be("当归(酒洗)");
            formulaHerbItem.Usage.Should().Be("先煎30分钟");
            formulaHerbItem.Remark.Should().Be("功效：补血、活血、调经");
        }

        [Fact]
        public void CommonUnits_ShouldBeSupported()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();

            // Act & Assert - 测试常用单位
            formulaHerbItem.Unit = "克";
            formulaHerbItem.Unit.Should().Be("克");

            formulaHerbItem.Unit = "钱";
            formulaHerbItem.Unit.Should().Be("钱");

            formulaHerbItem.Unit = "两";
            formulaHerbItem.Unit.Should().Be("两");

            formulaHerbItem.Unit = "g";
            formulaHerbItem.Unit.Should().Be("g");

            formulaHerbItem.Unit = "ml";
            formulaHerbItem.Unit.Should().Be("ml");
        }

        [Fact]
        public void DecocteMethod_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();

            // Act & Assert
            formulaHerbItem.DecocteMethod = DecocteMethod.Default;
            formulaHerbItem.DecocteMethod.Should().Be(DecocteMethod.Default);
        }

        [Fact]
        public void FormulaId_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formulaHerbItem = new FormulaHerbItem();
            var testFormulaId = Guid.NewGuid();

            // Act
            formulaHerbItem.FormulaId = testFormulaId;

            // Assert
            formulaHerbItem.FormulaId.Should().Be(testFormulaId);
        }
    }
}
