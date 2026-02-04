using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Module.Sync.Services;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Module.Sync.Tests.Services;

/// <summary>
/// ChecksumHelper 单元测试
/// 验证 Checksum 计算的正确性和一致性
/// OpenSpec: implement-data-sync
/// </summary>
public class ChecksumHelperTests
{
    #region Herb Checksum 测试

    [Fact]
    public void ComputeHerbChecksum_WithSameData_ShouldReturnSameChecksum()
    {
        // Arrange
        var herb1 = CreateTestHerb();
        var herb2 = CreateTestHerb();

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().Be(checksum2);
        checksum1.Should().NotBeNullOrEmpty();
        checksum1.Length.Should().Be(64); // SHA256 produces 64 hex characters
    }

    [Fact]
    public void ComputeHerbChecksum_WithDifferentName_ShouldReturnDifferentChecksum()
    {
        // Arrange
        var herb1 = CreateTestHerb();
        var herb2 = CreateTestHerb();
        herb2.Name = "当归";

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ComputeHerbChecksum_WithDifferentPrice_ShouldReturnDifferentChecksum()
    {
        // Arrange
        var herb1 = CreateTestHerb();
        var herb2 = CreateTestHerb();
        herb2.Price = 100m;

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ComputeHerbChecksum_WithDifferentStatus_ShouldReturnDifferentChecksum()
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
    public void ComputeHerbChecksum_WithDifferentAuditFields_ShouldReturnSameChecksum()
    {
        // Arrange - 审计字段不应影响 Checksum
        var herb1 = CreateTestHerb();
        herb1.CreatedAt = DateTime.UtcNow.AddDays(-10);
        herb1.UpdatedAt = DateTime.UtcNow.AddDays(-5);
        herb1.CreatedBy = Guid.NewGuid();
        herb1.UpdatedBy = Guid.NewGuid();

        var herb2 = CreateTestHerb();
        herb2.CreatedAt = DateTime.UtcNow;
        herb2.UpdatedAt = DateTime.UtcNow;
        herb2.CreatedBy = Guid.NewGuid();
        herb2.UpdatedBy = Guid.NewGuid();

        // Act
        var checksum1 = ChecksumHelper.ComputeHerbChecksum(herb1);
        var checksum2 = ChecksumHelper.ComputeHerbChecksum(herb2);

        // Assert
        checksum1.Should().Be(checksum2);
    }

    [Fact]
    public void ComputeHerbChecksum_WithDifferentIsDeleted_ShouldReturnDifferentChecksum()
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

    #region Patient Checksum 测试

    [Fact]
    public void ComputePatientChecksum_WithSameData_ShouldReturnSameChecksum()
    {
        // Arrange
        var patient1 = CreateTestPatient();
        var patient2 = CreateTestPatient();

        // Act
        var checksum1 = ChecksumHelper.ComputePatientChecksum(patient1);
        var checksum2 = ChecksumHelper.ComputePatientChecksum(patient2);

        // Assert
        checksum1.Should().Be(checksum2);
        checksum1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ComputePatientChecksum_WithDifferentName_ShouldReturnDifferentChecksum()
    {
        // Arrange
        var patient1 = CreateTestPatient();
        var patient2 = CreateTestPatient();
        patient2.Name = "李四";

        // Act
        var checksum1 = ChecksumHelper.ComputePatientChecksum(patient1);
        var checksum2 = ChecksumHelper.ComputePatientChecksum(patient2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ComputePatientChecksum_WithDifferentPhoneNumber_ShouldReturnDifferentChecksum()
    {
        // Arrange
        var patient1 = CreateTestPatient();
        var patient2 = CreateTestPatient();
        patient2.PhoneNumber = "13900139001";

        // Act
        var checksum1 = ChecksumHelper.ComputePatientChecksum(patient1);
        var checksum2 = ChecksumHelper.ComputePatientChecksum(patient2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ComputePatientChecksum_WithDifferentAuditFields_ShouldReturnSameChecksum()
    {
        // Arrange
        var patient1 = CreateTestPatient();
        patient1.CreatedAt = DateTime.UtcNow.AddDays(-10);

        var patient2 = CreateTestPatient();
        patient2.CreatedAt = DateTime.UtcNow;

        // Act
        var checksum1 = ChecksumHelper.ComputePatientChecksum(patient1);
        var checksum2 = ChecksumHelper.ComputePatientChecksum(patient2);

        // Assert
        checksum1.Should().Be(checksum2);
    }

    #endregion

    #region Formula Checksum 测试

    [Fact]
    public void ComputeFormulaChecksum_WithSameData_ShouldReturnSameChecksum()
    {
        // Arrange
        var formula1 = CreateTestFormula();
        var formula2 = CreateTestFormula();

        // Act
        var checksum1 = ChecksumHelper.ComputeFormulaChecksum(formula1);
        var checksum2 = ChecksumHelper.ComputeFormulaChecksum(formula2);

        // Assert
        checksum1.Should().Be(checksum2);
        checksum1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ComputeFormulaChecksum_WithDifferentHerbOrder_ShouldReturnSameChecksum()
    {
        // Arrange - Herbs 按 HerbId 排序，顺序不应影响 Checksum
        var herbId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var herbId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var formula1 = CreateTestFormula();
        formula1.Herbs = new List<FormulaHerbItem>
        {
            new() { HerbId = herbId1, HerbName = "黄芪", Dosage = 15, Unit = "g" },
            new() { HerbId = herbId2, HerbName = "当归", Dosage = 10, Unit = "g" }
        };

        var formula2 = CreateTestFormula();
        formula2.Herbs = new List<FormulaHerbItem>
        {
            new() { HerbId = herbId2, HerbName = "当归", Dosage = 10, Unit = "g" },
            new() { HerbId = herbId1, HerbName = "黄芪", Dosage = 15, Unit = "g" }
        };

        // Act
        var checksum1 = ChecksumHelper.ComputeFormulaChecksum(formula1);
        var checksum2 = ChecksumHelper.ComputeFormulaChecksum(formula2);

        // Assert
        checksum1.Should().Be(checksum2);
    }

    [Fact]
    public void ComputeFormulaChecksum_WithDifferentHerbDosage_ShouldReturnDifferentChecksum()
    {
        // Arrange
        var formula1 = CreateTestFormula();
        var formula2 = CreateTestFormula();
        formula2.Herbs!.First().Dosage = 20;

        // Act
        var checksum1 = ChecksumHelper.ComputeFormulaChecksum(formula1);
        var checksum2 = ChecksumHelper.ComputeFormulaChecksum(formula2);

        // Assert
        checksum1.Should().NotBe(checksum2);
    }

    [Fact]
    public void ComputeFormulaChecksum_WithNullHerbs_ShouldNotThrow()
    {
        // Arrange
        var formula = CreateTestFormula();
        formula.Herbs = null;

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

    #endregion

    #region ComputeChecksum 通用方法测试

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

    #endregion

    #region 辅助方法

    private static Herb CreateTestHerb()
    {
        return new Herb
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
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
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
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
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
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
