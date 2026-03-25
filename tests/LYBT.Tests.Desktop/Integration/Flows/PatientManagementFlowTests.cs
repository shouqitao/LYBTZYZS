using FluentAssertions;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.Integration.Fixtures;
using LYBT.Tests.Desktop._Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.Flows;

[IntegrationTest]
[Collection("WebApiIntegration")]
public class PatientManagementFlowTests : IDisposable
{
    private readonly WebApiFixture _fixture;
    private readonly RealTestComposition _composition;

    public PatientManagementFlowTests(WebApiFixture fixture)
    {
        _fixture = fixture;
        _composition = new RealTestComposition()
            .WithRealRefitClient(_fixture.ApiClient)
            .WithPatientServices()
            .Build();
    }

    public void Dispose()
    {
        if (_composition.GetServiceProvider() is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [StaFact]
    public async Task CreatePatient_WithValidData_PersistsToDatabase()
    {
        var patientRepo = _composition.Resolve<IPatientRepository>();
        var uniquePhone = $"138{DateTime.Now:MMddHHmmss}";
        
        var newPatient = new PatientInputDto
        {
            Name = "张三",
            PhoneNumber = uniquePhone,
            Gender = Gender.Male,
            Address = "北京市朝阳区"
        };

        var result = await patientRepo.CreateAsync(newPatient);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Name.Should().Be("张三");
        result.PhoneNumber.Should().Be(uniquePhone);
        result.Gender.Should().Be(Gender.Male);

        await _fixture.WithDbContextAsync(async db =>
        {
            var patient = await db.Set<Patient>().FindAsync(result.Id);
            patient.Should().NotBeNull();
            patient!.Name.Should().Be("张三");
            patient.PhoneNumber.Should().Be(uniquePhone);
            patient.Gender.Should().Be(Gender.Male);
            patient.Address.Should().Be("北京市朝阳区");
            patient.IsDeleted.Should().BeFalse();
        });
    }

    [StaFact]
    public async Task CreatePatient_WithDuplicatePhone_ShowsError()
    {
        var patientRepo = _composition.Resolve<IPatientRepository>();
        var duplicatePhone = $"139{DateTime.Now:MMddHHmmss}";
        
        var firstPatient = new PatientInputDto
        {
            Name = "李四",
            PhoneNumber = duplicatePhone,
            Gender = Gender.Female
        };
        
        var firstResult = await patientRepo.CreateAsync(firstPatient);
        firstResult.Should().NotBeNull();

        var secondPatient = new PatientInputDto
        {
            Name = "李五",
            PhoneNumber = duplicatePhone,
            Gender = Gender.Male
        };

        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            await patientRepo.CreateAsync(secondPatient);
        });

        exception.Should().NotBeNull();
    }

    [StaFact]
    public async Task SearchPatients_ByName_ReturnsMatchingResults()
    {
        var patientRepo = _composition.Resolve<IPatientRepository>();
        var timestamp = DateTime.Now.ToString("MMddHHmmss");
        
        await patientRepo.CreateAsync(new PatientInputDto
        {
            Name = "王小明",
            PhoneNumber = $"150{timestamp}01",
            Gender = Gender.Male
        });

        await patientRepo.CreateAsync(new PatientInputDto
        {
            Name = "王小红",
            PhoneNumber = $"150{timestamp}02",
            Gender = Gender.Female
        });

        await patientRepo.CreateAsync(new PatientInputDto
        {
            Name = "李小华",
            PhoneNumber = $"150{timestamp}03",
            Gender = Gender.Male
        });

        var searchResults = await patientRepo.SearchAsync("王");

        searchResults.Should().NotBeNull();
        searchResults.Should().Contain(p => p.Name == "王小明");
        searchResults.Should().Contain(p => p.Name == "王小红");
        searchResults.Should().NotContain(p => p.Name == "李小华");
    }

    [StaFact]
    public async Task LoadPatientDetail_ById_ReturnsCorrectData()
    {
        var patientRepo = _composition.Resolve<IPatientRepository>();
        var timestamp = DateTime.Now.ToString("MMddHHmmss");
        
        var newPatient = await patientRepo.CreateAsync(new PatientInputDto
        {
            Name = "赵六",
            PhoneNumber = $"151{timestamp}",
            Gender = Gender.Male,
            Address = "上海市浦东新区",
            IdNumber = "310101199001011234"
        });

        var detail = await patientRepo.GetByIdAsync(newPatient.Id);

        detail.Should().NotBeNull();
        detail!.Id.Should().Be(newPatient.Id);
        detail.Name.Should().Be("赵六");
        detail.PhoneNumber.Should().Be($"151{timestamp}");
        detail.Gender.Should().Be(Gender.Male);
        detail.Address.Should().Be("上海市浦东新区");
        detail.IdNumber.Should().Be("310101199001011234");
    }

    [StaFact]
    public async Task UpdatePatient_UpdatesInDatabase()
    {
        var patientRepo = _composition.Resolve<IPatientRepository>();
        var timestamp = DateTime.Now.ToString("MMddHHmmss");
        
        var createdPatient = await patientRepo.CreateAsync(new PatientInputDto
        {
            Name = "初始姓名",
            PhoneNumber = $"152{timestamp}",
            Gender = Gender.Male,
            Address = "初始地址"
        });

        var updateDto = new PatientInputDto
        {
            Id = createdPatient.Id,
            Name = "更新后的姓名",
            PhoneNumber = $"152{timestamp}",
            Gender = Gender.Female,
            Address = "更新后的地址"
        };

        var updatedResult = await patientRepo.UpdateAsync(updateDto);

        updatedResult.Should().NotBeNull();
        updatedResult.Name.Should().Be("更新后的姓名");
        updatedResult.Gender.Should().Be(Gender.Female);
        updatedResult.Address.Should().Be("更新后的地址");

        await _fixture.WithDbContextAsync(async db =>
        {
            var patient = await db.Set<Patient>().FindAsync(createdPatient.Id);
            patient.Should().NotBeNull();
            patient!.Name.Should().Be("更新后的姓名");
            patient.Gender.Should().Be(Gender.Female);
            patient.Address.Should().Be("更新后的地址");
        });

        var reloadedPatient = await patientRepo.GetByIdAsync(createdPatient.Id);
        reloadedPatient!.Name.Should().Be("更新后的姓名");
    }
}

public static class PatientCompositionExtensions
{
    public static RealTestComposition WithPatientServices(this RealTestComposition composition)
    {
        var servicesField = typeof(RealTestComposition).GetField(
            "_services", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (servicesField?.GetValue(composition) is not IServiceCollection services)
        {
            throw new InvalidOperationException("无法获取 ServiceCollection");
        }

        services.AddSingleton<IPatientRepository, PatientRepository>();

        return composition;
    }
}
