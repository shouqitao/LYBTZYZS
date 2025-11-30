using FluentAssertions;
using LYBT.Shared.Models.Enums;
using Xunit;
using FormulaEntity = LYBT.Entities.Formula.Formula;
using LYBT.Entities.Formulas;

namespace LYBT.Entities.Tests.Formula
{
    /// <summary>
    /// Formula实体单元测试 - 测试验方实体的所有属性和默认值
    /// </summary>
    public class FormulaModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Arrange & Act
            var formula = new FormulaEntity();

            // Assert
            formula.Id.Should().Be(Guid.Empty);
            formula.Name.Should().Be(string.Empty);
            formula.Effect.Should().BeNull();
            formula.Usage.Should().BeNull();
            formula.Remark.Should().BeNull();
            formula.Property.Should().BeNull();
            formula.Status.Should().Be(CommonStatus.Enabled);
            formula.IsShared.Should().BeFalse();
            formula.Herbs.Should().NotBeNull();
            formula.Herbs.Should().BeEmpty();
        }

        [Fact]
        public void Id_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formula = new FormulaEntity();
            var testId = Guid.NewGuid();

            // Act
            formula.Id = testId;

            // Assert
            formula.Id.Should().Be(testId);
        }

        [Fact]
        public void Name_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formula = new FormulaEntity();
            const string testName = "四物汤";

            // Act
            formula.Name = testName;

            // Assert
            formula.Name.Should().Be(testName);
        }

        [Fact]
        public void Effect_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formula = new FormulaEntity();
            const string testEffect = "补血和血，调经化瘀";

            // Act
            formula.Effect = testEffect;

            // Assert
            formula.Effect.Should().Be(testEffect);
        }

        [Fact]
        public void Usage_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formula = new FormulaEntity();
            const string testUsage = "水煎服，每日一剂，分二次服";

            // Act
            formula.Usage = testUsage;

            // Assert
            formula.Usage.Should().Be(testUsage);
        }

        [Fact]
        public void Remark_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formula = new FormulaEntity();
            const string testRemark = "经典补血方剂";

            // Act
            formula.Remark = testRemark;

            // Assert
            formula.Remark.Should().Be(testRemark);
        }

        [Fact]
        public void Property_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formula = new FormulaEntity();
            const string testProperty = "甘温，归肝、心、脾经";

            // Act
            formula.Property = testProperty;

            // Assert
            formula.Property.Should().Be(testProperty);
        }

        [Fact]
        public void Status_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formula = new FormulaEntity();

            // Act & Assert
            formula.Status = CommonStatus.Disabled;
            formula.Status.Should().Be(CommonStatus.Disabled);

            formula.Status = CommonStatus.Enabled;
            formula.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void Status_DefaultValueShouldBeEnabled()
        {
            // Arrange & Act
            var formula = new FormulaEntity();

            // Assert
            formula.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void IsShared_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formula = new FormulaEntity();

            // Act
            formula.IsShared = true;

            // Assert
            formula.IsShared.Should().BeTrue();
        }

        [Fact]
        public void IsShared_DefaultValueShouldBeFalse()
        {
            // Arrange & Act
            var formula = new FormulaEntity();

            // Assert
            formula.IsShared.Should().BeFalse();
        }

        [Fact]
        public void Herbs_PropertyCanBeSetAndGet()
        {
            // Arrange
            var formula = new FormulaEntity();
            var testHerbs = new List<FormulaHerbItem>
            {
                new() { HerbName = "当归", Quantity = 1 },
                new() { HerbName = "白芍", Quantity = 1 }
            };

            // Act
            formula.Herbs = testHerbs;

            // Assert
            formula.Herbs.Should().BeSameAs(testHerbs);
            formula.Herbs.Should().HaveCount(2);
        }

        [Fact]
        public void Herbs_ShouldBeInitializedAsEmptyList()
        {
            // Arrange & Act
            var formula = new FormulaEntity();

            // Assert
            formula.Herbs.Should().NotBeNull();
            formula.Herbs.Should().BeEmpty();
        }

        [Fact]
        public void NullableProperties_CanBeSetToNull()
        {
            // Arrange
            var formula = new FormulaEntity();

            // Act
            formula.Effect = null;
            formula.Usage = null;
            formula.Remark = null;
            formula.Property = null;

            // Assert
            formula.Effect.Should().BeNull();
            formula.Usage.Should().BeNull();
            formula.Remark.Should().BeNull();
            formula.Property.Should().BeNull();
        }

        [Fact]
        public void CreateCompleteFormula_ShouldSetAllProperties()
        {
            // Arrange
            var formula = new FormulaEntity();
            var formulaId = Guid.NewGuid();
            var herbs = new List<FormulaHerbItem>
            {
                new() { HerbName = "当归", Quantity = 12, Unit = "克" },
                new() { HerbName = "白芍", Quantity = 12, Unit = "克" },
                new() { HerbName = "川芎", Quantity = 9, Unit = "克" },
                new() { HerbName = "熟地黄", Quantity = 15, Unit = "克" }
            };

            // Act
            formula.Id = formulaId;
            formula.Name = "四物汤";
            formula.Effect = "补血和血，调经化瘀";
            formula.Usage = "水煎服，每日一剂，分二次服用";
            formula.Remark = "妇科调经要方";
            formula.Property = "甘温，归肝、心、脾经";
            formula.Status = CommonStatus.Enabled;
            formula.IsShared = true;
            formula.Herbs = herbs;

            // Assert
            formula.Id.Should().Be(formulaId);
            formula.Name.Should().Be("四物汤");
            formula.Effect.Should().Be("补血和血，调经化瘀");
            formula.Usage.Should().Be("水煎服，每日一剂，分二次服用");
            formula.Remark.Should().Be("妇科调经要方");
            formula.Property.Should().Be("甘温，归肝、心、脾经");
            formula.Status.Should().Be(CommonStatus.Enabled);
            formula.IsShared.Should().BeTrue();
            formula.Herbs.Should().HaveCount(4);
            formula.Herbs.Should().Contain(h => h.HerbName == "当归" && h.Quantity == 12);
        }

        [Fact]
        public void MultipleInstances_ShouldBeIndependent()
        {
            // Arrange & Act
            var formula1 = new FormulaEntity
            {
                Id = Guid.NewGuid(),
                Name = "四物汤",
                IsShared = true
            };

            var formula2 = new FormulaEntity
            {
                Id = Guid.NewGuid(),
                Name = "六味地黄丸",
                IsShared = false
            };

            // Assert
            formula1.Id.Should().NotBe(formula2.Id);
            formula1.Name.Should().NotBe(formula2.Name);
            formula1.IsShared.Should().NotBe(formula2.IsShared);
        }

        [Fact]
        public void Herbs_ListOperations_ShouldWork()
        {
            // Arrange
            var formula = new FormulaEntity();
            var herb1 = new FormulaHerbItem { HerbName = "当归", Quantity = 12 };
            var herb2 = new FormulaHerbItem { HerbName = "白芍", Quantity = 12 };

            // Act
            formula.Herbs.Add(herb1);
            formula.Herbs.Add(herb2);

            // Assert
            formula.Herbs.Should().HaveCount(2);
            formula.Herbs.Should().Contain(herb1);
            formula.Herbs.Should().Contain(herb2);

            // Act - Remove
            formula.Herbs.Remove(herb1);

            // Assert
            formula.Herbs.Should().HaveCount(1);
            formula.Herbs.Should().NotContain(herb1);
            formula.Herbs.Should().Contain(herb2);
        }

        [Fact]
        public void LongStrings_ShouldBeAccepted()
        {
            // Arrange
            var formula = new FormulaEntity();
            var longEffect = new string('效', 500); // 500个字符
            var longUsage = new string('用', 500);
            var longRemark = new string('注', 500);

            // Act
            formula.Effect = longEffect;
            formula.Usage = longUsage;
            formula.Remark = longRemark;

            // Assert
            formula.Effect.Should().Be(longEffect);
            formula.Usage.Should().Be(longUsage);
            formula.Remark.Should().Be(longRemark);
            formula.Effect.Should().HaveLength(500);
            formula.Usage.Should().HaveLength(500);
            formula.Remark.Should().HaveLength(500);
        }

        [Fact]
        public void EmptyStrings_ShouldBeHandledCorrectly()
        {
            // Arrange
            var formula = new FormulaEntity();

            // Act
            formula.Name = "";

            // Assert
            formula.Name.Should().Be("");
        }
    }
}