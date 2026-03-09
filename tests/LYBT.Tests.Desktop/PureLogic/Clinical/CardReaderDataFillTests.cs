using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Desktop.PureLogic.Clinical;

/// <summary>
/// US-CARD-002: 读卡数据填充到患者表单
/// 验收标准:
///   AC1: 身份证号已存在 -> 返回已有患者信息 + LastVisitTime
///   AC2: 身份证号不存在 -> QuickCreatePatient 创建新患者 + IsNewlyCreated=true
///   AC3: 新创建 -> IsNewlyCreated=true; 已有 -> IsNewlyCreated=false
/// Business Rules:
///   BR5: 读卡数据自动映射: 姓名->Name, 身份证号->IdNumber, 出生日期->BirthDate, 性别->Gender
/// </summary>
public class CardReaderDataFillTests
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<PatientCardReaderIntegration> _logger;
    private readonly PatientCardReaderIntegration _sut;

    public CardReaderDataFillTests()
    {
        _patientRepository = Substitute.For<IPatientRepository>();
        _logger = Substitute.For<ILogger<PatientCardReaderIntegration>>();
        _sut = new PatientCardReaderIntegration(_patientRepository, _logger);
    }

    #region Test Helpers

    private static CardReadResult CreateSuccessCardResult(
        string name = "李四",
        string idNumber = "320102199505151234",
        Gender gender = Gender.Male,
        DateTime? birthDate = null)
    {
        return new CardReadResult
        {
            IsSuccess = true,
            Name = name,
            IdNumber = idNumber,
            Gender = gender,
            BirthDate = birthDate ?? new DateTime(1995, 5, 15),
            Address = "江苏省南京市玄武区中山路1号",
            Nation = "汉",
            CardType = CardType.IdCard
        };
    }

    private static PatientDetailDto CreateExistingPatient(
        Guid? id = null,
        string name = "李四",
        string idNumber = "320102199505151234",
        DateTime? lastVisitTime = null,
        int visitCount = 3)
    {
        return new PatientDetailDto
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            IdNumber = idNumber,
            Gender = Gender.Male,
            BirthDate = new DateTime(1995, 5, 15),
            LastVisitTime = lastVisitTime ?? new DateTime(2026, 3, 1),
            VisitCount = visitCount
        };
    }

    #endregion

    #region AC1: 身份证号已存在 -> 返回已有患者信息 + LastVisitTime

    [Fact]
    public async Task FindPatientByIdNumber_existing_patient_returns_info_with_LastVisitTime()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var lastVisit = new DateTime(2026, 3, 1, 10, 30, 0);
        var existing = CreateExistingPatient(patientId, lastVisitTime: lastVisit, visitCount: 5);
        _patientRepository.GetByIdNumberAsync("320102199505151234").Returns(existing);

        // Act
        var result = await _sut.FindPatientByIdNumberAsync("320102199505151234");

        // Assert - AC1: 返回已有患者信息 + LastVisitTime
        result.Should().NotBeNull();
        result!.PatientId.Should().Be(patientId);
        result.Name.Should().Be("李四");
        result.IdNumber.Should().Be("320102199505151234");
        result.LastVisitTime.Should().Be(lastVisit);
        result.VisitCount.Should().Be(5);
        result.IsNewlyCreated.Should().BeFalse(); // AC3: 已有 -> false
    }

    [Fact]
    public async Task FindPatientByIdNumber_not_found_returns_null()
    {
        // Arrange
        _patientRepository.GetByIdNumberAsync("999999999999999999")
            .Returns((PatientDetailDto?)null);

        // Act
        var result = await _sut.FindPatientByIdNumberAsync("999999999999999999");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindPatientByIdNumber_empty_idNumber_returns_null(string? idNumber)
    {
        // Act
        var result = await _sut.FindPatientByIdNumberAsync(idNumber!);

        // Assert
        result.Should().BeNull();
        await _patientRepository.DidNotReceive().GetByIdNumberAsync(Arg.Any<string>());
    }

    #endregion

    #region AC2: 身份证号不存在 -> QuickCreatePatient + IsNewlyCreated=true

    [Fact]
    public async Task FindOrCreatePatient_new_patient_creates_with_IsNewlyCreated_true()
    {
        // Arrange
        var newPatientId = Guid.NewGuid();
        var cardResult = CreateSuccessCardResult();

        // IdNumber not found
        _patientRepository.GetByIdNumberAsync(cardResult.IdNumber)
            .Returns((PatientDetailDto?)null);

        // QuickCreate returns new ID
        _patientRepository.CreateAsync(Arg.Any<PatientInputDto>())
            .Returns(new PatientDetailDto { Id = newPatientId, Name = "李四" });

        // GetById returns the new patient
        _patientRepository.GetByIdAsync(newPatientId)
            .Returns(new PatientDetailDto
            {
                Id = newPatientId,
                Name = "李四",
                IdNumber = "320102199505151234",
                LastVisitTime = null,
                VisitCount = 0
            });

        // Act
        var result = await _sut.FindOrCreatePatientAsync(cardResult);

        // Assert - AC2 + AC3
        result.Should().NotBeNull();
        result.PatientId.Should().Be(newPatientId);
        result.IsNewlyCreated.Should().BeTrue();
        result.Name.Should().Be("李四");
        result.LastVisitTime.Should().BeNull();
        result.VisitCount.Should().Be(0);
    }

    [Fact]
    public async Task FindOrCreatePatient_existing_patient_returns_with_IsNewlyCreated_false()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var cardResult = CreateSuccessCardResult();
        var existing = CreateExistingPatient(existingId);
        _patientRepository.GetByIdNumberAsync(cardResult.IdNumber).Returns(existing);

        // Act
        var result = await _sut.FindOrCreatePatientAsync(cardResult);

        // Assert - AC3: 已有 -> false, 不调用 Create
        result.IsNewlyCreated.Should().BeFalse();
        result.PatientId.Should().Be(existingId);
        await _patientRepository.DidNotReceive().CreateAsync(Arg.Any<PatientInputDto>());
    }

    #endregion

    #region BR5: 读卡数据自动映射

    [Fact]
    public async Task QuickCreatePatient_maps_card_fields_correctly()
    {
        // Arrange
        var cardResult = CreateSuccessCardResult(
            name: "王五",
            idNumber: "440101198812121234",
            gender: Gender.Female,
            birthDate: new DateTime(1988, 12, 12));

        var capturedInput = (PatientInputDto?)null;
        _patientRepository.CreateAsync(Arg.Do<PatientInputDto>(x => capturedInput = x))
            .Returns(new PatientDetailDto { Id = Guid.NewGuid(), Name = "王五" });

        // Act
        await _sut.QuickCreatePatientAsync(cardResult);

        // Assert - BR5: 姓名->Name, 身份证号->IdNumber, 出生日期->BirthDate, 性别->Gender
        capturedInput.Should().NotBeNull();
        capturedInput!.Name.Should().Be("王五");
        capturedInput.IdNumber.Should().Be("440101198812121234");
        capturedInput.Gender.Should().Be(Gender.Female);
        capturedInput.BirthDate.Should().Be(new DateTime(1988, 12, 12));
        capturedInput.Address.Should().Be("江苏省南京市玄武区中山路1号");
        capturedInput.PhoneNumber.Should().BeNull("身份证不含电话号码");
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task QuickCreatePatient_throws_when_card_read_failed()
    {
        // Arrange
        var failedResult = new CardReadResult
        {
            IsSuccess = false,
            ErrorMessage = "读卡器超时"
        };

        // Act & Assert
        var act = () => _sut.QuickCreatePatientAsync(failedResult);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*读卡失败*");
    }

    [Fact]
    public async Task FindOrCreatePatient_throws_when_card_read_failed()
    {
        // Arrange
        var failedResult = new CardReadResult
        {
            IsSuccess = false,
            ErrorMessage = "设备被占用"
        };

        // Act & Assert
        var act = () => _sut.FindOrCreatePatientAsync(failedResult);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*读卡失败*");
    }

    [Fact]
    public async Task FindPatientByIdNumber_repository_exception_returns_null()
    {
        // Arrange
        _patientRepository.GetByIdNumberAsync(Arg.Any<string>())
            .Returns<PatientDetailDto?>(x => throw new InvalidOperationException("DB error"));

        // Act
        var result = await _sut.FindPatientByIdNumberAsync("320102199505151234");

        // Assert - graceful degradation
        result.Should().BeNull();
    }

    #endregion
}
