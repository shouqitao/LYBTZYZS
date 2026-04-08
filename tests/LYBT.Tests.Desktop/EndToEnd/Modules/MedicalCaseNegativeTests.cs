using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Refit;
using System.Net;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class MedicalCaseNegativeTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;
    private static int _idSequence;
    private static int _phoneSequence;

    public MedicalCaseNegativeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GenerateValidIdNumber()
    {
        var sequence = Interlocked.Increment(ref _idSequence);
        var day = 10 + (sequence % 18);
        var serial = 100 + (sequence % 900);
        var body = $"110101199001{day:D2}{serial:D3}";
        int[] weights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        char[] checkDigits = { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };
        var sum = 0;
        for (var i = 0; i < 17; i++) sum += (body[i] - '0') * weights[i];
        return body + checkDigits[sum % 11];
    }

    private async Task<Guid> CreateTestPatientAsync()
    {
        var patient = new PatientInputDto
        {
            Name = $"负测患者_{Guid.NewGuid():N}",
            Gender = Gender.Male,
            PhoneNumber = $"138{Interlocked.Increment(ref _phoneSequence):D8}",
            IdNumber = GenerateValidIdNumber(),
            Address = "北京市测试区测试街道1号",
            PinYinCode = "FCHZ"
        };
        var response = await PatientApi.CreatePatientAsync(patient);
        return response.Data!.Id;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreateCase_NonexistentPatientId_ShouldFail()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var input = new MedicalCaseInputDto
        {
            PatientId = Guid.NewGuid(),
            UserId = loginResponse.User.Id,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "测试现病史",
                TcmDiagnosis = "测试中医诊断"
            }
        };

        try
        {
            var response = await MedicalCaseApi.CreateMedicalCaseAsync(input);
            response.Success.Should().BeFalse("不存在的患者ID应创建失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.UnprocessableEntity, HttpStatusCode.NotFound);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreateCase_NonexistentDoctorId_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = Guid.NewGuid(),
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "测试现病史",
                TcmDiagnosis = "测试中医诊断"
            }
        };

        try
        {
            var response = await MedicalCaseApi.CreateMedicalCaseAsync(input);
            response.Success.Should().BeFalse("不存在的医生ID应创建失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.UnprocessableEntity, HttpStatusCode.NotFound);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task GetCase_NonexistentId_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var fakeId = Guid.NewGuid();

        try
        {
            var response = await MedicalCaseApi.GetMedicalCaseByIdAsync(fakeId);
            response.Success.Should().BeFalse("不存在的医案ID应返回失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CloseCase_AlreadyClosed_ShouldFail()
    {
        var loginResponse = await LoginAsSysadminAsync();
        var patientId = await CreateTestPatientAsync();
        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = loginResponse.User.Id,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "测试现病史",
                TcmDiagnosis = "测试中医诊断"
            }
        };
        var createResponse = await MedicalCaseApi.CreateMedicalCaseAsync(input);
        var caseId = createResponse.Data!.Id;

        await MedicalCaseApi.CloseCaseAsync(caseId);

        try
        {
            var response = await MedicalCaseApi.CloseCaseAsync(caseId);
            response.Success.Should().BeFalse("已关闭的医案不应再次关闭成功");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.UnprocessableEntity, HttpStatusCode.Conflict);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task DeleteCase_NonexistentId_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var fakeId = Guid.NewGuid();

        try
        {
            var response = await MedicalCaseApi.DeleteMedicalCaseAsync(fakeId);
            response.Success.Should().BeFalse("删除不存在的医案应失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
