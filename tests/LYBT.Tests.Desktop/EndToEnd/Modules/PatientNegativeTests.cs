using FluentAssertions;
using System.Threading;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Refit;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class PatientNegativeTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public PatientNegativeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static int _counter = 0;

    private static string GenerateValidIdNumber()
    {
        var unique = Interlocked.Increment(ref _counter);
        var day = 10 + (unique % 18);
        var seq = 100 + (unique % 899);
        var body = $"110101199001{day:D2}{seq:D3}";
        int[] weights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        char[] checkDigits = { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };
        var sum = 0;
        for (var i = 0; i < 17; i++) sum += (body[i] - '0') * weights[i];
        return body + checkDigits[sum % 11];
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreatePatient_EmptyName_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = new PatientInputDto { Name = "", Gender = Gender.Male, PhoneNumber = "13800138000", Address = "测试地址", IdNumber = GenerateValidIdNumber() };

        try
        {
            var response = await PatientApi.CreatePatientAsync(input);
            E2EAssertionHelpers.AssertError(response, "Name");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreatePatient_InvalidIdNumber_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = new PatientInputDto
        {
            Name = "测试患者",
            Gender = Gender.Male,
            IdNumber = "INVALID_ID_12345",
            PhoneNumber = "13800138000",
            Address = "测试地址"
        };

        try
        {
            var response = await PatientApi.CreatePatientAsync(input);
            E2EAssertionHelpers.AssertError(response);
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreatePatient_InvalidPhoneNumber_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = new PatientInputDto
        {
            Name = "测试患者",
            Gender = Gender.Male,
            PhoneNumber = "abc123",
            Address = "测试地址",
            IdNumber = GenerateValidIdNumber()
        };

        try
        {
            var response = await PatientApi.CreatePatientAsync(input);
            E2EAssertionHelpers.AssertError(response);
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task GetPatient_NonexistentId_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var fakeId = Guid.NewGuid();

        try
        {
            var response = await PatientApi.GetPatientByIdAsync(fakeId);
            response.Success.Should().BeFalse("不存在的患者ID应返回失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task DeletePatient_NonexistentId_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var fakeId = Guid.NewGuid();

        try
        {
            var response = await PatientApi.DeletePatientAsync(fakeId);
            response.Success.Should().BeFalse("删除不存在的患者应返回失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
