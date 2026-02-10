using FluentAssertions;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using MedicalCaseEntity = LYBT.Entities.MedicalCases.MedicalCase;

namespace LYBT.Desktop.Infrastructure.Tests.DataSources.Mappers;

/// <summary>
/// MedicalCaseDataSourceMapper 单元测试
/// Phase 4.4: Infrastructure P2 测试
/// </summary>
public class MedicalCaseDataSourceMapperTests
{
    private readonly MedicalCaseDataSourceMapper _mapper = new();

    #region ToEntity(MedicalCaseDetailDto) Tests

    [Fact]
    public void ToEntity_FromDetailDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new MedicalCaseDetailDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PatientName = "张三",
            DoctorName = "李医生",
            CaseNumber = "MC20260205001",
            CaseStatus = MedicalCaseStatus.Active,
            Remark = "初诊",
            CreatedAt = DateTime.Now.AddHours(-2)
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Id.Should().Be(dto.Id);
        entity.PatientId.Should().Be(dto.PatientId);
        entity.UserId.Should().Be(dto.UserId);
        entity.PatientName.Should().Be(dto.PatientName);
        entity.DoctorName.Should().Be(dto.DoctorName);
        entity.CaseNumber.Should().Be(dto.CaseNumber);
        entity.CaseStatus.Should().Be(dto.CaseStatus);
        entity.Remark.Should().Be(dto.Remark);
    }

    #endregion

    #region ToDetailDto Tests

    [Fact]
    public void ToDetailDto_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new MedicalCaseEntity
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PatientName = "李四",
            DoctorName = "王医生",
            CaseNumber = "MC20260205002",
            CaseStatus = MedicalCaseStatus.Completed,
            Remark = "复诊",
            CreatedAt = DateTime.Now.AddDays(-1),
            CompletedAt = DateTime.Now
        };

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.PatientId.Should().Be(entity.PatientId);
        dto.UserId.Should().Be(entity.UserId);
        dto.PatientName.Should().Be(entity.PatientName);
        dto.DoctorName.Should().Be(entity.DoctorName);
        dto.CaseNumber.Should().Be(entity.CaseNumber);
        dto.CaseStatus.Should().Be(entity.CaseStatus);
        dto.Remark.Should().Be(entity.Remark);
    }

    #endregion

    #region RoundTrip Tests

    [Fact]
    public void RoundTrip_DetailDto_ShouldPreserveData()
    {
        // Arrange
        var originalEntity = new MedicalCaseEntity
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PatientName = "王五",
            DoctorName = "赵医生",
            CaseNumber = "MC20260205003",
            CaseStatus = MedicalCaseStatus.Active,
            Remark = "开处方",
            CreatedAt = DateTime.Now
        };

        // Act
        var dto = _mapper.ToDetailDto(originalEntity);
        var roundTrippedEntity = _mapper.ToEntity(dto);

        // Assert
        roundTrippedEntity.Id.Should().Be(originalEntity.Id);
        roundTrippedEntity.PatientId.Should().Be(originalEntity.PatientId);
        roundTrippedEntity.UserId.Should().Be(originalEntity.UserId);
        roundTrippedEntity.PatientName.Should().Be(originalEntity.PatientName);
        roundTrippedEntity.DoctorName.Should().Be(originalEntity.DoctorName);
        roundTrippedEntity.CaseNumber.Should().Be(originalEntity.CaseNumber);
        roundTrippedEntity.CaseStatus.Should().Be(originalEntity.CaseStatus);
    }

    #endregion

    #region ToEntity(MedicalCaseInputDto) Tests

    [Fact]
    public void ToEntity_FromInputDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new MedicalCaseInputDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            NeedsPrescription = true,
            Remark = "新建医案"
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Id.Should().Be(dto.Id!.Value);
        entity.PatientId.Should().Be(dto.PatientId);
        entity.Remark.Should().Be(dto.Remark);
    }

    [Fact]
    public void ToInputDto_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new MedicalCaseEntity
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PatientName = "测试患者",
            DoctorName = "测试医生",
            NeedsPrescription = true,
            Remark = "编辑医案"
        };

        // Act
        var dto = _mapper.ToInputDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.PatientId.Should().Be(entity.PatientId);
        dto.NeedsPrescription.Should().Be(entity.NeedsPrescription);
        dto.Remark.Should().Be(entity.Remark);
    }

    #endregion
}
