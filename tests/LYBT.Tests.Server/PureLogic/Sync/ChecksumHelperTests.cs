using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Module.Sync.Services;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Sync;

/// <summary>
/// ChecksumHelper 单元测试
/// 测试职责: 算法正确性、边界条件、类型路由
/// 不测试: HTTP、DI、持久化 (集成测试覆盖)
/// OpenSpec: implement-data-sync
/// </summary>
public class ChecksumHelperTests
{
    #region 算法正确性测试 - Herb

    [Fact]
    public void ComputeHerbChecksum_WithSameData_ShouldReturnSameChecksum()
    {
        // Arrange - 使用相同 Id 确保完全相同的数据
        var sharedId = Guid.NewGuid();
        var herb1 = CreateTestHerb();
        herb1.Id = sharedId;
        var herb2 = CreateTestHerb();
        herb2.Id = sharedId;

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().Be(checksum2);
        checksum1.Should().NotBeNullOrEmpty();
        checksum1.Length.Should().Be(64, "SHA256 produces 64 hex characters");
    }

    [Fact]
    public void ComputeHerbChecksum_MultipleCallsSameData_ShouldReturnSame()
    {
        // Arrange - 确定性测试
        var herb = CreateTestHerb();

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb);
        var checksum3 = ChecksumHelper.ComputeHerbChecksum(herb);

