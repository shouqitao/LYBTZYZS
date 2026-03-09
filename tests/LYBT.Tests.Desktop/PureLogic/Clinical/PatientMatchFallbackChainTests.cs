using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Desktop.PureLogic.Clinical;

/// <summary>
/// PRD-15: 患者去重降级链
/// 降级链: (1) IdNumber 精确匹配 -> (2) Name+BirthDate 模糊匹配 -> (3) 多条命中列表选择 -> (4) 无匹配
/// TDD RED: 验证 MatchPatientAsync 降级链逻辑
/// </summary>
public class PatientMatchFallbackChainTests
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<PatientCardReaderIntegration> _logger;
    private readonly PatientCardReaderIntegration _sut;

    public PatientMatchFallbackChainTests()
    {
        _patientRepository = Substitute.For<IPatientRepository>();
        _logger = Substitute.For<ILogger<PatientCardReaderIntegration>>();
        _sut = new PatientCardReaderIntegration(_patientRepository, _logger);
    }

    private static CardReadResult CreateSuccessCardResult(
        string name = "张三",
        string idNumber = "110101199001011234",
        DateTime? birthDate = null)
    {
        return CardReadResult.Success(
            name: name,
            idNumber: idNumber,
            sex: "男",
            nation: "汉",
            birth: (birthDate ?? new DateTime(1990, 1, 1)).ToString("yyyyMMdd"),
            address: "北京市东城区",
            department: "北京市公安局",
            effectDate: "20200101",
            expireDate: "20400101");
    }

    private static PatientDetailDto CreatePatientDto(
        Guid? id = null,
        string name = "张三",
        string? idNumber = "110101199001011234",
        DateTime? birthDate = null)
    {
        return new PatientDetailDto
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            IdNumber = idNumber,
            BirthDate = birthDate ?? new DateTime(1990, 1, 1),
            Gender = Gender.Male,
            LastVisitTime = DateTime.Now.AddDays(-7),
            VisitCount = 3
        };
    }

    #region Step 1: IdNumber Exact Match

    [Fact]
    public async Task MatchPatientAsync_returns_ExactMatch_when_IdNumber_found()
    {
        // Arrange
        var cardResult = CreateSuccessCardResult();
        var existingPatient = CreatePatientDto();
        _patientRepository.GetByIdNumberAsync("110101199001011234")
            .Returns(existingPatient);

        // Act
        var result = await _sut.MatchPatientAsync(cardResult);

        // Assert
        result.MatchType.Should().Be(PatientMatchType.ExactMatch);
        result.Patient.Should().NotBeNull();
        result.Patient!.PatientId.Should().Be(existingPatient.Id);
        result.Patient.IsNewlyCreated.Should().BeFalse();
        result.Candidates.Should().BeEmpty();
    }

    #endregion

    #region Step 2: Name+BirthDate Single Match (fuzzy)

    [Fact]
    public async Task MatchPatientAsync_returns_FuzzyMatch_when_single_NameBirthDate_match()
    {
        // Arrange: IdNumber not found, but SearchAsync finds one patient with matching Name
        var cardResult = CreateSuccessCardResult(
            name: "张三",
            birthDate: new DateTime(1990, 1, 1));
        _patientRepository.GetByIdNumberAsync(Arg.Any<string>())
            .Returns((PatientDetailDto?)null);

        var matchId = Guid.NewGuid();
        // SearchAsync returns list items (no BirthDate), need detail for BirthDate check
        _patientRepository.SearchAsync("张三")
            .Returns(new List<PatientListDto>
            {
                new() { Id = matchId, Name = "张三" }
            });
        // GetByIdAsync to check BirthDate
        _patientRepository.GetByIdAsync(matchId)
            .Returns(CreatePatientDto(id: matchId, name: "张三", idNumber: null, birthDate: new DateTime(1990, 1, 1)));

        // Act
        var result = await _sut.MatchPatientAsync(cardResult);

        // Assert
        result.MatchType.Should().Be(PatientMatchType.FuzzyMatch);
        result.Patient.Should().NotBeNull();
        result.Patient!.Name.Should().Be("张三");
        result.Candidates.Should().HaveCount(1);
    }

    #endregion

    #region Step 3: Multiple Candidates

    [Fact]
    public async Task MatchPatientAsync_returns_MultipleCandidates_when_multiple_NameBirthDate_matches()
    {
        // Arrange
        var cardResult = CreateSuccessCardResult(
            name: "张三",
            birthDate: new DateTime(1990, 1, 1));
        _patientRepository.GetByIdNumberAsync(Arg.Any<string>())
            .Returns((PatientDetailDto?)null);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        _patientRepository.SearchAsync("张三")
            .Returns(new List<PatientListDto>
            {
                new() { Id = id1, Name = "张三" },
                new() { Id = id2, Name = "张三" }
            });
        _patientRepository.GetByIdAsync(id1)
            .Returns(CreatePatientDto(id: id1, name: "张三", idNumber: null, birthDate: new DateTime(1990, 1, 1)));
        _patientRepository.GetByIdAsync(id2)
            .Returns(CreatePatientDto(id: id2, name: "张三", idNumber: "220102199001011111", birthDate: new DateTime(1990, 1, 1)));

        // Act
        var result = await _sut.MatchPatientAsync(cardResult);

        // Assert
        result.MatchType.Should().Be(PatientMatchType.MultipleCandidates);
        result.Patient.Should().BeNull();
        result.Candidates.Should().HaveCount(2);
    }

    #endregion

    #region Step 4: No Match

    [Fact]
    public async Task MatchPatientAsync_returns_NoMatch_when_nothing_found()
    {
        // Arrange
        var cardResult = CreateSuccessCardResult();
        _patientRepository.GetByIdNumberAsync(Arg.Any<string>())
            .Returns((PatientDetailDto?)null);
        _patientRepository.SearchAsync("张三")
            .Returns(new List<PatientListDto>());

        // Act
        var result = await _sut.MatchPatientAsync(cardResult);

        // Assert
        result.MatchType.Should().Be(PatientMatchType.NoMatch);
        result.Patient.Should().BeNull();
        result.Candidates.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task MatchPatientAsync_throws_when_card_result_failed()
    {
        var failedResult = CardReadResult.Failure(-1, "读卡失败");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.MatchPatientAsync(failedResult));
    }

    [Fact]
    public async Task MatchPatientAsync_throws_when_card_result_null()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.MatchPatientAsync(null!));
    }

    [Fact]
    public async Task MatchPatientAsync_filters_by_BirthDate_from_search_results()
    {
        // Arrange: search returns patients with same name but different BirthDate
        var cardResult = CreateSuccessCardResult(
            name: "张三",
            birthDate: new DateTime(1990, 1, 1));
        _patientRepository.GetByIdNumberAsync(Arg.Any<string>())
            .Returns((PatientDetailDto?)null);

        var matchId = Guid.NewGuid();
        var nonMatchId = Guid.NewGuid();
        _patientRepository.SearchAsync("张三")
            .Returns(new List<PatientListDto>
            {
                new() { Id = matchId, Name = "张三" },
                new() { Id = nonMatchId, Name = "张三" },
            });
        _patientRepository.GetByIdAsync(matchId)
            .Returns(CreatePatientDto(id: matchId, name: "张三", birthDate: new DateTime(1990, 1, 1)));
        _patientRepository.GetByIdAsync(nonMatchId)
            .Returns(CreatePatientDto(id: nonMatchId, name: "张三", birthDate: new DateTime(1995, 5, 5)));

        // Act
        var result = await _sut.MatchPatientAsync(cardResult);

        // Assert: only the matching BirthDate should be in candidates
        result.MatchType.Should().Be(PatientMatchType.FuzzyMatch);
        result.Candidates.Should().HaveCount(1);
    }

    [Fact]
    public async Task MatchPatientAsync_skips_IdNumber_lookup_when_IdNumber_empty()
    {
        // When IdNumber is empty, skip exact match and go directly to fuzzy
        var cardResult = CreateSuccessCardResult(
            name: "张三",
            idNumber: "");
        _patientRepository.SearchAsync("张三")
            .Returns(new List<PatientListDto>());

        var result = await _sut.MatchPatientAsync(cardResult);

        result.MatchType.Should().Be(PatientMatchType.NoMatch);
        await _patientRepository.DidNotReceive().GetByIdNumberAsync(Arg.Any<string>());
    }

    #endregion
}
