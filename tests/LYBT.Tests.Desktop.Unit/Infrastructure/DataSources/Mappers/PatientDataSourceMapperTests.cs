using FluentAssertions;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Tests.DataSources.Mappers;

/// <summary>
/// PatientDataSourceMapper 单元测试
/// Phase 4.4: Infrastructure P2 测试
/// </summary>
public class PatientDataSourceMapperTests
{
    private readonly PatientDataSourceMapper _mapper = new();

    #region ToEntity(PatientDetailDto) Tests

    [Fact]
    public void ToEntity_FromDetailDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new PatientDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "张三",
            PinYinCode = "ZS",
            Gender = Gender.Male,
            BirthDate = new DateTime(1980, 5, 15),
            PhoneNumber = "13800138001",
            IdNumber = "110101198005150011",
            Address = "北京市朝阳区",
            AllergyHistory = "青霉素过敏",
            MedicalHistory = "高血压病史",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now.AddMonths(-12),
            LastVisitTime = DateTime.Now.AddDays(-7),
            VisitCount = 5
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Id.Should().Be(dto.Id);
        entity.Name.Should().Be(dto.Name);
        entity.PinYinCode.Should().Be(dto.PinYinCode);
        entity.Gender.Should().Be(dto.Gender);
        entity.BirthDate.Should().Be(dto.BirthDate);
        entity.PhoneNumber.Should().Be(dto.PhoneNumber);
        entity.IdNumber.Should().Be(dto.IdNumber);
        entity.Address.Should().Be(dto.Address);
        entity.AllergyHistory.Should().Be(dto.AllergyHistory);
        entity.MedicalHistory.Should().Be(dto.MedicalHistory);
        entity.Status.Should().Be(dto.Status);
    }

    #endregion

    #region ToDetailDto Tests

    [Fact]
    public void ToDetailDto_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "李四",
            PinYinCode = "LS",
            Gender = Gender.Female,
            BirthDate = new DateTime(1990, 8, 20),
            PhoneNumber = "13900139001",
            IdNumber = "110101199008200022",
            Address = "北京市海淀区",
            AllergyHistory = "无",
            MedicalHistory = "无",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now.AddMonths(-6),
            LastVisitTime = DateTime.Now.AddDays(-3),
            VisitCount = 2
        };

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
        dto.PinYinCode.Should().Be(entity.PinYinCode);
        dto.Gender.Should().Be(entity.Gender);
        dto.BirthDate.Should().Be(entity.BirthDate);
        dto.PhoneNumber.Should().Be(entity.PhoneNumber);
        dto.IdNumber.Should().Be(entity.IdNumber);
        dto.Address.Should().Be(entity.Address);
        dto.Status.Should().Be(entity.Status);
    }

    #endregion

    #region RoundTrip Tests

    [Fact]
    public void RoundTrip_DetailDto_ShouldPreserveData()
    {
        // Arrange
        var originalEntity = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "王五",
            PinYinCode = "WW",
            Gender = Gender.Male,
            BirthDate = new DateTime(1975, 3, 10),
            PhoneNumber = "13700137001",
            IdNumber = "110101197503100033",
            Address = "北京市西城区",
            AllergyHistory = "磺胺类过敏",
            MedicalHistory = "糖尿病",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now.AddYears(-2),
            LastVisitTime = DateTime.Now,
            VisitCount = 10
        };

        // Act
        var dto = _mapper.ToDetailDto(originalEntity);
        var roundTrippedEntity = _mapper.ToEntity(dto);

        // Assert
        roundTrippedEntity.Id.Should().Be(originalEntity.Id);
        roundTrippedEntity.Name.Should().Be(originalEntity.Name);
        roundTrippedEntity.Gender.Should().Be(originalEntity.Gender);
        roundTrippedEntity.BirthDate.Should().Be(originalEntity.BirthDate);
        roundTrippedEntity.PhoneNumber.Should().Be(originalEntity.PhoneNumber);
        roundTrippedEntity.IdNumber.Should().Be(originalEntity.IdNumber);
        roundTrippedEntity.Status.Should().Be(originalEntity.Status);
    }

    #endregion

    #region ToEntity(PatientInputDto) Tests

    [Fact]
    public void ToEntity_FromInputDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new PatientInputDto
        {
            Id = Guid.NewGuid(),
            Name = "赵六",
            PinYinCode = "ZL",
            Gender = Gender.Female,
            BirthDate = new DateTime(1985, 12, 1),
            PhoneNumber = "13600136001",
            IdNumber = "110101198512010044",
            Address = "北京市东城区",
            AllergyHistory = "无",
            MedicalHistory = "无"
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Id.Should().Be(dto.Id!.Value);
        entity.Name.Should().Be(dto.Name);
        entity.Gender.Should().Be(dto.Gender);
        entity.BirthDate.Should().Be(dto.BirthDate);
        entity.PhoneNumber.Should().Be(dto.PhoneNumber);
        entity.IdNumber.Should().Be(dto.IdNumber);
    }

    [Fact]
    public void ToInputDto_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "孙七",
            PinYinCode = "SQ",
            Gender = Gender.Male,
            BirthDate = new DateTime(1995, 6, 15),
            PhoneNumber = "13500135001",
            IdNumber = "110101199506150055",
            Address = "北京市丰台区",
            AllergyHistory = "花粉过敏",
            MedicalHistory = "哮喘"
        };

        // Act
        var dto = _mapper.ToInputDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
        dto.Gender.Should().Be(entity.Gender);
        dto.BirthDate.Should().Be(entity.BirthDate);
        dto.PhoneNumber.Should().Be(entity.PhoneNumber);
        dto.IdNumber.Should().Be(entity.IdNumber);
    }

    #endregion
}
