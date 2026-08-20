using FluentAssertions;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.RemoteApi;

[Collection("RemoteApi")]
[Trait("Category", "RemoteApi")]
public class AdminRoleTests : RemoteApiTestBase
{
    protected override async Task SetupRoleAsync()
    {
        // sysadmin 创建 Admin 账号并登录该 Admin
        await LoginAsCreatedAdminAsync();
    }

    [Fact]
    [Trait("US", "US-PAT-001")]
    public async Task GetPatients_ReturnsPaged()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await PatientApi.GetPatientsAsync(1, 10);
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [Fact]
    [Trait("US", "US-PAT-003")]
    public async Task CreatePatient_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var input = new PatientInputDto
        {
            Name = UniqueName("E2E患者"),
            Gender = Gender.Male,
            PhoneNumber = UniquePhone(),
            IdNumber = UniqueIdNumber()
        };
        var created = await PatientApi.CreatePatientAsync(input);
        created.Success.Should().BeTrue(created.Message);
        created.Data.Should().NotBeNull();
        if (created.Data != null)
        {
            var del = await PatientApi.DeletePatientAsync(created.Data.Id);
            del.Success.Should().BeTrue(del.Message);
        }
    }

    [Fact]
    [Trait("US", "US-PAT-004")]
    public async Task UpdatePatient_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var input = new PatientInputDto
        {
            Name = UniqueName("E2E患者"),
            Gender = Gender.Female,
            PhoneNumber = UniquePhone()
        };
        var created = await PatientApi.CreatePatientAsync(input);
        created.Success.Should().BeTrue(created.Message);
        created.Data.Should().NotBeNull();
        var id = created.Data!.Id;

        var updated = new PatientInputDto
        {
            Id = id,
            Name = UniqueName("E2E更新"),
            Gender = Gender.Female,
            PhoneNumber = UniquePhone()
        };
        var resp = await PatientApi.UpdatePatientAsync(id, updated);
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data!.Name.Should().Be(updated.Name);

        await PatientApi.DeletePatientAsync(id);
    }

    [Fact]
    [Trait("US", "US-HERB-001")]
    public async Task GetHerbs_ReturnsPaged()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await HerbApi.GetHerbsAsync(1, 10);
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [Fact]
    [Trait("US", "US-HERB-003")]
    public async Task CreateHerb_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var input = new HerbInputDto
        {
            Name = UniqueName("E2E药材"),
            PinYinCode = "EYYC",
            Category = "补气药",
            Unit = "g",
            Price = 10m
        };
        var created = await HerbApi.CreateHerbAsync(input);
        created.Success.Should().BeTrue(created.Message);
        created.Data.Should().NotBeNull();
        if (created.Data != null)
        {
            await HerbApi.DeleteHerbAsync(created.Data.Id);
        }
    }

    [Fact]
    [Trait("US", "US-FORM-001")]
    public async Task GetFormulas_ReturnsPaged()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await FormulaApi.GetFormulasAsync(1, 10);
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [Fact]
    [Trait("US", "US-REG-001")]
    public async Task GetRegistrations_ReturnsPaged()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await RegistrationApi.GetListAsync(1, 10);
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }
}
