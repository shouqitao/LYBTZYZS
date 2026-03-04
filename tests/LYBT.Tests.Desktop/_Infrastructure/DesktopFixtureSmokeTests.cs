using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Desktop.Infrastructure;

/// <summary>
/// DesktopFixture 烟雾测试.
/// 验证 DI 容器能正确解析所有服务，SQLite InMemory 能正常 CRUD.
/// </summary>
public sealed class DesktopFixtureSmokeTests : IDisposable
{
    private readonly DesktopFixture _fixture;

    public DesktopFixtureSmokeTests()
    {
        _fixture = new DesktopFixture();
        _fixture.CreateServiceProvider();
    }

    [Fact]
    public void CanResolveAllDataSources()
    {
        using var scope = _fixture.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetRequiredService<IPatientDataSource>().Should().NotBeNull();
        sp.GetRequiredService<IHerbDataSource>().Should().NotBeNull();
        sp.GetRequiredService<IFormulaDataSource>().Should().NotBeNull();
    }

    [Fact]
    public void CanResolveAllRepositories()
    {
        using var scope = _fixture.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetRequiredService<IPatientRepository>().Should().NotBeNull();
        sp.GetRequiredService<IHerbRepository>().Should().NotBeNull();
    }

    [Fact]
    public void CanResolveViewModels()
    {
        using var scope = _fixture.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetRequiredService<PatientMasterDetailViewModel>().Should().NotBeNull();
        sp.GetRequiredService<HerbMasterDetailViewModel>().Should().NotBeNull();
        sp.GetRequiredService<UserMasterDetailViewModel>().Should().NotBeNull();
    }

    [Fact]
    public async Task CanPerformCrudViaDataSource()
    {
        using var scope = _fixture.CreateScope();
        var dataSource = scope.ServiceProvider.GetRequiredService<IPatientDataSource>();

        // Create
        var input = new PatientInputDto
        {
            Name = "烟雾测试患者",
            Gender = Gender.Male,
            BirthDate = new DateTime(1990, 1, 1)
        };
        var created = await dataSource.CreateAsync(input);
        created.Should().NotBeNull();
        created.Name.Should().Be("烟雾测试患者");

        // Read
        var found = await dataSource.GetByIdAsync(created.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be("烟雾测试患者");
    }

    [Fact]
    public async Task CanPerformCrudViaRepository()
    {
        using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPatientRepository>();

        var input = new PatientInputDto
        {
            Name = "Repository测试患者",
            Gender = Gender.Female,
            BirthDate = new DateTime(1985, 6, 15)
        };
        var created = await repo.CreateAsync(input);
        created.Should().NotBeNull();

        var found = await repo.GetByIdAsync(created.Id);
        found.Should().NotBeNull();
    }

    public void Dispose() => _fixture.Dispose();
}
