using FluentAssertions;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Tests.DataSources.Mappers;

/// <summary>
/// UserDataSourceMapper 单元测试
/// Phase 4.4: Infrastructure P2 测试
/// </summary>
public class UserDataSourceMapperTests
{
    private readonly UserDataSourceMapper _mapper = new();

    #region ToEntity(UserDetailDto) Tests

    [Fact]
    public void ToEntity_FromDetailDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new UserDetailDto
        {
            Id = Guid.NewGuid(),
            UserName = "doctor1",
            RealName = "张医生",
            PinYinCode = "ZYS",
            Role = UserRole.Doctor,
            PhoneNumber = "13800138000",
            Email = "doctor1@clinic.com",
            Status = CommonStatus.Enabled,
            Remark = "主治医师",
            FailedLoginCount = 0,
            CreatedAt = DateTime.Now.AddMonths(-6),
            LastLoginTime = DateTime.Now.AddDays(-1)
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Id.Should().Be(dto.Id);
        entity.UserName.Should().Be(dto.UserName);
        entity.RealName.Should().Be(dto.RealName);
        entity.PinYinCode.Should().Be(dto.PinYinCode);
        entity.Role.Should().Be(dto.Role);
        entity.PhoneNumber.Should().Be(dto.PhoneNumber);
        entity.Email.Should().Be(dto.Email);
        entity.Status.Should().Be(dto.Status);
        entity.Remark.Should().Be(dto.Remark);
    }

    #endregion

    #region ToDetailDto Tests

    [Fact]
    public void ToDetailDto_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new User
        {
            Id = Guid.NewGuid(),
            UserName = "nurse1",
            RealName = "李护士",
            PinYinCode = "LHS",
            Role = UserRole.Receptionist,
            PhoneNumber = "13900139000",
            Email = "nurse1@clinic.com",
            Status = CommonStatus.Enabled,
            Remark = "护理人员",
            FailedLoginCount = 0,
            CreatedAt = DateTime.Now.AddMonths(-3),
            LastLoginTime = DateTime.Now
        };

        // Act
        var dto = _mapper.ToDetailDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.UserName.Should().Be(entity.UserName);
        dto.RealName.Should().Be(entity.RealName);
        dto.PinYinCode.Should().Be(entity.PinYinCode);
        dto.Role.Should().Be(entity.Role);
        dto.PhoneNumber.Should().Be(entity.PhoneNumber);
        dto.Email.Should().Be(entity.Email);
        dto.Status.Should().Be(entity.Status);
    }

    #endregion

    #region RoundTrip Tests

    [Fact]
    public void RoundTrip_DetailDto_ShouldPreserveData()
    {
        // Arrange
        var originalEntity = new User
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            RealName = "系统管理员",
            PinYinCode = "XTGLY",
            Role = UserRole.Admin,
            PhoneNumber = "13700137000",
            Email = "admin@clinic.com",
            Status = CommonStatus.Enabled,
            Remark = "管理账户",
            FailedLoginCount = 0,
            CreatedAt = DateTime.Now.AddYears(-1),
            LastLoginTime = DateTime.Now
        };

        // Act
        var dto = _mapper.ToDetailDto(originalEntity);
        var roundTrippedEntity = _mapper.ToEntity(dto);

        // Assert
        roundTrippedEntity.Id.Should().Be(originalEntity.Id);
        roundTrippedEntity.UserName.Should().Be(originalEntity.UserName);
        roundTrippedEntity.RealName.Should().Be(originalEntity.RealName);
        roundTrippedEntity.Role.Should().Be(originalEntity.Role);
        roundTrippedEntity.PhoneNumber.Should().Be(originalEntity.PhoneNumber);
        roundTrippedEntity.Email.Should().Be(originalEntity.Email);
        roundTrippedEntity.Status.Should().Be(originalEntity.Status);
    }

    #endregion

    #region ToEntity(UserInputDto) Tests

    [Fact]
    public void ToEntity_FromInputDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new UserInputDto
        {
            Id = Guid.NewGuid(),
            UserName = "newuser",
            RealName = "新用户",
            PinYinCode = "XYH",
            Role = UserRole.Doctor,
            PhoneNumber = "13600136000",
            Email = "newuser@clinic.com",
            Remark = "新入职"
        };

        // Act
        var entity = _mapper.ToEntity(dto);

        // Assert
        entity.Id.Should().Be(dto.Id!.Value);
        entity.UserName.Should().Be(dto.UserName);
        entity.RealName.Should().Be(dto.RealName);
        entity.Role.Should().Be(dto.Role);
        entity.PhoneNumber.Should().Be(dto.PhoneNumber);
        entity.Email.Should().Be(dto.Email);
    }

    [Fact]
    public void ToInputDto_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new User
        {
            Id = Guid.NewGuid(),
            UserName = "existuser",
            RealName = "已有用户",
            PinYinCode = "YYYH",
            Role = UserRole.Receptionist,
            PhoneNumber = "13500135000",
            Email = "existuser@clinic.com",
            Remark = "测试用户"
        };

        // Act
        var dto = _mapper.ToInputDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.UserName.Should().Be(entity.UserName);
        dto.RealName.Should().Be(entity.RealName);
        dto.Role.Should().Be(entity.Role);
        dto.PhoneNumber.Should().Be(entity.PhoneNumber);
        dto.Email.Should().Be(entity.Email);
    }

    #endregion
}
