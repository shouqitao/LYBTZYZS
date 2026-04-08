using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Refit;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class RegistrationNegativeTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public RegistrationNegativeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CreateRegistration_NonexistentPatient_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var input = new RegistrationInputDto
        {
            PatientId = Guid.NewGuid(),
            PatientName = "不存在的患者",
            DoctorId = Guid.NewGuid(),
            DoctorName = "不存在的医生"
        };

        try
        {
            var response = await RegistrationApi.CreateAsync(input);
            response.Success.Should().BeFalse("不存在的患者挂号应失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.UnprocessableEntity, HttpStatusCode.NotFound);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task GetRegistration_NonexistentId_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var fakeId = Guid.NewGuid();

        try
        {
            var response = await RegistrationApi.GetByIdAsync(fakeId);
            response.Success.Should().BeFalse("不存在的挂号ID应返回失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task StartVisit_NonexistentRegistration_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var fakeId = Guid.NewGuid();

        try
        {
            var response = await RegistrationApi.StartVisitAsync(fakeId);
            response.Success.Should().BeFalse("不存在的挂号开始就诊应失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task CancelRegistration_NonexistentId_ShouldFail()
    {
        await LoginAsSysadminAsync();
        var fakeId = Guid.NewGuid();

        try
        {
            var response = await RegistrationApi.CancelAsync(fakeId);
            response.Success.Should().BeFalse("取消不存在的挂号应失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.UnprocessableEntity);
        }
    }
}