        // Assert
        checksum1.Should().Be(checksum2).And.Be(checksum3);
    }

    [Theory]
    [InlineData("Name", "当归")]
    [InlineData("PinYinCode", "DG")]
    [InlineData("Category", "活血化瘀药")]
    [InlineData("Origin", "四川")]
    [InlineData("Spec", "特级")]
    [InlineData("Unit", "kg")]
    [InlineData("Effect", "补血活血")]
    [InlineData("Usage", "煎服15g")]
    [InlineData("Remark", "新备注")]
    public void ComputeHerbChecksum_WithDifferentStringField_ShouldReturnDifferent(string fieldName, string newValue)
    {
        // Arrange
        var herb1 = CreateTestHerb();
        var herb2 = CreateTestHerb();

        var property = typeof(Herb).GetProperty(fieldName);
        property!.SetValue(herb2, newValue);

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().NotBe(checksum2, $"Field '{fieldName}' change should affect checksum");
    }

    [Fact]
    public void ComputeHerbChecksum_WithDifferentPrice_ShouldReturnDifferent()
    {
        // Arrange
        var herb1 = CreateTestHerb();
        var herb2 = CreateTestHerb();
        herb2.Price = 100.50m;

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ComputeHerbChecksum_WithDifferentCostPrice_ShouldReturnDifferent()
    {
        // Arrange
        var herb1 = CreateTestHerb();
        var herb2 = CreateTestHerb();
        herb2.CostPrice = 80.00m;

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ComputeHerbChecksum_WithDifferentStatus_ShouldReturnDifferent()
    {
        // Arrange
        var herb1 = CreateTestHerb();
        var herb2 = CreateTestHerb();
        herb2.Status = CommonStatus.Disabled;

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ComputeHerbChecksum_WithDifferentIsDeleted_ShouldReturnDifferent()
    {
        // Arrange
        var herb1 = CreateTestHerb();
        herb1.IsDeleted = false;

        var herb2 = CreateTestHerb();
        herb2.IsDeleted = true;

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    #endregion

    #region 审计字段排除测试 - Herb

    [Fact]
    public void ComputeHerbChecksum_WithDifferentCreatedAt_ShouldReturnSame()
    {
        // Arrange - 使用相同 Id
        var sharedId = Guid.NewGuid();
        var herb1 = CreateTestHerb();
        herb1.Id = sharedId;
        herb1.CreatedAt = DateTime.UtcNow.AddDays(-10);

        var herb2 = CreateTestHerb();
        herb2.Id = sharedId;
        herb2.CreatedAt = DateTime.UtcNow;

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().Be(checksum2, "CreatedAt is an audit field and should not affect checksum");
    }

    [Fact]
    public void ComputeHerbChecksum_WithDifferentUpdatedAt_ShouldReturnSame()
    {
        // Arrange - 使用相同 Id
        var sharedId = Guid.NewGuid();
        var herb1 = CreateTestHerb();
        herb1.Id = sharedId;
        herb1.UpdatedAt = DateTime.UtcNow.AddDays(-5);

        var herb2 = CreateTestHerb();
        herb2.Id = sharedId;
        herb2.UpdatedAt = DateTime.UtcNow;

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().Be(checksum2, "UpdatedAt is an audit field and should not affect checksum");
    }

    [Fact]
    public void ComputeHerbChecksum_WithDifferentCreatedBy_ShouldReturnSame()
    {
        // Arrange - 使用相同 Id
        var sharedId = Guid.NewGuid();
        var herb1 = CreateTestHerb();
        herb1.Id = sharedId;
        herb1.CreatedBy = Guid.NewGuid();

        var herb2 = CreateTestHerb();
        herb2.Id = sharedId;
        herb2.CreatedBy = Guid.NewGuid();

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().Be(checksum2, "CreatedBy is an audit field and should not affect checksum");
    }

    [Fact]
    public void ComputeHerbChecksum_WithDifferentUpdatedBy_ShouldReturnSame()
    {
        // Arrange - 使用相同 Id
        var sharedId = Guid.NewGuid();
        var herb1 = CreateTestHerb();
        herb1.Id = sharedId;
        herb1.UpdatedBy = Guid.NewGuid();

        var herb2 = CreateTestHerb();
        herb2.Id = sharedId;
        herb2.UpdatedBy = Guid.NewGuid();

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().Be(checksum2, "UpdatedBy is an audit field and should not affect checksum");
    }

    #endregion

    #region 算法正确性测试 - Patient

    [Fact]
    public void ComputePatientChecksum_WithSameData_ShouldReturnSameChecksum()
    {
        // Arrange - 使用相同 Id
        var sharedId = Guid.NewGuid();
        var patient1 = CreateTestPatient();
        patient1.Id = sharedId;
        var patient2 = CreateTestPatient();
        patient2.Id = sharedId;

        // Act
        var checksum1 = ChecksumHelper.ComputePatientChecksum(patient1);
        var checksum2 = ChecksumHelper.ComputePatientChecksum(patient2);

        // Assert
        checksum1.Should().Be(checksum2);
        checksum1.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("Name", "李四")]
    [InlineData("PinYinCode", "LS")]
    [InlineData("PhoneNumber", "13900139001")]
    [InlineData("IdNumber", "110101199001150022")]
    [InlineData("Address", "上海市浦东新区")]
    [InlineData("AllergyHistory", "青霉素过敏")]
    [InlineData("MedicalHistory", "高血压病史")]
    [InlineData("DisableReason", "迁移")]
    public void ComputePatientChecksum_WithDifferentStringField_ShouldReturnDifferent(string fieldName, string newValue)
    {
        // Arrange
        var patient1 = CreateTestPatient();
        var patient2 = CreateTestPatient();

        var property = typeof(Patient).GetProperty(fieldName);
        property!.SetValue(patient2, newValue);

        // Act
        var checksum1 = ChecksumHelper.ComputePatientChecksum(patient1);
        var checksum2 = ChecksumHelper.ComputePatientChecksum(patient2);

        // Assert
        checksum1.Should().NotBe(checksum2, $"Field '{fieldName}' change should affect checksum");
    }

    [Fact]
    public void ComputePatientChecksum_WithDifferentGender_ShouldReturnDifferent()
    {
        // Arrange
        var patient1 = CreateTestPatient();
        var patient2 = CreateTestPatient();
        patient2.Gender = Gender.Female;

        // Act
        var checksum1 = ChecksumHelper.ComputePatientChecksum(patient1);
        var checksum2 = ChecksumHelper.ComputePatientChecksum(patient2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ComputePatientChecksum_WithDifferentBirthDate_ShouldReturnDifferent()
    {
        // Arrange
        var patient1 = CreateTestPatient();
        var patient2 = CreateTestPatient();
        patient2.BirthDate = new DateTime(1985, 6, 20);

        // Act
        var checksum1 = ChecksumHelper.ComputePatientChecksum(patient1);
        var checksum2 = ChecksumHelper.ComputePatientChecksum(patient2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ComputePatientChecksum_WithDifferentAuditFields_ShouldReturnSame()
    {
        // Arrange - 使用相同的业务数据，只有审计字段不同
        var sharedId = Guid.NewGuid();

        var patient1 = CreateTestPatient();
        patient1.Id = sharedId; // 确保相同 Id
        patient1.CreatedAt = DateTime.UtcNow.AddDays(-10);
        patient1.UpdatedAt = DateTime.UtcNow.AddDays(-5);
        patient1.CreatedBy = Guid.NewGuid();
        patient1.UpdatedBy = Guid.NewGuid();

        var patient2 = CreateTestPatient();
        patient2.Id = sharedId; // 确保相同 Id
        patient2.CreatedAt = DateTime.UtcNow;
        patient2.UpdatedAt = DateTime.UtcNow;
        patient2.CreatedBy = Guid.NewGuid();
        patient2.UpdatedBy = Guid.NewGuid();

        // Act
        var checksum1 = ChecksumHelper.ComputePatientChecksum(patient1);
        var checksum2 = ChecksumHelper.ComputePatientChecksum(patient2);

        // Assert
        checksum1.Should().Be(checksum2, "Audit fields should not affect checksum");
    }

    #endregion

    #region 算法正确性测试 - Formula

    [Fact]
    public void ComputeFormulaChecksum_WithSameData_ShouldReturnSameChecksum()
    {
        // Arrange - 使用相同 Id
        var sharedId = Guid.NewGuid();
        var formula1 = CreateTestFormula();
        formula1.Id = sharedId;
        var formula2 = CreateTestFormula();
        formula2.Id = sharedId;

        // Act
        var checksum1 = ChecksumHelper.ComputeFormulaChecksum(formula1);
        var checksum2 = ChecksumHelper.ComputeFormulaChecksum(formula2);

        // Assert
        checksum1.Should().Be(checksum2);
        checksum1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ComputeFormulaChecksum_WithDifferentHerbOrder_ShouldReturnSame()
    {
        // Arrange - 使用相同 Id，Herbs 按 HerbId 排序，顺序不应影响 Checksum
        var sharedId = Guid.NewGuid();
        var herbId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var herbId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var formula1 = CreateTestFormula();
        formula1.Id = sharedId;
        formula1.Herbs = new List<FormulaHerbItem>
        {
            new() { HerbId = herbId1, HerbName = "黄芪", Dosage = 15, Unit = "g" },
            new() { HerbId = herbId2, HerbName = "当归", Dosage = 10, Unit = "g" }
        };

        var formula2 = CreateTestFormula();
        formula2.Id = sharedId;
        formula2.Herbs = new List<FormulaHerbItem>
        {
            new() { HerbId = herbId2, HerbName = "当归", Dosage = 10, Unit = "g" },
            new() { HerbId = herbId1, HerbName = "黄芪", Dosage = 15, Unit = "g" }
        };

        // Act
        var checksum1 = ChecksumHelper.ComputeFormulaChecksum(formula1);
        var checksum2 = ChecksumHelper.ComputeFormulaChecksum(formula2);

        // Assert
        checksum1.Should().Be(checksum2, "Herbs are sorted by HerbId, order should not matter");
    }

    [Fact]
    public void ComputeFormulaChecksum_WithDifferentHerbDosage_ShouldReturnDifferent()
    {
        // Arrange
        var herbId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var formula1 = CreateTestFormula();
        var formula2 = CreateTestFormula();

        // 使用 Single 查找特定 Herb，避免依赖集合顺序
        var targetHerb = formula2.Herbs!.Single(h => h.HerbId == herbId);
        targetHerb.Dosage = 20;

        // Act
        var checksum1 = ChecksumHelper.ComputeFormulaChecksum(formula1);
        var checksum2 = ChecksumHelper.ComputeFormulaChecksum(formula2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ComputeFormulaChecksum_WithDifferentHerbRemark_ShouldReturnDifferent()
    {
        // Arrange
        var formula1 = CreateTestFormula();
        var formula2 = CreateTestFormula();
        formula2.Herbs!.First().Remark = "先煎";

        // Act
        var checksum1 = ChecksumHelper.ComputeFormulaChecksum(formula1);
        var checksum2 = ChecksumHelper.ComputeFormulaChecksum(formula2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Theory]
    [InlineData("Name", "逍遥散")]
    [InlineData("Category", "理气剂")]
    [InlineData("Effect", "疏肝解郁")]
    [InlineData("Indication", "肝郁气滞")]
    [InlineData("Usage", "每日一剂")]
    [InlineData("Remark", "经典名方")]
    [InlineData("Property", "凉")]
    public void ComputeFormulaChecksum_WithDifferentStringField_ShouldReturnDifferent(string fieldName, string newValue)
    {
        // Arrange
        var formula1 = CreateTestFormula();
        var formula2 = CreateTestFormula();

        var property = typeof(Formula).GetProperty(fieldName);
        property!.SetValue(formula2, newValue);

        // Act
        var checksum1 = ChecksumHelper.ComputeFormulaChecksum(formula1);
        var checksum2 = ChecksumHelper.ComputeFormulaChecksum(formula2);

        // Assert
        checksum1.Should().NotBe(checksum2, $"Field '{fieldName}' change should affect checksum");
    }

    [Fact]
    public void ComputeFormulaChecksum_WithDifferentFormulaType_ShouldReturnDifferent()
    {
        // Arrange
        var formula1 = CreateTestFormula();
        formula1.FormulaType = FormulaType.Classic;

        var formula2 = CreateTestFormula();
        formula2.FormulaType = FormulaType.Experience; // Classic=1, Experience=2

        // Act
        var checksum1 = ChecksumHelper.ComputeFormulaChecksum(formula1);
        var checksum2 = ChecksumHelper.ComputeFormulaChecksum(formula2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    #endregion

    #region 边界条件测试 - Null/Empty

    [Fact]
    public void ComputeFormulaChecksum_WithNullHerbs_ShouldNotThrow()
    {
        // Arrange
        var formula = CreateTestFormula();
        formula.Herbs = null!; // 故意赋值 null 以测试方法健壮性

        // Act
        var act = () => ChecksumHelper.ComputeFormulaChecksum(formula);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ComputeFormulaChecksum_WithEmptyHerbs_ShouldNotThrow()
    {
        // Arrange
        var formula = CreateTestFormula();
        formula.Herbs = new List<FormulaHerbItem>();

        // Act
        var act = () => ChecksumHelper.ComputeFormulaChecksum(formula);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ComputeHerbChecksum_WithNullName_ShouldNotThrow()
    {
        // Arrange
        var herb = CreateTestHerb();
        herb.Name = null!;

        // Act
        var act = () => ChecksumHelper.ComputeHerbChecksum(herb);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ComputeHerbChecksum_WithEmptyName_ShouldReturnDifferentFromNonEmpty()
    {
        // Arrange
        var herb1 = CreateTestHerb();
        herb1.Name = "";

        var herb2 = CreateTestHerb();
        herb2.Name = "黄芪";

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    #endregion

    #region 边界条件测试 - 特殊字符

    [Fact]
    public void ComputeHerbChecksum_WithSpecialCharacters_ShouldHandle()
    {
        // Arrange
        var herb = CreateTestHerb();
        herb.Name = "黄芪（蜜炙）";
        herb.Effect = "补气升阳，\n固表止汗";
        herb.Remark = "注意：孕妇慎用！";

        // Act
        var act = () => ChecksumHelper.ComputeHerbChecksum(herb);

        // Assert
        act.Should().NotThrow();
        var checksum = act();
        checksum.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ComputeHerbChecksum_WithUnicodeCharacters_ShouldHandle()
    {
        // Arrange
        var herb = CreateTestHerb();
        herb.Name = "黃耆"; // 繁体中文
        herb.Remark = "测试 Unicode: \u4e2d\u6587";

        // Act
        var checksum = ChecksumHelper.ComputeHerbChecksum(herb);

        // Assert
        checksum.Should().NotBeNullOrEmpty();
        checksum.Length.Should().Be(64);
    }

    #endregion

    #region 边界条件测试 - 数值精度

    [Fact]
    public void ComputeHerbChecksum_WithSameDecimalValue_ShouldBeConsistent()
    {
        // Arrange - 使用相同 Id 和相同精度的 decimal
        var sharedId = Guid.NewGuid();
        var herb1 = CreateTestHerb();
        herb1.Id = sharedId;
        herb1.Price = 50.00m;

        var herb2 = CreateTestHerb();
        herb2.Id = sharedId;
        herb2.Price = 50.00m; // 相同值，相同精度

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().Be(checksum2, "Identical decimal values should produce same checksum");
    }

    [Fact]
    public void ComputeHerbChecksum_WithSmallDecimalDifference_ShouldReturnDifferent()
    {
        // Arrange
        var herb1 = CreateTestHerb();
        herb1.Price = 50.00m;

        var herb2 = CreateTestHerb();
        herb2.Price = 50.01m;

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().NotBe(checksum2, "Small decimal difference should produce different checksum");
    }

    #endregion

    #region 边界条件测试 - 日期

    [Fact]
    public void ComputePatientChecksum_WithDateTimeBoundaries_ShouldHandle()
    {
        // Arrange
        var patient1 = CreateTestPatient();
        patient1.BirthDate = DateTime.MinValue;

        var patient2 = CreateTestPatient();
        patient2.BirthDate = DateTime.MaxValue;

        // Act
        var act1 = () => ChecksumHelper.ComputePatientChecksum(patient1);
        var act2 = () => ChecksumHelper.ComputePatientChecksum(patient2);

        // Assert
        act1.Should().NotThrow();
        act2.Should().NotThrow();
        act1().Should().NotBe(act2());
    }

    #endregion

    #region 边界条件测试 - 大数据量

    [Fact]
    public void ComputeFormulaChecksum_WithLargeHerbsList_ShouldHandle()
    {
        // Arrange
        var formula = CreateTestFormula();
        formula.Herbs = Enumerable.Range(1, 100)
            .Select(i => new FormulaHerbItem
            {
                HerbId = Guid.NewGuid(),
                HerbName = $"药材{i}",
                Dosage = i,
                Unit = "g"
            })
            .ToList();

        // Act
        var act = () => ChecksumHelper.ComputeFormulaChecksum(formula);

        // Assert
        act.Should().NotThrow();
        var checksum = act();
        checksum.Should().NotBeNullOrEmpty();
        checksum.Length.Should().Be(64);
    }

    #endregion

    #region 类型路由测试

    [Theory]
    [InlineData("Herb")]
    [InlineData("Patient")]
    [InlineData("Formula")]
    public void ComputeChecksum_WithValidEntityType_ShouldNotThrow(string entityType)
    {
        // Arrange
        object entity = entityType switch
        {
            "Herb" => CreateTestHerb(),
            "Patient" => CreateTestPatient(),
            "Formula" => CreateTestFormula(),
            _ => throw new ArgumentException()
        };

        // Act
        var act = () => ChecksumHelper.ComputeChecksum(entity, entityType);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ComputeChecksum_WithInvalidEntityType_ShouldThrowArgumentException()
    {
        // Arrange
        var herb = CreateTestHerb();

        // Act
        var act = () => ChecksumHelper.ComputeChecksum(herb, "InvalidType");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*不支持的实体类型*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("herb")] // 小写
    [InlineData("HERB")] // 大写
    public void ComputeChecksum_WithInvalidCaseOrEmpty_ShouldThrowArgumentException(string entityType)
    {
        // Arrange
        var herb = CreateTestHerb();

        // Act
        var act = () => ChecksumHelper.ComputeChecksum(herb, entityType);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region 测试数据工厂方法

    private static Herb CreateTestHerb()
    {
        return new Herb
        {
            Id = Guid.NewGuid(), // 每次生成新 ID，避免测试干扰
            Name = "黄芪",
            PinYinCode = "HQ",
            Category = "补气药",
            Origin = "内蒙古",
            Spec = "统货",
            Unit = "g",
            Price = 50m,
            CostPrice = 30m,
            Effect = "补气升阳，固表止汗",
            Usage = "9-30g",
            Remark = "生用偏于走表，炙用偏于补中",
            Status = CommonStatus.Enabled,
            IsDeleted = false
        };
    }

    private static Patient CreateTestPatient()
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            Name = "张三",
            PinYinCode = "ZS",
            Gender = Gender.Male,
            BirthDate = new DateTime(1990, 1, 15),
            IdNumber = "110101199001150011",
            PhoneNumber = "13800138000",
            Address = "北京市朝阳区",
            AllergyHistory = "无",
            MedicalHistory = "既往体健",
            Status = CommonStatus.Enabled,
            DisableReason = null,
            IsDeleted = false
        };
    }

    private static Formula CreateTestFormula()
    {
        var herbId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var herbId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        return new Formula
        {
            Id = Guid.NewGuid(),
            Name = "补中益气汤",
            Category = "补益剂",
            Effect = "补中益气，升阳举陷",
            Indication = "脾胃气虚",
            Usage = "水煎服",
            Remark = "李东垣名方",
            Property = "温",
            Status = CommonStatus.Enabled,
            FormulaType = FormulaType.Classic,
            IsDeleted = false,
            Herbs = new List<FormulaHerbItem>
            {
                new() { HerbId = herbId1, HerbName = "黄芪", Dosage = 15, Unit = "g" },
                new() { HerbId = herbId2, HerbName = "党参", Dosage = 10, Unit = "g" }
            }
        };
    }

    #endregion
}
